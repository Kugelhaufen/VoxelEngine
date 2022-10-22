using UnityEngine;

namespace VoxelEngine
{
    [RequireComponent(typeof(VoxelObj))]
    public class VoxelObjVoxelCountDestroyer : MonoBehaviour
    {
        public int deleteAtThisVoxelCount = 0;
        private VoxelObj myVoxelObj;

        private void Awake()
        {
            myVoxelObj = GetComponent<VoxelObj>();

            if (this.enabled)
            {
                myVoxelObj.UpdateFinished += CheckVoxelCount;
            }
        }

        private void OnEnable()
        {
            myVoxelObj.UpdateFinished += CheckVoxelCount;
        }

        private void OnDisable()
        {
            myVoxelObj.UpdateFinished -= CheckVoxelCount;
        }

        private void CheckVoxelCount(VoxelObj sender)
        {
            int voxelCount = myVoxelObj.GetCurrentlyRenderedVoxelCount();
            if (voxelCount <= deleteAtThisVoxelCount)
            {
                if(myVoxelObj != null)
                {
                    Destroy(myVoxelObj.GetVoxelObjHolder().gameObject);
                }
            }
        }
    }
}