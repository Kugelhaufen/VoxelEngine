using VoxelEngine.Jobs;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine.ConnectedComponent
{
    internal class BlobExtractor
    {
        public struct BlobExtractionData
        {
            //extract from this
            public VoxelObj voxelObj;
            public int blobAmount;
            public int minVoxelsForNewExtraction;
            public int doNotExtractLabel;
        }

        public struct BlobExtractionResult
        {
            public VoxelObj[] extractedBlobs;
            public bool originalVoxelObjMapEdited;
            public Action applyOriginalVoxelObjMapChanges;
        }

        /// <summary>
        /// Extracts new VoxelObjs from <paramref name="voxelObj"/> according to the labelMap
        /// <para>Disposes the labelMap (when not needed anymore (<see cref="BlobExtractionResult.applyOriginalVoxelObjMapChanges"/>)).</para>
        /// </summary>
        /// <param name="extractionData"></param>
        /// <param name="analysisJob"></param>
        /// <returns></returns>
        public BlobExtractionResult ExtractBlobs(BlobExtractionData extractionData, BlobAnalysisJob analysisJob)
        {
            bool originalVoxelObjEdited = false;

            //voxelObj might have been destroyed while jobs were executed
            if(extractionData.voxelObj == null)
            {
                DisposeBlobAnalysisJob(analysisJob);

                return new BlobExtractionResult()
                {
                    applyOriginalVoxelObjMapChanges = () => { },
                    extractedBlobs = new VoxelObj[0],
                    originalVoxelObjMapEdited = false
                };
            }

            List<VoxelObj> newVoxelObjs = new List<VoxelObj>();

            try
            {
                //create new voxelObjs
                VoxelObj[] blobVoxelObjs = new VoxelObj[extractionData.blobAmount + 1];
                for (int blob = 1; blob != blobVoxelObjs.Length; blob++)
                {
                    if (blob == extractionData.doNotExtractLabel) continue;

                    originalVoxelObjEdited = true;

                    if (analysisJob.voxelsInBlob[blob] < extractionData.minVoxelsForNewExtraction)
                    {
                        continue;
                    }

                    blobVoxelObjs[blob] = extractionData.voxelObj.InstantiateVoxelObj();
                    Vector3 blobLocalSpaceStartPos = new Vector3
                        (
                        analysisJob.smallestBlobAxisValues[blob].x * VoxelObj.voxelSize,
                        analysisJob.smallestBlobAxisValues[blob].y * VoxelObj.voxelSize,
                        analysisJob.smallestBlobAxisValues[blob].z * VoxelObj.voxelSize
                        );
                    Vector3 blobWorldSpaceStartPos = extractionData.voxelObj.GetVoxelObjHolder().transform.TransformPoint(blobLocalSpaceStartPos);
                    blobVoxelObjs[blob].GetVoxelObjHolder().transform.position = blobWorldSpaceStartPos;
                    blobVoxelObjs[blob].GetVoxelObjHolder().transform.rotation = extractionData.voxelObj.GetVoxelObjHolder().transform.rotation;
                    blobVoxelObjs[blob].SetChunkMaterial(extractionData.voxelObj.ChunkMaterial);

                    int3 blobMapSizes = new int3(analysisJob.biggestBlobAxisValues[blob].x - analysisJob.smallestBlobAxisValues[blob].x + 1, analysisJob.biggestBlobAxisValues[blob].y - analysisJob.smallestBlobAxisValues[blob].y + 1, analysisJob.biggestBlobAxisValues[blob].z - analysisJob.smallestBlobAxisValues[blob].z + 1);
                    blobVoxelObjs[blob].CreateEmptyVoxelMap(blobMapSizes);

                    newVoxelObjs.Add(blobVoxelObjs[blob]);
                }

                //Fill blob voxelmaps
                void fillBlobVoxelMap(int blob, int index, int3 index3d)
                {
                    if (analysisJob.labelMapInput[index] == blob)
                    {
                        if (analysisJob.voxelsInBlob[blob] > extractionData.minVoxelsForNewExtraction)
                        {
                            int3 currentBlobVoxelMapIndex = index3d - analysisJob.smallestBlobAxisValues[blob];
                            int currentFlatBlobVoxelMapIndex = VoxelMap.GetFlatMapIndex(currentBlobVoxelMapIndex, blobVoxelObjs[blob].VoxelMap.dimensions);

                            blobVoxelObjs[blob].VoxelMap.voxelData[currentFlatBlobVoxelMapIndex] = extractionData.voxelObj.VoxelMap.voxelData[index];
                        }
                    }
                }
                LoopThroughBlobsInOriginalVoxelObj(analysisJob, extractionData.blobAmount, extractionData.doNotExtractLabel, extractionData.voxelObj, fillBlobVoxelMap);

                //Method for editing original voxelobj (can be called later when needed) (e.g useful to prevent flickering)
                void applyChangesToOriginalVoxelObj()
                {
                    if (originalVoxelObjEdited == false)
                    {
                        return;
                    }

                    bool voxelObjHasBeenDestroyed = extractionData.voxelObj == null;
                    if (voxelObjHasBeenDestroyed)
                    {
                        DisposeBlobAnalysisJob(analysisJob);
                        return;
                    }

                    void editOriginaleVoxelObj(int blob, int index, int3 index3d)
                    {
                        if (analysisJob.labelMapInput[index] == blob)
                        {
                            extractionData.voxelObj.SetVoxelFilledValue(index3d, false);
                        }
                    }
                    LoopThroughBlobsInOriginalVoxelObj(analysisJob, extractionData.blobAmount, extractionData.doNotExtractLabel ,extractionData.voxelObj, editOriginaleVoxelObj);

                    DisposeBlobAnalysisJob(analysisJob);
                }

                Queue<VoxelObj> _newVoxelObjs = new Queue<VoxelObj>();
                foreach (VoxelObj obj in blobVoxelObjs)
                {
                    if (obj != null) _newVoxelObjs.Enqueue(obj);
                }

                if (originalVoxelObjEdited == false)
                {
                    DisposeBlobAnalysisJob(analysisJob);
                }

                var returnValue = new BlobExtractionResult()
                {
                    extractedBlobs = _newVoxelObjs.ToArray(),
                    originalVoxelObjMapEdited = originalVoxelObjEdited,
                    applyOriginalVoxelObjMapChanges = applyChangesToOriginalVoxelObj
                };
                
                return returnValue;
            }
            catch (Exception ex)
            {
                DisposeBlobAnalysisJob(analysisJob);
                analysisJob.labelMapInput.Dispose();
                throw ex;
            }
        }

        private delegate void BlobVoxelMapLoop(int blob, int index, int3 index3d);

        /// <summary>
        /// </summary>
        /// <param name="analysisJob"></param>
        /// <param name="blobAmount"></param>
        /// <param name="originalVoxelObj"></param>
        /// <param name="callInLoop">parameters are int index and int3 index3d</param>
        private void LoopThroughBlobsInOriginalVoxelObj(BlobAnalysisJob analysisJob, int blobAmount, int doNotExtractLabel, VoxelObj originalVoxelObj, BlobVoxelMapLoop callInLoop)
        {
            for (int blob = 1; blob != blobAmount + 1; blob++)
            {
                if (blob == doNotExtractLabel) continue;

                int maxIndex = VoxelMap.GetFlatMapIndex(analysisJob.biggestBlobAxisValues[blob], originalVoxelObj.VoxelMap.dimensions);
                int index = VoxelMap.GetFlatMapIndex(analysisJob.smallestBlobAxisValues[blob], originalVoxelObj.VoxelMap.dimensions);
                int3 index3d = VoxelMap.Get3dMapIndex(index, originalVoxelObj.VoxelMap.dimensions);

                while (index <= maxIndex)
                {
                    callInLoop.Invoke(blob, index, index3d);

                    index3d.z += 1;
                    if (index3d.z > analysisJob.biggestBlobAxisValues[blob].z)
                    {
                        index3d.z = analysisJob.smallestBlobAxisValues[blob].z;
                        index3d.y += 1;

                        if (index3d.y > analysisJob.biggestBlobAxisValues[blob].y)
                        {
                            index3d.y = analysisJob.smallestBlobAxisValues[blob].y;
                            index3d.x += 1;
                        }
                    }

                    index = VoxelMap.GetFlatMapIndex(index3d, originalVoxelObj.VoxelMap.dimensions);
                }
            }
        }

        /// <summary>
        /// Disposes all NativeArrays etc in the BlobAnalysisJob (except fot <see cref="BlobAnalysisJob.labelMapInput"/>)
        /// </summary>
        /// <param name="blobAnalysisJob"></param>
        private void DisposeBlobAnalysisJob(BlobAnalysisJob blobAnalysisJob)
        {
            blobAnalysisJob.smallestBlobAxisValues.Dispose();
            blobAnalysisJob.biggestBlobAxisValues.Dispose();
            blobAnalysisJob.voxelsInBlob.Dispose();
            blobAnalysisJob.largestBlobLabel.Dispose();
        }
    }
}