using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine.ConnectedComponent
{
    [AddComponentMenu("VoxelEngine/" + nameof(VoxelObjAmountManager))]
    public class VoxelObjAmountManager : MonoBehaviour
    {
        private static VoxelObjAmountManager instance;
        public static VoxelObjAmountManager Instance { get => instance; }

        public int maxManagedBoxColliderCount = 3500;
        public int hysteresis = 0;
        public bool autoAddCclVoxelobjs;
        public int addCclObjIfVoxelcountIsSmaller = 300;

        private Queue<VoxelObj> managedVoxelObjs = new Queue<VoxelObj>();
        bool waitingForObjDestruction = false;
        bool manageAfterDestruction = false;

        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            ConnectedComponentPhysics.anyCclNewVoxelObjsUpdateCompletion += OnAnyCclComplete;
        }

        public static void CreateInstance()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("VoxelObjAmountManager");
                instance = go.AddComponent<VoxelObjAmountManager>();
            }
        }

        private void OnAnyCclComplete(VoxelObj[] newVoxelObjs)
        {
            if (autoAddCclVoxelobjs == false) return;

            foreach (VoxelObj voxelObj in newVoxelObjs)
            {
                if (voxelObj.GetCurrentlyRenderedVoxelCount() < addCclObjIfVoxelcountIsSmaller)
                {
                    AddVoxelObjToManagement(voxelObj);
                }
            }
        }

        public void AddVoxelObjToManagement(VoxelObj voxelObj)
        {
            managedVoxelObjs.Enqueue(voxelObj);
            manageVoxelObjAmount();
        }

        private void manageVoxelObjAmount()
        {
            if (this.isActiveAndEnabled == false) return;
            if (waitingForObjDestruction)
            {
                manageAfterDestruction = true;
                return;
            }

            int totalBoxColliderAmount = VoxelObjPhysicsManager.TotalBoxColliderAmount;

            if (totalBoxColliderAmount <= maxManagedBoxColliderCount) return;

            VoxelObj last = null;
            while (totalBoxColliderAmount > maxManagedBoxColliderCount - hysteresis)
            {
                if (managedVoxelObjs.Count == 0) break;

                VoxelObj obj = managedVoxelObjs.Dequeue();
                if (obj == null) continue;

                totalBoxColliderAmount -= obj.MyVoxelObjPhysicsManager.MyBoxColliderAmount;
                Destroy(obj.GetVoxelObjHolder().gameObject);
                last = obj;
            }
            WaitForObjDestruction(last);
        }

        private void WaitForObjDestruction(VoxelObj obj)
        {
            StartCoroutine(waitForDestruction());
            IEnumerator waitForDestruction()
            {
                waitingForObjDestruction = true;
                yield return new WaitUntil(() => (obj == null));
                waitingForObjDestruction = false;

                if (manageAfterDestruction)
                {
                    manageAfterDestruction = false;
                    manageVoxelObjAmount();
                }
            }
        }
    }
}