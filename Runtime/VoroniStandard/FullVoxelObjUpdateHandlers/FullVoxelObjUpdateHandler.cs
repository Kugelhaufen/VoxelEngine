using System;
using UnityEngine;

namespace VoxelEngine.VoroniStandard
{
    public abstract class FullVoxelObjUpdateHandler : ScriptableObject
    {
        public abstract void StartFullVoxelObjUpdate(VoxelObj voxelObj, Action callBack = null);
    }
}