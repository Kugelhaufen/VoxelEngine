using System;
using System.Collections.Generic;

namespace VoxelEngine
{
    /// <summary>
    /// This class can be used to update multible <see cref="VoxelObj"/>s and get a callback when all updates are complete
    /// </summary>
    public class MultibleVoxelObjUpdater
    {
        public void FullVoxelObjUpdate(IEnumerable<VoxelObj> voxelObjs, Action callOnCompletion)
        {
            List<VoxelObj> _voxelObjs = new List<VoxelObj>();

            foreach (VoxelObj voxelObj in voxelObjs)
            {
                _voxelObjs.Add(voxelObj);
            }

            int amountCurrentlyUpdatingVoxelObjs = _voxelObjs.Count;

            foreach (VoxelObj voxelObj in _voxelObjs)
            {
                voxelObj.FullVoxelObjUpdate(onUpdateCompletion);
            }

            void onUpdateCompletion()
            {
                amountCurrentlyUpdatingVoxelObjs -= 1;

                if (amountCurrentlyUpdatingVoxelObjs != 0) return;
                else if (callOnCompletion != null) callOnCompletion.Invoke();
            }
        }

        public void VoxelObjUpdate(IEnumerable<VoxelObj> voxelObjs, Action callOnCompletion)
        {
            List<VoxelObj> _voxelObjs = new List<VoxelObj>();
            foreach (VoxelObj voxelObj in voxelObjs) _voxelObjs.Add(voxelObj);

            int amountCurrentlyUpdatingVoxelObjs = _voxelObjs.Count;

            for (int i = 0; i < _voxelObjs.Count; i++)
            {
                VoxelObj voxelObj = _voxelObjs[i];
                voxelObj.VoxelObjUpdate(onUpdateCompletion);
            }

            void onUpdateCompletion()
            {
                amountCurrentlyUpdatingVoxelObjs -= 1;

                if (amountCurrentlyUpdatingVoxelObjs != 0) return;
                else if (callOnCompletion != null) callOnCompletion.Invoke();
            }

        }
    }
}
