using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm.Jobs
{
    [BurstCompile]
    public struct CclPiReLabelJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<int> labelMap;
        [ReadOnly] public int3 labelMapDemensions;
        [ReadOnly] public NativeArray<int> zLineLabelOffsets;
        [ReadOnly] public NativeArray<int> unionFindPointers;
        [ReadOnly] public NativeArray<int> newLabels;

        public void Execute(int XYIndex)
        {
            int xIndex = XYIndex / labelMapDemensions.y;
            int yIndex = XYIndex - xIndex * labelMapDemensions.y;
            int zRowStartIndex = VoxelMap.GetFlatMapIndex(xIndex, yIndex, 0, labelMapDemensions);

            for (int zIndex = 0; zIndex != labelMapDemensions.z; zIndex++)
            {
                int currentIndex = zRowStartIndex + zIndex;
                if (labelMap[currentIndex] > 0) labelMap[currentIndex] = newLabels[UnionFindRoot(labelMap[currentIndex] + zLineLabelOffsets[XYIndex])];
            }
        }

        private int UnionFindRoot(int pointer)
        {
            int rootPointer = pointer;
            while (rootPointer != unionFindPointers[rootPointer]) rootPointer = unionFindPointers[rootPointer];
            return rootPointer;
        }
    }
}
