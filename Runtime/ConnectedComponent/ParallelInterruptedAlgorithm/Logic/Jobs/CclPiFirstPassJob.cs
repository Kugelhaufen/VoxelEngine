using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm.Jobs
{
    [BurstCompile]
    public struct CclPiFirstPassJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<VoxelData> voxelMapInput;
        [ReadOnly] public int3 voxelMapDimensions;

        [NativeDisableParallelForRestriction] public NativeArray<int> labelMapOutput;
        [NativeDisableParallelForRestriction] public NativeArray<int> labelsInLine;

        public void Execute(int XYIndex)
        {
            int xIndex = XYIndex / voxelMapDimensions.y;
            int yIndex = XYIndex - xIndex * voxelMapDimensions.y;
            int zRowStartIndex = VoxelMap.GetFlatMapIndex(xIndex, yIndex, 0, voxelMapDimensions);

            int labelsFound = 0;
            bool lastVoxelWasEmpty = true;
            for (int zIndex = 0; zIndex != voxelMapDimensions.z; zIndex++)
            {
                int currentIndex = zRowStartIndex + zIndex;

                if (voxelMapInput[currentIndex].Filled)
                {
                    if (lastVoxelWasEmpty == true)
                    {
                        labelsFound += 1;
                        labelMapOutput[currentIndex] = labelsFound;
                        lastVoxelWasEmpty = false;
                    }
                    else labelMapOutput[currentIndex] = labelsFound;
                }
                else lastVoxelWasEmpty = true;
            }
            labelsInLine[XYIndex] = labelsFound;
        }
    }
}
