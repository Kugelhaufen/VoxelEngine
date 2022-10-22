using System;
using UnityEngine;

namespace VoxelEngine.VoroniStandard
{
    [CreateAssetMenu(fileName = "Immediate Full VoxelObj Update Handler", menuName = "Voxel Engine/Voroni Standard/Full VoxeObj Updater Handlers/Immediate Full VoxelObj Update Handler", order = 1)]
    public class ImmediateFullVoxelObjUpdateHandler : FullVoxelObjUpdateHandler
    {
        public override void StartFullVoxelObjUpdate(VoxelObj voxelObj, Action callBack = null)
        {
            voxelObj.FullVoxelObjUpdateImmediate();
            callBack();
        }
    }
}