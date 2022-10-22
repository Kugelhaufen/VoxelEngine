using System;
using UnityEngine;

namespace VoxelEngine
{
    public abstract class VoxelReplaceUpdater : ScriptableObject
    {
        public abstract void ReplaceUpdate(VoxelObj existingVoxelObj, Action applyExistingVoxelObjChanges, VoxelObj[] newVoxelObjs, Action callOnCompletion = null);
        public abstract void ReplaceUpdate(VoxelReplaceUpdateData updateData, Action applyExistingVoxelObjChanges, Action callOnCompletion);

        public void SetDefaultReplaceDataValues(VoxelReplaceUpdateData data, VoxelObj existingVoxelObj, VoxelObj[] newVoxelObjs)
        {
            SetDefaultReplaceDataValues(data, newVoxelObjs);
            SetDefaultReplaceDataValues(data, existingVoxelObj);
        }

        public void SetDefaultReplaceDataValues(VoxelReplaceUpdateData data, VoxelObj existingVoxelObj)
        {
            data.ExistingVoxelObj = existingVoxelObj;
            data.ExistingVoxelObjUpdate = existingVoxelObj.VoxelObjUpdate;
        }

        public void SetDefaultReplaceDataValues(VoxelReplaceUpdateData data, VoxelObj[] newVoxelObjs)
        {
            VoxelReplaceUpdateData.VoxelObjUpdateHandler[] newObjUdpateMethods = new VoxelReplaceUpdateData.VoxelObjUpdateHandler[newVoxelObjs.Length];
            for (int i = 0; i < newVoxelObjs.Length; i++)
            {
                newObjUdpateMethods[i] = newVoxelObjs[i].FullVoxelObjUpdate;
            }

            data.NewVoxelObjs = newVoxelObjs;
            data.NewVoxelObjUpdates = newObjUdpateMethods;
        }
    }
}