using System;
using Unity.Collections;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm
{
    public class ParallelInterruptedAlgorithm
    {
        ParallelInterrupedLabeler labler;
        IPiBlobExtractor piBlobExtractor;

        public ParallelInterruptedAlgorithm(IPiBlobExtractor piBlobExtractor)
        {
            labler = new ParallelInterrupedLabeler();
            this.piBlobExtractor = piBlobExtractor;
        }

        public void ConnectedComponentExtraction(VoxelObj voxelObj, int minBlobVoxelCountForExtraction, ConnectedComponentExtractor.ExtractionCallBack callBack = null)
        {
            labler.ConnectedComponentLabeling(voxelObj, onLabelingComplete);
            void onLabelingComplete(NativeArray<int> labelMap, int blobCount)
            {
                piBlobExtractor.LabelMapExtraction(voxelObj, labelMap, blobCount, minBlobVoxelCountForExtraction, onMapExtractionComplete);
                void onMapExtractionComplete(VoxelObj[] newVoxelObjs, bool originalVoxelObjEdited, Action applyVoxelObjChanges)
                {
                    callBack.Invoke(voxelObj, originalVoxelObjEdited, applyVoxelObjChanges, newVoxelObjs);
                }
            }
        }

        public void ConnectedComponentExtractionImmediate(VoxelObj voxelObj, int minBlobVoxelCountForExtraction, ConnectedComponentExtractor.ExtractionCallBack callBack = null)
        {
            labler.ConnectedComponentLabelingImmediate(voxelObj, onLabelingComplete);
            void onLabelingComplete(NativeArray<int> labelMap, int blobCount)
            {
                piBlobExtractor.LabelMapExtractionImmediate(voxelObj, labelMap, blobCount, minBlobVoxelCountForExtraction, onMapExtractionComplete);
                void onMapExtractionComplete(VoxelObj[] newVoxelObjs, bool originalVoxelObjEdited, Action applyVoxelObjChanges)
                {
                    callBack.Invoke(voxelObj, originalVoxelObjEdited, applyVoxelObjChanges, newVoxelObjs);
                }
            }
        }
    }
}