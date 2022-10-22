using System;
using UnityEngine;

namespace VoxelEngine.VoroniStandard
{
    [CreateAssetMenu(fileName = "Standard VoxelObj Update Handler", menuName = "Voxel Engine/Voroni Standard/VoxeObj Updater Handlers/Standard VoxelObj Update Handler", order = 0)]
    public class StandardVoxelObjUpdateHandler : VoxelObjUpdateHandler
    {
        public override void StartVoxelObjUpdate(VoxelObj voxelObj, Action callBack = null)
        {
            voxelObj.VoxelObjUpdate(callBack);
        }
    }
}