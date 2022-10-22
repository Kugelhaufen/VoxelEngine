using System;

namespace VoxelEngine
{
    public class VoxelReplaceUpdateData
    {
        public delegate void VoxelObjUpdateHandler(Action callBack = null);

        public VoxelObj ExistingVoxelObj { get; set; }
        public VoxelObjUpdateHandler ExistingVoxelObjUpdate { get; set; }
        public VoxelObj[] NewVoxelObjs { get; set; }
        public VoxelObjUpdateHandler[] NewVoxelObjUpdates { get; set; }
    }
}