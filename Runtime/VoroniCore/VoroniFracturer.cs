using VoxelEngine.Jobs;
using VoxelEngine.VoroniCore.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine.VoroniCore
{
    public class VoroniFracturer
    {
        public delegate void ReportFractureCompletion(VoxelObj[] newVoxelobjs);

        /// <summary>
        /// Base data required for <see cref="VoroniFracture"/>
        /// </summary>
        public struct VoroniFractureData
        {
            /// <summary>
            /// The <see cref="VoxelMap"/> that is going to be fractured into new <see cref="VoxelObj"/> objects
            /// </summary>
            public VoxelMap fractureMap;
            /// <summary>
            /// The position difference between localSpace(0,0,0) and the local space postion of the voxel representing <see cref="VoroniFractureData.fractureMap"/>.Map[0,0,0].
            /// This information is relevant as the VoxelMap indices are convertet into the localSpace postions in order to determine the closest seed.
            /// </summary>
            public float3? fractureMapLocalSpacePositionOffset;
            /// <summary>
            /// The size of a voxel (length width height).
            /// This information is required as the VoxelMap indices are convertet into the localSpace postions in order to determine the closest seed.
            /// </summary>
            public float voxelSize;
            /// <summary>
            /// The seeds used for the voroni algorithm. The seeds are given in local space (of the <see cref="VoroniFractureData.fractureMap"/>).
            /// </summary>
            public float3[] seedLocalSpacePositions;
            /// <summary>
            /// The new <see cref="VoxelObj"/>s (shrapnels) are only created if they have at least this amount of voxels
            /// </summary>
            public int minVoxelsInBlob;
            /// <summary>
            /// The <see cref="VoxelObj"/> that is going to be instantiated to create the shrapnels. (This <see cref="VoxelObj"/> must be a component of a GameObject)
            /// </summary>
            public VoxelObj voxelObjPrefab;
        }

        /// <summary>
        /// <see cref="VoroniFractureData.fractureMap"/> is going to be fractured into new VoxelObjs using a Voroni algorithm
        /// </summary>
        public void VoroniFracture(VoroniFractureData fractureData, ReportFractureCompletion callOnCompletion)
        {
            AllocateAndStartVoroniJobs(fractureData, out VoroniLabelMapGeneratorJob labelMapJob, out BlobAnalysisJob analysisJob, out JobHandle analysisJobHandle, out IDisposable[] disposables);

            JobCallbackManager.Register(analysisJobHandle, onJobCompletion, disposables);

            void onJobCompletion()
            {
                analysisJobHandle.Complete();
                try
                {
                    bool requiredObjsDestroyed = (fractureData.fractureMap == null || fractureData.voxelObjPrefab == null);
                    if (requiredObjsDestroyed)
                    {
                        return;
                    }

                    var newVoxelObjs = Extraction(fractureData, labelMapJob, analysisJob);
                    if(callOnCompletion != null)
                    {
                        callOnCompletion.Invoke(newVoxelObjs);
                    }
                }
                finally
                {
                    foreach (IDisposable disposable in disposables)
                    {
                        disposable.Dispose();
                    }
                }
            }

        }

        /// <summary>
        /// <see cref="VoroniFractureData.fractureMap"/> is going to be fractured into new VoxelObjs using a Voroni algorithm
        /// </summary>
        public VoxelObj[] VoroniFractureImmediate(VoroniFractureData fractureData)
        {
            AllocateAndStartVoroniJobs(fractureData, out VoroniLabelMapGeneratorJob labelMapJob, out BlobAnalysisJob analysisJob, out JobHandle analysisJobHandle, out IDisposable[] disposables);
            analysisJobHandle.Complete();

            try
            {
                var newVoxelObjs = Extraction(fractureData, labelMapJob, analysisJob);
                return newVoxelObjs;
            }
            finally
            {
                foreach (IDisposable disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }

        private void AllocateAndStartVoroniJobs(VoroniFractureData fractureData, out VoroniLabelMapGeneratorJob labelMapJob, out BlobAnalysisJob analysisJob, out JobHandle analysisJobHandle, out IDisposable[] disposables)
        {
            NativeArray<VoxelData> nativeFractureMap = new NativeArray<VoxelData>(fractureData.fractureMap.voxelData.Length, Allocator.TempJob);
            NativeArray<int> labelMap = new NativeArray<int>(fractureData.fractureMap.voxelData.Length, Allocator.TempJob);
            NativeArray<float3> nativeSeeds = new NativeArray<float3>(fractureData.seedLocalSpacePositions.Length, Allocator.TempJob);

            nativeFractureMap.CopyFrom(fractureData.fractureMap.voxelData);
            nativeSeeds.CopyFrom(fractureData.seedLocalSpacePositions);

            if (fractureData.fractureMapLocalSpacePositionOffset.HasValue == false)
            {
                fractureData.fractureMapLocalSpacePositionOffset = new float3();
            }

            labelMapJob = new VoroniLabelMapGeneratorJob
            {
                voxelMapInput = nativeFractureMap,
                voxelMapLocalPosOffset = fractureData.fractureMapLocalSpacePositionOffset.Value,
                voxelSize = fractureData.voxelSize,
                localPosSeeds = nativeSeeds,
                voxelMapDimensions = fractureData.fractureMap.dimensions,
                labelMapOutput = labelMap
            };
            JobHandle fractureJobHandle = labelMapJob.Schedule(fractureData.fractureMap.voxelData.Length, 15);

            analysisJob = new BlobAnalysisJob
            {
                labelMapInput = labelMap,
                labelMapDemensionsInput = fractureData.fractureMap.dimensions,
                emptyLabelValueInput = VoroniLabelMapGeneratorJob.emptyLabelLabelMapValue,
                smallestBlobAxisValues = new NativeArray<int3>(fractureData.seedLocalSpacePositions.Length + 1, Allocator.TempJob),
                biggestBlobAxisValues = new NativeArray<int3>(fractureData.seedLocalSpacePositions.Length + 1, Allocator.TempJob),
                voxelsInBlob = new NativeArray<int>(fractureData.seedLocalSpacePositions.Length + 1, Allocator.TempJob),
                largestBlobLabel = new NativeArray<int>(1, Allocator.TempJob)
            };

            disposables = new IDisposable[]
            {
                nativeFractureMap,
                labelMap,
                nativeSeeds,
                analysisJob.smallestBlobAxisValues,
                analysisJob.biggestBlobAxisValues,
                analysisJob.voxelsInBlob,
                analysisJob.largestBlobLabel
            };

            analysisJobHandle = analysisJob.Schedule(fractureJobHandle);
        }

        private VoxelObj[] Extraction(VoroniFractureData fractureData, VoroniLabelMapGeneratorJob labelMapJob, BlobAnalysisJob analysisJob)
        {
            List<VoxelObj> returnNewVoxelObjs = new List<VoxelObj>();
            VoxelObj[] newVoxelObjs = new VoxelObj[fractureData.seedLocalSpacePositions.Length + 1];

            for (int index = 0; index != labelMapJob.labelMapOutput.Length; index++)
            {
                int label = labelMapJob.labelMapOutput[index];

                if (label == VoroniLabelMapGeneratorJob.emptyLabelLabelMapValue) continue;
                if (analysisJob.voxelsInBlob[label] < fractureData.minVoxelsInBlob) continue;

                if (newVoxelObjs[label] == null)
                {
                    newVoxelObjs[label] = createNewShrapnelVoxelObj(label);
                    returnNewVoxelObjs.Add(newVoxelObjs[label]);
                }

                int3 index3d = VoxelMap.Get3dMapIndex(index, fractureData.fractureMap.dimensions);
                int newObjMapIndex = VoxelMap.GetFlatMapIndex(index3d - analysisJob.smallestBlobAxisValues[label], newVoxelObjs[label].VoxelMap.dimensions);

                newVoxelObjs[label].VoxelMap.voxelData[newObjMapIndex] = labelMapJob.voxelMapInput[index];
            }

            return returnNewVoxelObjs.ToArray();

            VoxelObj createNewShrapnelVoxelObj(int label)
            {
                Vector3 position = new Vector3(
                    analysisJob.smallestBlobAxisValues[label].x * VoxelObj.voxelSize,
                    analysisJob.smallestBlobAxisValues[label].y * VoxelObj.voxelSize,
                    analysisJob.smallestBlobAxisValues[label].z * VoxelObj.voxelSize);

                VoxelObj newVoxelObj = fractureData.voxelObjPrefab.InstantiateVoxelObj();
                newVoxelObj.SetChunkMaterial(fractureData.voxelObjPrefab.ChunkMaterial);

                if (fractureData.fractureMapLocalSpacePositionOffset.HasValue)
                {
                    position += (Vector3)fractureData.fractureMapLocalSpacePositionOffset.Value;
                }
                newVoxelObj.GetVoxelObjHolder().transform.position = position;

                int3 MapSize = new int3(
                    analysisJob.biggestBlobAxisValues[label].x - analysisJob.smallestBlobAxisValues[label].x + 1,
                    analysisJob.biggestBlobAxisValues[label].y - analysisJob.smallestBlobAxisValues[label].y + 1,
                    analysisJob.biggestBlobAxisValues[label].z - analysisJob.smallestBlobAxisValues[label].z + 1);

                newVoxelObj.CreateEmptyVoxelMap(MapSize);

                return newVoxelObj;
            }
        }
    }
}