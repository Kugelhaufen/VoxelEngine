using VoxelEngine.Jobs;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm
{
    /// <summary>
    /// Extracts all labels from the VoxelObj except the ones that has the most voxels in the lowest (non empty) Y Layer. (Very basic gravity simulation)
    /// </summary>
    public class PiGravityBlobExtractor : IPiBlobExtractor
    {
        BlobExtractor blobExtractor = new BlobExtractor();

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
                labelMap.Dispose();
                callBack.Invoke(new VoxelObj[0], false, applyChanges);
                return;
            }
            
            var biggestBlobAxisValues = new NativeArray<int3>(blobAmount + 1, Allocator.Persistent);
            var smallestBlobAxisValues = new NativeArray<int3>(blobAmount + 1, Allocator.Persistent);
            var voxelsInBlob = new NativeArray<int>(blobAmount + 1, Allocator.Persistent);
            var largestBlobLabel = new NativeArray<int>(1, Allocator.Persistent);

            BlobAnalysisJob analysisJob = new BlobAnalysisJob
            {
                labelMapInput = labelMap,
                labelMapDemensionsInput = voxelObj.VoxelMap.dimensions,
                emptyLabelValueInput = 0,
                biggestBlobAxisValues = biggestBlobAxisValues,
                smallestBlobAxisValues = smallestBlobAxisValues,
                voxelsInBlob = voxelsInBlob,
                largestBlobLabel = largestBlobLabel
            };

            IDisposable[] analysisJobDisposables = new IDisposable[]
            {
                analysisJob.biggestBlobAxisValues,
                analysisJob.smallestBlobAxisValues,
                analysisJob.voxelsInBlob,
                analysisJob.largestBlobLabel
            };

            SimpleGravityBlobAnalysisJob gravityJob = new SimpleGravityBlobAnalysisJob()
            {
                labelMapDemensionsInput = voxelObj.VoxelMap.dimensions,
                labelMapInput = labelMap,
                blobCountInput = blobAmount,
                emptyLabelValueInput = 0,
                mostGroundedBlob = new NativeArray<int>(1, Allocator.TempJob)
            };

            IDisposable[] gravityJobDisposables = new IDisposable[]
            {
                gravityJob.mostGroundedBlob
            };

            JobHandle analysisHandle = analysisJob.Schedule();
            JobHandle gravityJobHandle = gravityJob.Schedule();
            int runningJobs = 2;
            bool canceled = false;

            if (immediate)
            {
                analysisHandle.Complete();
                gravityJobHandle.Complete();
                Extraction();
            }
            else
            {
                JobCallbackManager.Register(analysisHandle, OnJobCompletion, OnCanceled);
                JobCallbackManager.Register(gravityJobHandle, OnJobCompletion, OnCanceled);
            }

            return;

            void OnCanceled()
            {
                if (canceled == true) return;
                
                canceled = true;
                analysisHandle.Complete();
                gravityJobHandle.Complete();
                DisposeAll();
            }

            void DisposeAll()
            {
                foreach (IDisposable d in analysisJobDisposables) d.Dispose();
                foreach (IDisposable d in gravityJobDisposables) d.Dispose();
                labelMap.Dispose();
            }

            void OnJobCompletion()
            {
                if (canceled)
                {
                    return;
                }

                runningJobs--;
                if (runningJobs == 0)
                {
                    analysisHandle.Complete();
                    gravityJobHandle.Complete();
                    Extraction();
                }
            }

            void Extraction()
            {
                BlobExtractor.BlobExtractionData blobExtractionData = new BlobExtractor.BlobExtractionData()
                {
                    doNotExtractLabel = gravityJob.mostGroundedBlob[0],
                    blobAmount = blobAmount,
                    minVoxelsForNewExtraction = minVoxelsForNewExtraction,
                    voxelObj = voxelObj
                };

                var extractionResult = blobExtractor.ExtractBlobs(blobExtractionData, analysisJob);
                DisposeAll();
                callBack(extractionResult.extractedBlobs, extractionResult.originalVoxelObjMapEdited, extractionResult.applyOriginalVoxelObjMapChanges);
            }
        }
    }
}