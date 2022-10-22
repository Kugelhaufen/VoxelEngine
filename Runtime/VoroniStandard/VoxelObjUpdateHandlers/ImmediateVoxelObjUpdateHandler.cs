using System;
using UnityEngine;

namespace VoxelEngine.VoroniStandard
{
    [CreateAssetMenu(fileName = "Immediate VoxelObj Update Handler", menuName = "Voxel Engine/Voroni Standard/VoxeObj Updater Handlers/Immediate VoxelObj Update Handler", order = 1)]
    public class ImmediateVoxelObjUpdateHandler : VoxelObjUpdateHandler
    {
        public override void StartVoxelObjUpdate(VoxelObj voxelObj, Action callBack = null)
        {
            voxelObj.VoxelObjUpdateImmediate();
            callBack();
        }
    }
}