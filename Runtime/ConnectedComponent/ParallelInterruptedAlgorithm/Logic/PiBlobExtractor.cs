using VoxelEngine.Jobs;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm
{
    /// <summary>
    /// Extract all lablebs from VoxelObj except the one with the most voxels
    /// </summary>
    public class PiBlobExtractor : IPiBlobExtractor
    {
        public void LabelMapExtraction(VoxelObj voxelObj, NativeArray<int> labelMap, int blobAmount, int minVoxelsForNewExtraction, IPiBlobExtractor.CallBack callBack)
        {
            LabelMapExtractionInternal(voxelObj, labelMap, blobAmount, minVoxelsForNewExtraction, false, callBack);
        }

        public void LabelMapExtractionImmediate(VoxelObj voxelObj, NativeArray<int> labelMap, int blobAmount, int minVoxelsForNewExtraction, IPiBlobExtractor.CallBack callBack)
        {
            LabelMapExtractionInternal(voxelObj, labelMap, blobAmount, minVoxelsForNewExtraction, true, callBack);
        }

        private void LabelMapExtractionInternal(VoxelObj voxelObj, NativeArray<int> labelMap, int blobAmount, int minVoxelsForNewExtraction, bool immediate, IPiBlobExtractor.CallBack callBack)
        {
            if (blobAmount <= 1)
            {
                void applyChanges() { }
                callBack.Invoke(new VoxelObj[0], false, applyChanges);
                return;
            }

            BlobAnalysisJob analysisJob = new BlobAnalysisJob
            {
                labelMapInput = labelMap,
                labelMapDemensionsInput = voxelObj.VoxelMap.dimensions,
                emptyLabelValueInput = 0,
                biggestBlobAxisValues = new NativeArray<int3>(blobAmount + 1, Allocator.Persistent),
                smallestBlobAxisValues = new NativeArray<int3>(blobAmount + 1, Allocator.Persistent),
                voxelsInBlob = new NativeArray<int>(blobAmount + 1, Allocator.Persistent),
                largestBlobLabel = new NativeArray<int>(1, Allocator.Persistent)
            };

            IDisposable[] disposeOnCancel = new IDisposable[]
            {
                analysisJob.biggestBlobAxisValues,
                analysisJob.smallestBlobAxisValues,
                analysisJob.voxelsInBlob,
                analysisJob.largestBlobLabel,

                analysisJob.labelMapInput
            };
            JobHandle analysisHandle = analysisJob.Schedule();

            if (immediate)
            {
                analysisHandle.Complete();
                Extraction();
            }
            else
            {
                JobCallbackManager.Register(analysisHandle, Extraction, disposeOnCancel);
            }

            return;

            void Extraction()
            {
                analysisHandle.Complete();

                BlobExtractor blobExtractor = new BlobExtractor();
                BlobExtractor.BlobExtractionData blobExtractionData = new BlobExtractor.BlobExtractionData()
                {
                    doNotExtractLabel = analysisJob.largestBlobLabel[0],
                    blobAmount = blobAmount,
                    minVoxelsForNewExtraction = minVoxelsForNewExtraction,
                    voxelObj = voxelObj
                };
                var extractionResult = blobExtractor.ExtractBlobs(blobExtractionData, analysisJob);
                callBack(extractionResult.extractedBlobs, extractionResult.originalVoxelObjMapEdited, extractionResult.applyOriginalVoxelObjMapChanges);
            }
        }
    }
}