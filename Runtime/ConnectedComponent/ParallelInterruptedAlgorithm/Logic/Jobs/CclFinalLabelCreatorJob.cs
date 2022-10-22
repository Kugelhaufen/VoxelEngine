using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm.Jobs
{
    [BurstCompile]
    public struct CclFinalLabelCreatorJob : IJob
    {
        public NativeArray<int> unionFindPointers;

        public NativeArray<int> newLabels;
        public NativeArray<int> finalLabelAmount;

        public void Execute()
        {
            for (int i = 1; i != newLabels.Length; i++)
            {
                if (unionFindPointers[i] == i)
                {
                    finalLabelAmount[0] += 1;
                    newLabels[i] = finalLabelAmount[0];
                }
            }
        }
    }
}
