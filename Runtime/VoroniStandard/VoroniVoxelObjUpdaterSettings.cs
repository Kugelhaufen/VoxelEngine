namespace VoxelEngine.VoroniStandard
{
    [System.Serializable]
    public class VoroniVoxelObjUpdaterSettings
    {
        public VoxelReplaceUpdater voxelReplaceUpdater;
        public VoxelObjUpdateHandler existingVoxelObjUpdateHandler;
        public FullVoxelObjUpdateHandler shrapnelVoxelObjUpdateHandler;
    }
}