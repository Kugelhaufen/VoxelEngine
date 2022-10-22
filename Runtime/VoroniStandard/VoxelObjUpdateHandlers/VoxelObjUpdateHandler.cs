using System;
using UnityEngine;

namespace VoxelEngine.VoroniStandard
{
    public abstract class VoxelObjUpdateHandler : ScriptableObject
    {
        public abstract void StartVoxelObjUpdate(VoxelObj voxelObj, Action callBack = null);
    }
}