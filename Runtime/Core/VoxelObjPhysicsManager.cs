using VoxelEngine.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public class VoxelObjPhysicsManager
    {
        public VoxelObj MyVoxelObj { get; private set; }
        public Rigidbody MyRigidBody { get; private set; }
        private GameObject _myBoxColliderHolder;
        public GameObject MyBoxColliderHolder
        {
            get
            {
                if (_myBoxColliderHolder == null)
                {
                    _myBoxColliderHolder = new GameObject("boxColliderHolder");
                    _myBoxColliderHolder.transform.parent = MyVoxelObj.GetVoxelObjHolder().transform;
                    _myBoxColliderHolder.transform.position = MyVoxelObj.transform.position;
                    _myBoxColliderHolder.transform.rotation = MyVoxelObj.transform.rotation;
                }
                return _myBoxColliderHolder;
            }
        }

        public bool PhysicsEnabled { get; private set; } = false;
        public int MyBoxColliderAmount { get; private set; }
        public static int TotalBoxColliderAmount { get; private set; }

        public event Action BoxCollidersGenerated;
        private Queue<Action> ExecuteWhenUpdateFinished = new Queue<Action>();
        private Queue<Action> ExecuteWhenNextUpdateFinished = new Queue<Action>();
        public bool CreatingBoxColliders { get; private set; } = false;

        private bool boxColliderUpdateRequired = false;
        private Vector3 savedVelocity;

        public VoxelObjPhysicsManager(VoxelObj myVoxelObj)
        {
            this.MyVoxelObj = myVoxelObj;

            MyVoxelObj.OnDestroying += OnDestroy;
        }

        private void OnDestroy()
        {
            TotalBoxColliderAmount -= MyBoxColliderAmount;
        }

        /// <summary>
        /// </summary>
        /// <param name="disableAllCunkMeshColliders">Should be set to true if the chunks already have MeshColliders (when the first VoxelObjUpdate has already been called)</param>
        /// <param name="callGenerateBoxCollidersNow">Should be set to ture if the chunks already render a mesh (when the first VoxelObjUpdate has already been called)</param>
        public void EnablePhysics(bool disableAllCunkMeshColliders = false, bool callGenerateBoxCollidersNow = false)
        {
            PhysicsEnabled = true;

            if (MyRigidBody == null) MyRigidBody = MyVoxelObj.GetVoxelObjHolder().gameObject.AddComponent<Rigidbody>();

            if(disableAllCunkMeshColliders)
            {
                DisableAllCunkMeshColliders();
            }

            if(callGenerateBoxCollidersNow)
            {
                GenerateBoxColliders();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="disableAllCunkMeshColliders">Should be set to true if the chunks already have MeshColliders (when the first VoxelObjUpdate has already been called)</param>
        /// <param name="generateBoxCollidersNow">Should be set to ture if the chunks already render a mesh (when the first VoxelObjUpdate has already been called)</param>
        public void EnablePhysicsImmediate(bool disableAllCunkMeshColliders = false, bool callGenerateBoxCollidersImmediateNow = false)
        {
            PhysicsEnabled = true;

            if (MyRigidBody == null) MyRigidBody = MyVoxelObj.GetVoxelObjHolder().gameObject.AddComponent<Rigidbody>();
            if (disableAllCunkMeshColliders)
            {
                DisableAllCunkMeshColliders();
            }

            if (callGenerateBoxCollidersImmediateNow)
            {
                GenerateBoxCollidersImmediate();
            }

        }

        public void DisablePhysics(bool destroyBoxColliders = false)
        {
            PhysicsEnabled = false;

            if (MyRigidBody != null) MonoBehaviour.Destroy(MyRigidBody);

            if (destroyBoxColliders) DestroyBoxCollides();
        }

        public void DisableAllCunkMeshColliders()
        {
            if (MyVoxelObj.Chunks == null) return;
            foreach (Chunk chunk in MyVoxelObj.Chunks)
            {
                chunk.MyMeshCollider.enabled = false;
            }
        }

        /// <summary>
        /// Sets rigidbody.kinematic to true or false. In case it is set to true: Velocity is saved and reapplied when kinematic is set to false again 
        /// </summary>
        public void ToggleRigidbodyKinematic(bool isKinematic)
        {
            if (MyRigidBody == null) return;

            if(isKinematic == true)
            {
                MyRigidBody.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
                savedVelocity = MyRigidBody.linearVelocity;
#else
                savedVelocity = MyRigidBody.velocity;
#endif
            }
            else
            {
                MyRigidBody.isKinematic = false;
#if UNITY_6000_0_OR_NEWER
                MyRigidBody.linearVelocity = savedVelocity;
#else
                MyRigidBody.velocity = savedVelocity;
#endif
            }
        }

        public void DestroyBoxCollides()
        {
            foreach (BoxCollider collider in MyBoxColliderHolder.GetComponents<BoxCollider>()) MonoBehaviour.DestroyImmediate(collider);
            TotalBoxColliderAmount -= MyBoxColliderAmount;
            MyBoxColliderAmount = 0;
        }

        public void GenerateBoxCollidersImmediate(Action doOnCompletion = null)
        {
            GenerateBoxCollidersInternal(true, doOnCompletion);
        }

        public void GenerateBoxColliders(Action doOnCompletion = null)
        {
            GenerateBoxCollidersInternal(false, doOnCompletion);
        }

        private void GenerateBoxCollidersInternal(bool immediate, Action doOnCompletion = null)
        {
            if(PhysicsEnabled == false)
            {
                Debug.LogError("Physics are not enabled");
                return;
            }
            
            if (MyVoxelObj.VoxelMap == null) return;

            if (CreatingBoxColliders)
            {
                boxColliderUpdateRequired = true;
                if(doOnCompletion != null) ExecuteWhenNextUpdateFinished.Enqueue(doOnCompletion);

                return;
            }
            else CreatingBoxColliders = true;

            if (doOnCompletion != null) ExecuteWhenUpdateFinished.Enqueue(doOnCompletion);

            startJob();
            return;

            void startJob()
            {
                NativeArray<VoxelData> nativeVoxelMap = new NativeArray<VoxelData>(MyVoxelObj.VoxelMap.voxelData.Length, Allocator.TempJob);
                nativeVoxelMap.CopyFrom(MyVoxelObj.VoxelMap.voxelData);

                BoxColliderOptimisationJob bcoJob = new BoxColliderOptimisationJob(nativeVoxelMap, MyVoxelObj.VoxelMap.dimensions);
                JobHandle bcoHandle = bcoJob.Schedule();

                IDisposable[] disposables = new IDisposable[5]
                {
                    bcoJob.voxelMap,
                    bcoJob.boxColliderStartPosByMapIndex,
                    bcoJob.boxColliderEndPosByMapIndex,
                    bcoJob.colliderCount,
                    bcoJob.voxelCount
                };

                if(immediate)
                {
                    onJobCompletion();
                }
                else
                {
                    JobCallbackManager.Register(bcoHandle, onJobCompletion, disposables);
                }

                void onJobCompletion()
                {
                    bcoHandle.Complete();

                    try
                    {
                        if (MyVoxelObj != null)
                        {
                            addColliders(bcoJob);
                        }
                    }
                    finally
                    {
                        disposeJob(bcoJob);
                    }

                    CreatingBoxColliders = false;
                    invokeCompletionEvents();
                }
            }

            void addColliders(BoxColliderOptimisationJob completedJob)
            {
                float3 voxelCenterOffset = new float3(VoxelObj.voxelSize / 2, VoxelObj.voxelSize / 2, VoxelObj.voxelSize / 2);
                Vector3 _voxelsize = new Vector3(VoxelObj.voxelSize, VoxelObj.voxelSize, VoxelObj.voxelSize);

                bool wantedKinematicValue = MyRigidBody.isKinematic;
                if (wantedKinematicValue == false) ToggleRigidbodyKinematic(true);

                BoxCollider[] existingBoxcolliders = MyBoxColliderHolder.GetComponents<BoxCollider>();
                for (int i = 0; i != completedJob.colliderCount[0]; i++)
                {
                    BoxCollider newBoxCollider;
                    ///Reuse existing boxcollider or create a new one if we ran out of existing colliders
                    if (i < existingBoxcolliders.Length) newBoxCollider = existingBoxcolliders[i];
                    else newBoxCollider = MyBoxColliderHolder.AddComponent<BoxCollider>();

                    Vector3 minPos = completedJob.boxColliderStartPosByMapIndex[i] * VoxelObj.voxelSize + voxelCenterOffset;
                    Vector3 maxPos = completedJob.boxColliderEndPosByMapIndex[i] * VoxelObj.voxelSize + voxelCenterOffset;
                    newBoxCollider.center = (maxPos - minPos) / 2 + minPos;
                    newBoxCollider.size = _voxelsize + maxPos - minPos;
                }

                ///Destroy old boxcolliders if there are any that have not been reused
                for (int i = completedJob.colliderCount[0]; i < existingBoxcolliders.Length; i++) MonoBehaviour.Destroy(existingBoxcolliders[i]);

                TotalBoxColliderAmount -= MyBoxColliderAmount;
                MyBoxColliderAmount = completedJob.colliderCount[0];
                TotalBoxColliderAmount += MyBoxColliderAmount;

                MyRigidBody.mass = completedJob.voxelCount[0] * VoxelObj.voxelMass;

                if (wantedKinematicValue == false) ToggleRigidbodyKinematic(false);
            }

            void disposeJob(BoxColliderOptimisationJob job)
            {
                job.voxelMap.Dispose();
                job.boxColliderStartPosByMapIndex.Dispose();
                job.boxColliderEndPosByMapIndex.Dispose();
                job.colliderCount.Dispose();
                job.voxelCount.Dispose();
            }

            void invokeCompletionEvents()
            {
                BoxCollidersGenerated.Invoke();

                if (ExecuteWhenUpdateFinished.Count > 0)
                {
                    foreach (Action action in ExecuteWhenUpdateFinished) action.Invoke();
                    ExecuteWhenUpdateFinished.Clear();

                    if (ExecuteWhenNextUpdateFinished.Count > 0)
                    {
                        foreach (Action action in ExecuteWhenNextUpdateFinished) ExecuteWhenUpdateFinished.Enqueue(action);
                        ExecuteWhenNextUpdateFinished.Clear();
                    }
                }

                if (boxColliderUpdateRequired)
                {
                    boxColliderUpdateRequired = false;
                    GenerateBoxColliders();
                }
            }
        }
    }
}
