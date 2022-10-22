using VoxelEngine.ConnectedComponent;
using System;
using UnityEngine;

namespace VoxelEngine.VoroniStandard
{
    [CreateAssetMenu(fileName = "Immediate Connected Component Full VoxelObj Update Handler", menuName = "Voxel Engine/Voroni Standard/Full VoxeObj Updater Handlers/Immediate Connected Component Full VoxelObj Update Handler", order = 2)]
    public class ImmediateConnectedComponentFullVoxelObjUpdateHandler : FullVoxelObjUpdateHandler
    {
        public VoxelObjUpdateHandler FallBackConnectedComponentIsNull;
        public override void StartFullVoxelObjUpdate(VoxelObj voxelObj, Action callBack = null)
        {
            ConnectedComponentPhysics cclPhysics = voxelObj.GetComponent<ConnectedComponentPhysics>();

            if (cclPhysics != null)
            {
                cclPhysics.ConnectedComponentFullVoxelObjUpdateImmediate(callBack);
            }
            else
            {
                FallBackConnectedComponentIsNull.StartVoxelObjUpdate(voxelObj, callBack);
            }
        }
    }
}