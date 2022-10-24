using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    [AddComponentMenu("VoxelEngine/" + nameof(VoxelObj))]
    public class VoxelObj : MonoBehaviour
    {
        // "Constants" (can be changed (const is faster than static readonly))
        public const float voxelSize = 0.2f;
        public const int chunkSizeInVoxels = 16; ///size for x,y,z Axis in Voxels (MAX 16 (beacuse of Unity's limited Mesh sizes))
        public const float voxelMass = 0.01f;
        ////

        // References
        private Material chunkMaterial;
        public Material ChunkMaterial => chunkMaterial;

        private VoxelObjHolder voxelObjHolder;
        public VoxelMap VoxelMap { get; private set; }
        public Chunk[,,] Chunks { get; private set; }
        public VoxelObjPhysicsManager MyVoxelObjPhysicsManager { private set; get; }

        // VoxelObjUpdate related variables
        public bool UpdatingVoxelObj { get; private set; } = false;
        private bool CurrentUpdateStartedBoxColliderGeneration;
        public delegate void VoxelObjUpdateEventHandler(VoxelObj sender);
        public event VoxelObjUpdateEventHandler UpdateStarted;
        /// <summary> <see cref="UpdateFinished"/> gets invoked after all meshes and colliders (no matter if mesh- or boxcolliders) have been generated </summary>
        public event VoxelObjUpdateEventHandler UpdateFinished;
        private Queue<Action> ExecuteWhenUpdateFinished = new Queue<Action>();
        private Queue<Action> ExecuteWhenNextUpdateFinished = new Queue<Action>();
        public bool GenerateColliders { get; set; } = true;
        public event Action OnDestroying;

        private HashSet<Chunk> NextUpdateChunkBatch;
        private HashSet<Chunk> currentlyUpdatingChunks;
        private bool awatingBoxColliderCompletion;

        private NativeArray<VoxelData> nativeVoxelMapForMeshJobs;
        private void Awake()
        {
            resolveDependencies();
            
            void resolveDependencies()
            {
                MyVoxelObjPhysicsManager = new VoxelObjPhysicsManager(this);
                MyVoxelObjPhysicsManager.BoxCollidersGenerated += OnBoxCollidersGenerated;

                currentlyUpdatingChunks = new HashSet<Chunk>();
                NextUpdateChunkBatch = new HashSet<Chunk>();
            }
        }

        private void OnDestroy()
        {
            if (OnDestroying != null) OnDestroying.Invoke();
        }

        public void SetChunkMaterial(Material material)
        {
            chunkMaterial = material;

            if (Chunks != null)
            {
                foreach (Chunk c in Chunks) c.MyRenderer.material = chunkMaterial;
            }
        }

        #region init methods (new map and chunks etc)
        /// <summary>
        /// This method is faster than instantiating a <see cref="VoxelObj"/>-GameObject directly (Instantiate(<see cref="VoxelObj"/>)) because this method creates
        /// the <see cref="VoxelObjHolder"/> before and instantiates the <param name="VoxelObjPrefab"/> with the <see cref="VoxelObjHolder"/> as parent parameter
        /// </summary>
        /// <param name="VoxelObjPrefab"></param>
        /// <param name=""></param>
        public VoxelObj InstantiateVoxelObj()
        {
            VoxelObjHolder voxelObjHolder = Instantiate(VoxelObjHolder.RuntimePrefab);
            voxelObjHolder.gameObject.SetActive(true);

            VoxelObj voxelObj = Instantiate(this, voxelObjHolder.transform);
            voxelObjHolder.gameObject.name = VoxelObjHolder.GenerateGameObjectName(voxelObj.gameObject.name);

            voxelObj.SetMyVoxelObjHolderInternal(voxelObjHolder);

            return voxelObj;
        }

        /// <summary>
        /// Returns <see cref="VoxelObj.voxelObjHolder"/>. If <see cref="VoxelObj.voxelObjHolder"/> is null a new <see cref="VoxelObjHolder"/> will be created and returned.
        /// </summary>
        public VoxelObjHolder GetVoxelObjHolder()
        {
            if (voxelObjHolder == null)
            {
                VoxelObjHolder holder = Instantiate(VoxelObjHolder.RuntimePrefab, transform.position, transform.rotation);
                holder.SetGeneratedGameObjectName(gameObject.name);
                holder.gameObject.SetActive(true);
                SetMyVoxelObjHolder(holder);
            }

            return voxelObjHolder;
        }

        public void SetMyVoxelObjHolder(VoxelObjHolder voxelObjHolder)
        {
            if (this.voxelObjHolder != null)
            {
                Debug.LogError("VoxelObj already has a VoxelObjHolder");
                return;
            }

            if (voxelObjHolder.MyVoxelObj != null)
            {
                Debug.LogError("VoxelObjHolder already holds a VoxelObj. That can not be changed. Create a new VoxelObjHolder instead");
                return;
            }

            if (voxelObjHolder.transform != this.transform.parent)
            {
                this.transform.position = voxelObjHolder.transform.position;
                this.transform.rotation = voxelObjHolder.transform.rotation;
                this.transform.SetParent(voxelObjHolder.transform);
            }
            SetMyVoxelObjHolderInternal(voxelObjHolder);
        }

        private void SetMyVoxelObjHolderInternal(VoxelObjHolder voxelObjHolder)
        {
            this.voxelObjHolder = voxelObjHolder;
            this.voxelObjHolder.MyVoxelObj = this;
        }

        public void CreateEmptyVoxelMap(int3 mapDimensions)
        {
            if (VoxelMap != null)
            {
                throw new InvalidOperationException("Can not create new VoxelMap: This VoxelObj already has a VoxelMap. Create a new VoxelObj instead.");
            }

            VoxelMap = new VoxelMap(mapDimensions);
            CreateChunkObjs();
        }

        public void LoadExistingVoxelMap(VoxelMap voxelMap)
        {
            if (VoxelMap != null)
            {
                throw new InvalidOperationException("Can not create new VoxelMap: This VoxelObj already has a VoxelMap. Create a new VoxelObj instead");
            }
            VoxelMap = voxelMap;
            CreateChunkObjs();
        }

        private void CreateChunkObjs()
        {
            if (Chunks != null)
            {
                foreach (Chunk chunk in Chunks)
                {
                    Destroy(chunk.gameObject);
                }
            }

            int chunkArraySizeX = (int)Mathf.Ceil((float)VoxelMap.dimensions.x / (float)chunkSizeInVoxels);
            int chunkArraySizeY = (int)Mathf.Ceil((float)VoxelMap.dimensions.y / (float)chunkSizeInVoxels);
            int chunkArraySizeZ = (int)Mathf.Ceil((float)VoxelMap.dimensions.z / (float)chunkSizeInVoxels);

            Chunks = new Chunk[chunkArraySizeX, chunkArraySizeY, chunkArraySizeZ];

            Chunk localChunkRuntimePrefab = Instantiate(Chunk.RuntimePrefab);
            localChunkRuntimePrefab.GetComponent<Renderer>().material = chunkMaterial;

            GameObject chunkHolderObj = new GameObject();
            chunkHolderObj.transform.SetParent(GetVoxelObjHolder().transform);
            chunkHolderObj.transform.localPosition = Vector3.zero;
            chunkHolderObj.transform.localRotation = Quaternion.identity;
            chunkHolderObj.transform.localScale = Vector3.one;
            chunkHolderObj.name = "Chunks";

            Transform holderTransform = chunkHolderObj.transform;

            for (int y = 0; y < chunkArraySizeY; y++)
            {
                for (int x = 0; x < chunkArraySizeX; x++)
                {
                    for (int z = 0; z < chunkArraySizeZ; z++)
                    {
                        Chunks[x, y, z] = Instantiate(localChunkRuntimePrefab, holderTransform);
                        Chunks[x, y, z].gameObject.SetActive(true);
                        int3 positionInVoxelMap = new int3(x * chunkSizeInVoxels, y * chunkSizeInVoxels, z * chunkSizeInVoxels);
                        Chunks[x, y, z].Initialize(this, positionInVoxelMap);
                    }
                }
            }

            Destroy(localChunkRuntimePrefab.gameObject);
        }
        #endregion

        #region voxelMap edit methods
        /// <summary>
        /// This will add the <see cref="Chunk"/> in which the voxel at the specified index is to <see cref="NextUpdateChunkBatch"/>.
        /// <para> This method will not check wether the voxel is a direct neighbor to another Chunk. (Use <see cref="VoxelObj.SetVoxelFilledValue(int3, bool)"></see>) </para>
        /// <para> Note: This Method is slower than <see cref="MarkVoxelAsDirty(int, int, int)"/> or <see cref="MarkVoxelAsDirty(int3)"/>. </para>
        /// </summary>
        public void MarkVoxelAsDirty(int flatIndex)
        {
            NextUpdateChunkBatch.Add(GetChunkByVoxelMapIndex(flatIndex));
        }

        /// <summary>
        /// This will add the <see cref="Chunk"/> in which the voxel at the specified index is to <see cref="NextUpdateChunkBatch"/>
        /// <para> This method will not check wether the voxel is a direct neighbor to another Chunk. (Use <see cref="VoxelObj.SetVoxelFilledValue(int3, bool)"></see>) </para>
        /// </summary>
        public void MarkVoxelAsDirty(int xIndex, int yIndex, int zIndex)
        {
            NextUpdateChunkBatch.Add(GetChunkByVoxelMapIndex(xIndex, yIndex, zIndex));
        }

        /// <summary>
        /// This will add the <see cref="Chunk"/> in which the voxel at the specified index is to <see cref="NextUpdateChunkBatch"/>
        /// <para> This method will not check wether the voxel is a direct neighbor to another Chunk. (Use <see cref="VoxelObj.SetVoxelFilledValue(int3, bool)"></see>) </para>
        /// </summary>
        public void MarkVoxelAsDirty(int3 index3d)
        {
            NextUpdateChunkBatch.Add(GetChunkByVoxelMapIndex(index3d));
        }

        /// <summary>
        /// This will add the <see cref="Chunk"/> at the specified index in the <see cref="Chunks"/> array to <see cref="NextUpdateChunkBatch"/>
        /// </summary>
        public void MarkChunkAsDirty(int x, int y, int z)
        {
            NextUpdateChunkBatch.Add(Chunks[x, y, z]);
        }

        /// <summary>
        /// This will add the <see cref="Chunk"/> at the specified index in the <see cref="Chunks"/> array to <see cref="NextUpdateChunkBatch"/>
        /// </summary>
        public void MarkChunkAsDirty(int3 index)
        {
            NextUpdateChunkBatch.Add(Chunks[index.x, index.y, index.z]);
        }

        public Chunk[] GetChunksInNextUpdateChunkBatch()
        {
            List<Chunk> updateChunkBatch = new List<Chunk>();
            foreach (Chunk c in NextUpdateChunkBatch) updateChunkBatch.Add(c);
            return updateChunkBatch.ToArray();
        }

        /// <summary>
        /// Sets <see cref="VoxelData.Filled"/> to the specified value at the specified index and adds the Chunk in which the voxel is located to <see cref="NextUpdateChunkBatch"/>.
        /// <para>If <see cref="VoxelData.Filled"/> is set to false, it is also checked whether the voxel is a direct neighbor to another Chunk. If so, the neighboring Chunk is also added to <see cref="NextUpdateChunkBatch"/>.</para>
        /// </summary>
        public void SetVoxelFilledValue(int3 index3d, bool filled)
        {
            SetVoxelFilledValue(index3d.x, index3d.y, index3d.z, filled);
        }

        /// <summary>
        /// Sets <see cref="VoxelData.Filled"/> to the specified value at the specified index and adds the Chunk in which the voxel is located to <see cref="NextUpdateChunkBatch"/>.
        /// <para>If <see cref="VoxelData.Filled"/> is set to false, it is also checked whether the voxel is a direct neighbor to another Chunk. If so, the neighboring Chunk is also added to <see cref="NextUpdateChunkBatch"/>.</para>
        /// <para>Note: This method is slower than <see cref="SetVoxelFilledValue(int, int, int, bool)"/> or <see cref="SetVoxelFilledValue(int3, bool)"/>.</para>
        /// </summary>
        public void SetVoxelFilledValue(int flatIndex, bool filled)
        {
            var index3d = VoxelMap.Get3dMapIndex(flatIndex, VoxelMap.dimensions);
            _SetVoxelFilledValue(index3d.x, index3d.y, index3d.z, flatIndex, filled);
        }

        /// <summary>
        /// Sets <see cref="VoxelData.Filled"/> to the specified value at the specified index and adds the Chunk in which the voxel is located to <see cref="NextUpdateChunkBatch"/>.
        /// <para>If <see cref="VoxelData.Filled"/> is set to false, it is also checked whether the voxel is a direct neighbor to another Chunk. If so, the neighboring Chunk is also added to <see cref="NextUpdateChunkBatch"/>.</para>
        /// </summary>
        public void SetVoxelFilledValue(int xIndex, int yIndex, int zIndex, bool filled)
        {
            int flatIndex = VoxelMap.GetFlatMapIndex(xIndex, yIndex, zIndex, VoxelMap.dimensions);
            _SetVoxelFilledValue(xIndex, yIndex, zIndex, flatIndex, filled);
        }

        private void _SetVoxelFilledValue(int x, int y, int z, int flatIndex, bool filled)
        {
            if (VoxelMap.voxelData[flatIndex].Filled == filled)
            {
                return;
            }

            VoxelMap.voxelData[flatIndex].Filled = filled;
            MarkVoxelAsDirty(x, y, z);

            if (filled == false)
            {
                var neighbors = GetChunkBorderVoxelNeighbor(x, y, z);
                foreach (int3 neighbor in neighbors)
                {
                    MarkVoxelAsDirty(neighbor);
                }
            }
        }

        /// <summary>
        /// A voxel located at the edges/outermost layer of a chunk "touches" directly (potentially) neighboring chunks
        /// <para>This method returns the coordinates of neighboring voxels if they are in a different (neighboring) chunk</para>
        /// </summary>
        public int3[] GetChunkBorderVoxelNeighbor(int xIndex, int yIndex, int zIndex)
        {
            int xVoxelIndexInChunk = xIndex % chunkSizeInVoxels;
            bool xIndexIsOnChunkBorder = xVoxelIndexInChunk == 0 || xVoxelIndexInChunk == chunkSizeInVoxels - 1;

            int yVoxelIndexInChunk = yIndex % chunkSizeInVoxels;
            bool yIndexIsOnChunkBorder = yVoxelIndexInChunk == 0 || yVoxelIndexInChunk == chunkSizeInVoxels - 1;

            int zVoxelIndexInChunk = zIndex % chunkSizeInVoxels;
            bool zIndexIsOnChunkBorder = zVoxelIndexInChunk == 0 || zVoxelIndexInChunk == chunkSizeInVoxels - 1;

            List<int3> neighbors = new List<int3>();

            if (xIndexIsOnChunkBorder)
            {
                int neighborXIndex = getNeighborIndex(xVoxelIndexInChunk, xIndex);

                bool outOfBound = neighborXIndex < 0 || neighborXIndex >= VoxelMap.dimensions.x;

                if (outOfBound == false)
                {
                    neighbors.Add(new int3(neighborXIndex, yIndex, zIndex));
                }
            }

            if (yIndexIsOnChunkBorder)
            {
                int neighborYIndex = getNeighborIndex(yVoxelIndexInChunk, yIndex);

                bool outOfBound = neighborYIndex < 0 || neighborYIndex >= VoxelMap.dimensions.y;

                if (outOfBound == false)
                {
                    neighbors.Add(new int3(xIndex, neighborYIndex, zIndex));
                }
            }

            if (zIndexIsOnChunkBorder)
            {
                int neighborZIndex = getNeighborIndex(zVoxelIndexInChunk, zIndex);

                bool outOfBound = neighborZIndex < 0 || neighborZIndex >= VoxelMap.dimensions.z;

                if (outOfBound == false)
                {
                    neighbors.Add(new int3(xIndex, yIndex, neighborZIndex));
                }
            }

            return neighbors.ToArray();

            int getNeighborIndex(int voxelIndexInChunk, int index)
            {
                int neighborIndex;
                if (voxelIndexInChunk == 0)
                {
                    neighborIndex = index - 1;
                }
                else
                {
                    neighborIndex = index + 1;
                }

                return neighborIndex;
            }
        }
        #endregion

        #region VoxelObj update methods

            /// <summary>
            /// Returns the amount of chunks that are going to be updated on the VoxelObjUpdate.
            /// </summary>
        public int GetNextUpdateChunkBatchCunt()
        {
            return NextUpdateChunkBatch.Count;
        }

        /// <summary>
        /// Returns the amount of chunks that are currently updating.
        /// </summary>
        /// <returns></returns>
        public int GeCcurrentlyUpdatingChunksCount()
        {
            return currentlyUpdatingChunks.Count;
        }

        /// <summary>
        /// Starts a non-immediate VoxelObjUpdate (takes at least one frame to complete). This method will add all chunks to <see cref="NextUpdateChunkBatch"/> and then start the update.
        /// </summary>
        /// <param name="doOnCompletion"></param>
        public void FullVoxelObjUpdate(Action doOnCompletion = null)
        {
            if (doOnCompletion != null) AddUpdateCallBack(doOnCompletion);

            AddAllCunksToNextUpdateBatch();
            VoxelObjUpdateInternal(false);
        }

        /// <summary>
        /// Starts an immediate VoxelObjUpdate (finishes within directly, no execution over multible frames). This method will add all chunks to <see cref="NextUpdateChunkBatch"/> and then start the update.
        /// </summary>
        public void FullVoxelObjUpdateImmediate()
        {
            AddAllCunksToNextUpdateBatch();
            VoxelObjUpdateInternal(true);
        }

        public void AddAllCunksToNextUpdateBatch()
        {
            foreach (Chunk c in Chunks) NextUpdateChunkBatch.Add(c);
        }

        /// <summary>
        /// Starts a non-immediate VoxelObjUpdate (takes at least one frame to complete). This method will update all the chunks that are in <see cref="NextUpdateChunkBatch"/>.
        /// </summary>
        /// <param name="doOnCompletion"></param>
        public void VoxelObjUpdate(Action doOnCompletion = null)
        {
            VoxelObjUpdateInternal(false, doOnCompletion);
        }

        /// <summary>
        /// Starts an immediate VoxelObjUpdate (finishes within directly , no execution over multible frames). This method will update all the chunks that are in <see cref="NextUpdateChunkBatch"/>.
        /// </summary>
        public void VoxelObjUpdateImmediate()
        {
            VoxelObjUpdateInternal(true);
        }

        private void AddUpdateCallBack(Action callBack)
        {
            if (UpdatingVoxelObj == false)
            {
                ExecuteWhenUpdateFinished.Enqueue(callBack);
            }
            else ExecuteWhenNextUpdateFinished.Enqueue(callBack);
        }

        private void VoxelObjUpdateInternal(bool immediate, Action doOnCompletion = null)
        {
            if (VoxelMap == null)
            {
                throw new InvalidOperationException($"Could not update VoxelObj: {nameof(VoxelMap)} is null. Call {nameof(VoxelObj.CreateEmptyVoxelMap)} or {nameof(VoxelObj.LoadExistingVoxelMap)} first.");
            }

            if (NextUpdateChunkBatch.Count == 0)
            {
                Debug.LogWarning($"Could not update {nameof(VoxelObj)}: {nameof(NextUpdateChunkBatch)} is empty", this);
                if (doOnCompletion != null) doOnCompletion.Invoke();
                return;
            }

            if (doOnCompletion != null) AddUpdateCallBack(doOnCompletion);

            if (UpdatingVoxelObj) return;
            else UpdatingVoxelObj = true;

            if (this == null)
            {
                throw new InvalidOperationException($"You have called {nameof(VoxelObjUpdate)} on a {nameof(VoxelObj)} that has already been destroyed");
            }

            if (UpdateStarted != null) UpdateStarted.Invoke(this);

            bool generateBoxColliders = MyVoxelObjPhysicsManager.PhysicsEnabled && GenerateColliders;
            bool generateMeshColliders = !generateBoxColliders;

            nativeVoxelMapForMeshJobs = new NativeArray<VoxelData>(VoxelMap.voxelData.Length, Allocator.TempJob);
            nativeVoxelMapForMeshJobs.CopyFrom(VoxelMap.voxelData);

            HashSet<Chunk> startUpdateChunks = NextUpdateChunkBatch;
            NextUpdateChunkBatch = new HashSet<Chunk>();

            if (immediate)
            {
                if(generateBoxColliders)
                {
                    MyVoxelObjPhysicsManager.GenerateBoxCollidersImmediate();
                }

                foreach(Chunk chunk in startUpdateChunks)
                {
                    chunk.UpdateChunkImmediate(nativeVoxelMapForMeshJobs, VoxelMap.dimensions, generateMeshColliders);
                }
                FinishVoxelObjUpdate();
            }
            else
            {
                if (generateBoxColliders)
                {
                    MyVoxelObjPhysicsManager.GenerateBoxColliders();
                    CurrentUpdateStartedBoxColliderGeneration = true;
                }
                else CurrentUpdateStartedBoxColliderGeneration = false;

                foreach (Chunk chunk in startUpdateChunks) currentlyUpdatingChunks.Add(chunk);

                foreach (Chunk chunk in startUpdateChunks)
                {  
                    chunk.UpdateChunk(nativeVoxelMapForMeshJobs, VoxelMap.dimensions, generateMeshColliders, OnNonImmediateChunkUpdateComplete);
                }
            }
        }

        private void OnNonImmediateChunkUpdateComplete(Chunk senderChunk)
        {
            currentlyUpdatingChunks.Remove(senderChunk);
            if (currentlyUpdatingChunks.Count == 0)
            {
                if(CurrentUpdateStartedBoxColliderGeneration)
                {
                    if (MyVoxelObjPhysicsManager.CreatingBoxColliders == false) FinishVoxelObjUpdate();
                    else awatingBoxColliderCompletion = true;
                }
                else FinishVoxelObjUpdate();
            }
        }

        public void OnBoxCollidersGenerated()
        {
            if(awatingBoxColliderCompletion)
            {
                awatingBoxColliderCompletion = false;
                FinishVoxelObjUpdate();
            }
        }

        private void FinishVoxelObjUpdate()
        {
            UpdatingVoxelObj = false;
            nativeVoxelMapForMeshJobs.Dispose();

            if (UpdateFinished != null) UpdateFinished.Invoke(this);

            foreach (Action action in ExecuteWhenUpdateFinished) action.Invoke();
            ExecuteWhenUpdateFinished.Clear();

            foreach (Action action in ExecuteWhenNextUpdateFinished) ExecuteWhenUpdateFinished.Enqueue(action);
            ExecuteWhenNextUpdateFinished.Clear();

            if (NextUpdateChunkBatch.Count > 0 && this != null) VoxelObjUpdate();
        }
        #endregion

        #region Calculation etc

        /// <summary>
        /// slowest
        /// </summary>
        /// <param name="flatIndex"></param>
        /// <returns></returns>
        public Chunk GetChunkByVoxelMapIndex(int flatIndex)
        {
            int3 index3d = VoxelMap.Get3dMapIndex(flatIndex, VoxelMap.dimensions);
            return GetChunkByVoxelMapIndex(index3d.x, index3d.y, index3d.z);
        }

        public Chunk GetChunkByVoxelMapIndex(int3 index3d)
        {
            return GetChunkByVoxelMapIndex(index3d.x, index3d.y, index3d.z);
        }

        public Chunk GetChunkByVoxelMapIndex(int xIndex, int yIndex, int zIndex)
        {
            int xChunkIndex = xIndex / chunkSizeInVoxels;
            int yChunkIndex = yIndex / chunkSizeInVoxels;
            int zChunkIndex = zIndex / chunkSizeInVoxels;

            return Chunks[xChunkIndex, yChunkIndex, zChunkIndex];
        }

        public int3 WorldPosToMapCords(Vector3 worldPos)
        {
            Vector3 localPos = GetVoxelObjHolder().transform.InverseTransformPoint(worldPos);
            return LocalPosToMapCords(localPos);
        }

        public Vector3 MapCordsToWorldPos(int3 mapCords)
        {
            Vector3 localPos = new Vector3(mapCords.x * voxelSize, mapCords.y * voxelSize, mapCords.z * voxelSize);
            Vector3 worldPos = GetVoxelObjHolder().transform.TransformPoint(localPos);
            return worldPos;
        }

        public Vector3 MapCordsToLocalPos(int3 mapCords)
        {
            Vector3 localPos = new Vector3(mapCords.x * voxelSize, mapCords.y * voxelSize, mapCords.z * voxelSize);
            return localPos;
        }

        public int3 LocalPosToMapCords(Vector3 localPos)
        {
            return new int3((int)(localPos.x / voxelSize), (int)(localPos.y / voxelSize), (int)(localPos.z / voxelSize));
        }

        /// <summary>
        /// Loops through the entire <see cref="VoxelMap"/> and returns the amount of Voxels where <see cref="VoxelData.Filled"/> = true
        /// </summary>
        public int GetMapVoxelCount()
        {
            int voxelCount = 0;
            foreach (VoxelData voxel in VoxelMap.voxelData) if (voxel.Filled) voxelCount += 1;
            return voxelCount;
        }

        /// <summary>
        /// Loops through all chunks and returns the sum of all <see cref="Chunk.CurrentlyRenderedVoxelCount"/>
        /// </summary>
        public int GetCurrentlyRenderedVoxelCount()
        {
            int voxelCount = 0;
            foreach(Chunk chunk in Chunks)
            {
                voxelCount += chunk.CurrentlyRenderedVoxelCount;
            }
            return voxelCount;
        }

        /// <summary>
        /// Loops through all chunks and returns the sum of all <see cref="Chunk.CurrentlyRenderedVoxelCount"/>
        /// </summary>
        public long GetCurrentlyRenderedVoxelCountLong()
        {
            long voxelCount = 0;
            foreach (Chunk chunk in Chunks)
            {
                voxelCount += chunk.CurrentlyRenderedVoxelCount;
            }
            return voxelCount;
        }
        #endregion
    }
}