using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm.Jobs
{
    [BurstCompile]
    public struct CclPiLabelEquivalenceFinderJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> labelMapInput;
        [ReadOnly] public NativeArray<int> zLineLabelOffsets;
        [ReadOnly] public int3 labelMapDemensions;

        [WriteOnly] public NativeQueue<int2>.ParallelWriter unionMergeCalls;

        public void Execute(int XYIndex)
        {
            int xIndex = XYIndex / labelMapDemensions.y;
            int yIndex = XYIndex - xIndex * labelMapDemensions.y;

            if (yIndex + 1 >= labelMapDemensions.y && xIndex + 1 >= labelMapDemensions.x) return;

            int zRowStartIndex = VoxelMap.GetFlatMapIndex(xIndex, yIndex, 0, labelMapDemensions);
            int northNeighborStartIndex = VoxelMap.GetFlatMapIndex(xIndex, yIndex + 1, 0, labelMapDemensions);
            int eastNeighborStartIndex = VoxelMap.GetFlatMapIndex(xIndex + 1, yIndex, 0, labelMapDemensions);

            int2 lastNorthmergecall = int2.zero;
            int2 lastEastmergecall = int2.zero;

            for (int zIndex = 0; zIndex != labelMapDemensions.z; zIndex++)
            {
                int currentIndex = zRowStartIndex + zIndex;

                if (labelMapInput[currentIndex] > 0)
                {
                    if (yIndex + 1 < labelMapDemensions.y)
                    {
                        if (labelMapInput[northNeighborStartIndex + zIndex] > 0)
                        {
                            int2 mergeCall = new int2(labelMapInput[currentIndex] + zLineLabelOffsets[XYIndex], labelMapInput[northNeighborStartIndex + zIndex] + zLineLabelOffsets[XYIndex + 1]);
                            if (mergeCall.x != lastNorthmergecall.x || mergeCall.y != lastNorthmergecall.y)
                            {
                                unionMergeCalls.Enqueue(mergeCall);
                                lastNorthmergecall = mergeCall;
                            }
                        }
                    }

                    if (xIndex + 1 < labelMapDemensions.x)
                    {
                        if (labelMapInput[eastNeighborStartIndex + zIndex] > 0)
                        {
                            int2 mergeCall = new int2(labelMapInput[currentIndex] + zLineLabelOffsets[XYIndex], labelMapInput[eastNeighborStartIndex + zIndex] + zLineLabelOffsets[(xIndex + 1) * (labelMapDemensions.y) + yIndex]);
                            if (mergeCall.x != lastEastmergecall.x || mergeCall.y != lastEastmergecall.y)
                            {
                                unionMergeCalls.Enqueue(mergeCall);
                                lastEastmergecall = mergeCall;
                            }
                        }
                    }
                }
            }
        }
    }
}
