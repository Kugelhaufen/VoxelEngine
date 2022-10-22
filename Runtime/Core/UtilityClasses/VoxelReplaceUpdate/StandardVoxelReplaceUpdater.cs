using System;
using UnityEngine;

namespace VoxelEngine
{
    [CreateAssetMenu(fileName = "Standard Voxel Replace Updater", menuName = "Voxel Engine/Core/Voxel Replace Updaters/Standard Voxel Replace Updater", order = 1)]
    public class StandardVoxelReplaceUpdater : VoxelReplaceUpdater
    {
        public override void ReplaceUpdate(VoxelObj existingVoxelObj, Action applyExistingVoxelObjChanges, VoxelObj[] newVoxelObjs, Action callOnCompletion = null)
        {
            VoxelReplaceUpdateData data = new VoxelReplaceUpdateData();
            SetDefaultReplaceDataValues(data, existingVoxelObj, newVoxelObjs);
            ReplaceUpdate(data, applyExistingVoxelObjChanges, callOnCompletion);
        }

        public override void ReplaceUpdate(VoxelReplaceUpdateData updateData, Action applyExistingVoxelObjChanges, Action callOnCompletion)
        {
            int amountCurrentlyUpdatingVoxelObjs = updateData.NewVoxelObjs.Length + 1;

            for (int i = 0; i < updateData.NewVoxelObjs.Length; i++)
            {
                updateData.NewVoxelObjUpdates[i](updateCompletion);
            }

            applyExistingVoxelObjChanges();
            if(updateData.ExistingVoxelObj.GetNextUpdateChunkBatchCunt() > 0)
            {
                updateData.ExistingVoxelObjUpdate(updateCompletion);
            }
            else
            {
                updateCompletion();
            }

            void updateCompletion()
            {
                amountCurrentlyUpdatingVoxelObjs -= 1;

                if (amountCurrentlyUpdatingVoxelObjs != 0) return;
                else if (callOnCompletion != null) callOnCompletion.Invoke();
            }
        }
    }
}