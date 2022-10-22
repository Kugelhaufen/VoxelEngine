using System;
using Unity.Collections;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm
{
    public interface IPiBlobExtractor
    {
        public delegate void CallBack(VoxelObj[] newVoxelObjs, bool originalVoxelObjEdited, Action applyOriginalVoxelObjMapEdit);

        /// <summary>
        /// Extracts new VoxelObjs from <paramref name="voxelObj"/> according to the labelMap. Get new (extracted) VoxelObjs using <paramref name="callBack"/>.
        /// </summary>
        /// <param name="callBack">Is called when the extraction is complete.</param>
        void LabelMapExtraction(VoxelObj voxelObj, NativeArray<int> labelMap, int blobAmount, int minVoxelsForNewExtraction, CallBack callBack);
        /// <summary>
        /// Extracts new VoxelObjs from <paramref name="voxelObj"/> according to the labelMap. Get new (extracted) VoxelObjs using <paramref name="callBack"/>.
        /// </summary>
        /// <param name="callBack">Is called when the extraction is complete.</param>
        void LabelMapExtractionImmediate(VoxelObj voxelObj, NativeArray<int> labelMap, int blobAmount, int minVoxelsForNewExtraction, CallBack callBack);
    }
}