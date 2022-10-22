using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.VoroniStandard.Jobs
{
    [BurstCompile]
    public struct VoxelMapEdgeDestroyerJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<VoxelData> voxelMapInput;
        [ReadOnly] public int3 voxelMapMapDimensions;

        [NativeDisableParallelForRestriction] public NativeArray<VoxelData> voxelMapOutput;
        public void Execute(int index)
        {
            if (voxelMapInput[index].Filled == false) return;

            int3 index3d = VoxelMap.Get3dMapIndex(index, voxelMapMapDimensions);

            int neighborCount = 0;

            for (int i = 0; i != 6; i++)
            {
                int3 offset = VoxelLookupTable.neighborVoxelIndexOffsets[i];
                int3 neighborIndex = index3d + offset;
                if (VoxelMap.Index3dOutsideOfBounds(neighborIndex, voxelMapMapDimensions)) continue;

                int flatNeighborIndex = VoxelMap.GetFlatMapIndex(neighborIndex, voxelMapMapDimensions);
                if (voxelMapInput[flatNeighborIndex].Filled) neighborCount += 1;
            }

            if (neighborCount <= 3)
            {
                VoxelData data = voxelMapOutput[index];
                data.Filled = false;
                voxelMapOutput[index] = data;
            }
            else voxelMapOutput[index] = voxelMapInput[index];
        }
    }
}
