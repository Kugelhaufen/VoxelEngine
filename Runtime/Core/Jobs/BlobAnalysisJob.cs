using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.Jobs
{
    [BurstCompile]
    public struct BlobAnalysisJob : IJob
    {
        /// <summary>
        /// Label values (except <see cref="emptyLabelValueInput"/>) must be >= 0 as the Label value is used as index for <see cref="voxelsInBlob"/>.
        /// </summary>
        [ReadOnly] public NativeArray<int> labelMapInput;
        [ReadOnly] public int3 labelMapDemensionsInput;
        [ReadOnly] public int emptyLabelValueInput;

        public NativeArray<int3> smallestBlobAxisValues;
        public NativeArray<int3> biggestBlobAxisValues;
        public NativeArray<int> voxelsInBlob;
        public NativeArray<int> largestBlobLabel;

        public void Execute()
        {
            for (int i = 0; i != smallestBlobAxisValues.Length; i++)
            {
                smallestBlobAxisValues[i] = labelMapDemensionsInput;
            }

            for (int i = 0; i != labelMapInput.Length; i++)
            {
                if (labelMapInput[i] == emptyLabelValueInput) continue;

                int blob = labelMapInput[i];
                voxelsInBlob[blob] += 1;

                int3 mapIndex3d = VoxelMap.Get3dMapIndex(i, labelMapDemensionsInput);
                int3 newSmallestValues = new int3(smallestBlobAxisValues[blob].x, smallestBlobAxisValues[blob].y, smallestBlobAxisValues[blob].z);
                int3 newBiggestValues = new int3(biggestBlobAxisValues[blob].x, biggestBlobAxisValues[blob].y, biggestBlobAxisValues[blob].z);

                if (mapIndex3d.x < smallestBlobAxisValues[blob].x) newSmallestValues.x = mapIndex3d.x;
                if (mapIndex3d.x > biggestBlobAxisValues[blob].x) newBiggestValues.x = mapIndex3d.x;

                if (mapIndex3d.y < smallestBlobAxisValues[blob].y) newSmallestValues.y = mapIndex3d.y;
                if (mapIndex3d.y > biggestBlobAxisValues[blob].y) newBiggestValues.y = mapIndex3d.y;

                if (mapIndex3d.z < smallestBlobAxisValues[blob].z) newSmallestValues.z = mapIndex3d.z;
                if (mapIndex3d.z > biggestBlobAxisValues[blob].z) newBiggestValues.z = mapIndex3d.z;

                smallestBlobAxisValues[blob] = newSmallestValues;
                biggestBlobAxisValues[blob] = newBiggestValues;
            }

            largestBlobLabel[0] = 0;
            for (int i = 1; i != voxelsInBlob.Length; i++)
            {
                if (voxelsInBlob[i] > voxelsInBlob[largestBlobLabel[0]]) largestBlobLabel[0] = i;
            }
        }
    }
}