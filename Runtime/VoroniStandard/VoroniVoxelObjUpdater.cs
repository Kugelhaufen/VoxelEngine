using System;

namespace VoxelEngine.VoroniStandard
{
    public class VoroniVoxelObjUpdater
    {
        public VoroniVoxelObjUpdaterSettings Settings { get; set; }

        public void Update(VoxelObj existingVoxelObj, Action applyExistingVoxelObjChanges, VoxelObj[] newVoxelObjs, Action callOnCompletion = null)
        {
            VoxelReplaceUpdateData.VoxelObjUpdateHandler[] newObjsUpdateHandlers = new VoxelReplaceUpdateData.VoxelObjUpdateHandler[newVoxelObjs.Length];
            for(int i = 0; i < newVoxelObjs.Length; i++)
            {
                VoxelObj shrapnelObj = newVoxelObjs[i];
                newObjsUpdateHandlers[i] = 
                    (Action callBack) => 
                    {
                        Settings.shrapnelVoxelObjUpdateHandler.StartFullVoxelObjUpdate(shrapnelObj, callBack);
                    };
            }

            VoxelReplaceUpdateData voxelReplaceUpdateData = new VoxelReplaceUpdateData()
            {
                ExistingVoxelObj = existingVoxelObj,
                ExistingVoxelObjUpdate =
                    (Action callBack) =>
                    {
                        Settings.existingVoxelObjUpdateHandler.StartVoxelObjUpdate(existingVoxelObj, callBack);
                    },
                NewVoxelObjs = newVoxelObjs,
                NewVoxelObjUpdates = newObjsUpdateHandlers,
            };

            Settings.voxelReplaceUpdater.ReplaceUpdate(voxelReplaceUpdateData, applyExistingVoxelObjChanges, callOnCompletion);
        }
    }
}