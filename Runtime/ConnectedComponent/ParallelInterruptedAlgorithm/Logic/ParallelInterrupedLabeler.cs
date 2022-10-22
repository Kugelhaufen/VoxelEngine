using VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm
{
    public class ParallelInterrupedLabeler
    {
        public delegate void LabelingCallBack(NativeArray<int> labelMap, int blobCount);

        public void ConnectedComponentLabeling(VoxelObj voxelObj, LabelingCallBack callBack)
        {
            ConnectedComponentLabelingInternal(voxelObj, false, callBack);
        }

        public void ConnectedComponentLabelingImmediate(VoxelObj voxelObj, LabelingCallBack callBack)
        {
            ConnectedComponentLabelingInternal(voxelObj, true, callBack);
        }

        private void ConnectedComponentLabelingInternal(VoxelObj voxelObj, bool immediate, LabelingCallBack callBack)
        {
            List<IDisposable> disposables = new List<IDisposable>();

            VoxelMap voxelMap = voxelObj.VoxelMap;
            NativeArray<int> labelMap = new NativeArray<int>(voxelMap.voxelData.Length, Allocator.Persistent);
            NativeArray<int> labelsFoundTotal = new NativeArray<int>(1, Allocator.Persistent);

            int zLineAmmount = voxelMap.dimensions.x * voxelMap.dimensions.y;
            NativeArray<int> zLineLabelOffsets = new NativeArray<int>(zLineAmmount, Allocator.Persistent);

            NativeQueue<int2> unionMergeCalls = new NativeQueue<int2>(Allocator.Persistent);

            disposables.Add(labelMap);
            disposables.Add(labelsFoundTotal);
            disposables.Add(zLineLabelOffsets);
            disposables.Add(unionMergeCalls);

            verticalLabelCreation();
            return;

            void verticalLabelCreation()
            {
                NativeArray<VoxelData> cclVoxelMap = new NativeArray<VoxelData>(voxelMap.voxelData.Length, Allocator.TempJob);
                cclVoxelMap.CopyFrom(voxelMap.voxelData);
                disposables.Add(cclVoxelMap);

                NativeArray<int> labelAmountPerLine = new NativeArray<int>(zLineAmmount, Allocator.TempJob);
                disposables.Add(labelAmountPerLine);

                //First Pass(first temporary labels):
                CclPiFirstPassJob firstPass = new CclPiFirstPassJob
                {
                    voxelMapInput = cclVoxelMap,
                    voxelMapDimensions = voxelMap.dimensions,
                    labelMapOutput = labelMap,
                    labelsInLine = labelAmountPerLine
                };
                JobHandle firstPasshandle = firstPass.Schedule(zLineAmmount, 25);

                //Create label offsets for every zLine:
                CclZLineLabelOffsetCreatorJob labelOffsetJob = new CclZLineLabelOffsetCreatorJob
                {
                    labelAmountPerLine = labelAmountPerLine,
                    labelsFoundTotal = labelsFoundTotal,
                    zLineLabelOffsets = zLineLabelOffsets
                };
                JobHandle labelOffsetJobHandle = labelOffsetJob.Schedule(firstPasshandle);

                //Find equivalences between labels for UnionFind merge calls:
                CclPiLabelEquivalenceFinderJob equivalenceFinderJob = new CclPiLabelEquivalenceFinderJob
                {
                    labelMapInput = labelMap,
                    zLineLabelOffsets = zLineLabelOffsets,
                    labelMapDemensions = voxelMap.dimensions,
                    unionMergeCalls = unionMergeCalls.AsParallelWriter()
                };
                JobHandle equivalenceFinderHandle = equivalenceFinderJob.Schedule(zLineAmmount, 25, labelOffsetJobHandle);

                //Sync Point 1: labelOffsetJob needs to be completed because the amount of labels is required for further allocations
                if (immediate)
                {
                    onLabelOffsetJobCompletion();
                }
                else
                {
                    JobCallbackManager.Register(labelOffsetJobHandle, onLabelOffsetJobCompletion, callOnCancel);

                    void callOnCancel()
                    {
                        labelOffsetJobHandle.Complete();
                        equivalenceFinderHandle.Complete();
                        foreach (IDisposable disposable in disposables)
                        {
                            disposable.Dispose();
                        }
                    }
                }

                void onLabelOffsetJobCompletion()
                {
                    labelOffsetJobHandle.Complete();
                    labelJoiningAndRelabeling(equivalenceFinderHandle);
                }
            }

            void labelJoiningAndRelabeling(JobHandle labelEquivalenceFinderHandle)
            {
                NativeArray<int> unionFindPointers = new NativeArray<int>(labelsFoundTotal[0] + 1, Allocator.TempJob);
                NativeArray<int> unionFindRootMemberAmmount = new NativeArray<int>(labelsFoundTotal[0] + 1, Allocator.TempJob);

                disposables.Add(unionFindPointers);
                disposables.Add(unionFindRootMemberAmmount);

                //unionFind merging
                UnionFindJob unionFindJob = new UnionFindJob
                {
                    pointers = unionFindPointers,
                    rootMemberAmmount = unionFindRootMemberAmmount,
                    mergeCalls = unionMergeCalls,
                };
                JobHandle unionFindJobHandle = unionFindJob.Schedule(labelEquivalenceFinderHandle);

                NativeArray<int> newLabels = new NativeArray<int>(labelsFoundTotal[0] + 1, Allocator.TempJob);
                NativeArray<int> finalLabelAmount = new NativeArray<int>(1, Allocator.TempJob);

                disposables.Add(newLabels);
                disposables.Add(finalLabelAmount);

                //calculate new labels (makes extracion easier)
                CclFinalLabelCreatorJob finalLabelCreatorJob = new CclFinalLabelCreatorJob
                {
                    unionFindPointers = unionFindPointers,
                    finalLabelAmount = finalLabelAmount,
                    newLabels = newLabels
                };
                JobHandle finalLabelCreatorJobHandle = finalLabelCreatorJob.Schedule(unionFindJobHandle);

                //Relable lableMap with final Labels using UnionFind:
                CclPiReLabelJob reLabelJob = new CclPiReLabelJob
                {
                    unionFindPointers = unionFindPointers,
                    labelMapDemensions = voxelMap.dimensions,
                    labelMap = labelMap,
                    zLineLabelOffsets = zLineLabelOffsets,
                    newLabels = newLabels
                };
                JobHandle reLableHandle = reLabelJob.Schedule(zLineAmmount, 25, finalLabelCreatorJobHandle);

                if (immediate)
                {
                    onCclCompletion();
                }
                else
                {
                    JobCallbackManager.Register(reLableHandle, onCclCompletion, disposables);
                }

                void onCclCompletion()
                {
                    reLableHandle.Complete();

                    int blobCount = finalLabelAmount[0];
                    disposables.Remove(labelMap);
                    foreach (IDisposable disposable in disposables)
                    {
                        disposable.Dispose();
                    }

                    callBack.Invoke(labelMap, blobCount);
                }
            }
        }
    }
}