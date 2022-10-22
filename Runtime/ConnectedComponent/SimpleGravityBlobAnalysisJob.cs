using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.ConnectedComponent
{
    /// <summary>
    /// Finds the blob that has the most voxels in the lowest (not empty) Y Layer in the VoxelMap;
    /// </summary>
    [BurstCompile]
    public struct SimpleGravityBlobAnalysisJob : IJob
    {
        /// <summary>
        /// Label values (except <see cref="emptyLabelValueInput"/>) must be >= 0 as the Label value is used as index for <see cref="voxelsInBlob"/>.
        /// </summary>
        [ReadOnly] public NativeArray<int> labelMapInput;
        [ReadOnly] public int3 labelMapDemensionsInput;
        [ReadOnly] public int emptyLabelValueInput;
        [ReadOnly] public int blobCountInput;

        public NativeArray<int> mostGroundedBlob;

        public void Execute()
        {
            NativeParallelHashMap<int, int> blobIdToCount = new NativeParallelHashMap<int, int>(blobCountInput, Allocator.Temp);
            int mostVoxelsBlobId = emptyLabelValueInput;
            int mostVoxelsBlobVoxelCount = 0;

            for (int yLayer = 0; yLayer < labelMapDemensionsInput.y; yLayer++)
            {
                for (int x = 0; x < labelMapDemensionsInput.x; x++)
                {
                    for (int z = 0; z < labelMapDemensionsInput.z; z++)
                    {
                        int flatIndex = VoxelMap.GetFlatMapIndex(x, yLayer, z, labelMapDemensionsInput);
                        if (labelMapInput[flatIndex] == emptyLabelValueInput)
                        {
                            continue;
                        }

                        int blobId = labelMapInput[flatIndex];
                        int newCount;
                        if (blobIdToCount.ContainsKey(blobId))
                        {
                            newCount = blobIdToCount[blobId] + 1;
                            blobIdToCount[blobId] = newCount;

                        }
                        else
                        {
                            newCount = 1;
                            blobIdToCount.Add(blobId, newCount);
                        }

                        if (newCount > mostVoxelsBlobVoxelCount)
                        {
                            mostVoxelsBlobId = blobId;
                            mostVoxelsBlobVoxelCount = newCount;
                        }
                    }
                }
                
                bool voxelsInCurrentYlayer = mostVoxelsBlobVoxelCount > 0;
                if (voxelsInCurrentYlayer)
                {
                    mostGroundedBlob[0] = mostVoxelsBlobId;
                    return;
                }
            }
        }
    }
}