using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm.Jobs
{
    [BurstCompile]
    public struct CclZLineLabelOffsetCreatorJob : IJob
    {
        [ReadOnly] public NativeArray<int> labelAmountPerLine;

        [WriteOnly] public NativeArray<int> zLineLabelOffsets;
        public NativeArray<int> labelsFoundTotal;

        public void Execute()
        {
            for (int i = 0; i != labelAmountPerLine.Length; i++)
            {
                zLineLabelOffsets[i] = labelsFoundTotal[0];
                labelsFoundTotal[0] += labelAmountPerLine[i];
            }
        }
    }
}
