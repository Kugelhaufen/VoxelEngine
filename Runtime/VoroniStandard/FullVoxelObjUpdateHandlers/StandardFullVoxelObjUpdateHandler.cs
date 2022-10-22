using System;
using UnityEngine;

namespace VoxelEngine.VoroniStandard
{
    [CreateAssetMenu(fileName = "Standard Full VoxelObj Update Handler", menuName = "Voxel Engine/Voroni Standard/Full VoxeObj Updater Handlers/Standard Full VoxelObj Update Handler", order = 0)]
    public class StandardFullVoxelObjUpdateHandler : FullVoxelObjUpdateHandler
    {
        public override void StartFullVoxelObjUpdate(VoxelObj voxelObj, Action callBack = null)
        {
            voxelObj.FullVoxelObjUpdate(callBack);
        }
    }
}