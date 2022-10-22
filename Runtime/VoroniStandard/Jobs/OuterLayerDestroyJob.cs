using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.VoroniStandard.Jobs
{
    [BurstCompile]
    public struct OuterLayerDestroyJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<VoxelData> voxelMapInput;
        [ReadOnly] public int3 voxelMapMapDimensions;

        [NativeDisableParallelForRestriction] public NativeArray<VoxelData> voxelMapOutput;

        public void Execute(int i)
        {
            int3 index = VoxelMap.Get3dMapIndex(i, voxelMapMapDimensions);

            if (index.x < 0 || index.y < 0 || index.z < 0) return;
            if (index.x >= voxelMapMapDimensions.x || index.y >= voxelMapMapDimensions.y || index.z >= voxelMapMapDimensions.z) return;

            int flatIndex = VoxelMap.GetFlatMapIndex(index.x, index.y, index.z, voxelMapMapDimensions);
            voxelMapOutput[flatIndex] = voxelMapInput[flatIndex];
            if (voxelMapInput[flatIndex].Filled == false) return;

            foreach (int3 offset in VoxelLookupTable.neighborVoxelIndexOffsets)
            {
                int3 neighborIndex = index - offset;
                bool smallerAsBounds = (neighborIndex.x < 0 || neighborIndex.y < 0 || neighborIndex.z < 0);
                bool biggerAsBounds = (neighborIndex.x >= voxelMapMapDimensions.x || neighborIndex.y >= voxelMapMapDimensions.y || neighborIndex.z >= voxelMapMapDimensions.z);
                bool neighborOutsideBounds = (smallerAsBounds || biggerAsBounds);

                if(neighborOutsideBounds)
                {
                    VoxelData data = voxelMapInput[flatIndex];
                    data.Filled = false;
                    voxelMapOutput[flatIndex] = data;
                    continue;
                }
                else
                {
                    int flatNeighborIndex = VoxelMap.GetFlatMapIndex(neighborIndex.x, neighborIndex.y, neighborIndex.z, voxelMapMapDimensions);

                    if (voxelMapInput[flatNeighborIndex].Filled == false)
                    {
                        VoxelData data = voxelMapInput[flatIndex];
                        data.Filled = false;
                        voxelMapOutput[flatIndex] = data;
                    }
                }
            }
        }
    }
}
