using VoxelEngine.ConnectedComponent;
using System;
using UnityEngine;

namespace VoxelEngine.VoroniStandard
{
    [CreateAssetMenu(fileName = "Connected Component Full VoxelObj Update Handler", menuName = "Voxel Engine/Voroni Standard/Full VoxeObj Updater Handlers/Connected Component Full VoxelObj Update Handler", order = 3)]
    public class ConnectedComponentFullVoxelObjUpdateHandler : FullVoxelObjUpdateHandler
    {
        public VoxelObjUpdateHandler fallBackConnectedComponentIsNull;

        public override void StartFullVoxelObjUpdate(VoxelObj voxelObj, Action callBack = null)
        {
            ConnectedComponentPhysics cclPhysics = voxelObj.GetComponent<ConnectedComponentPhysics>();

            if (cclPhysics != null)
            {
                cclPhysics.ConnectedComponentFullVoxelObjUpdate(callBack);
            }
            else
            {
                fallBackConnectedComponentIsNull.StartVoxelObjUpdate(voxelObj, callBack);
            }
        }
    }
}