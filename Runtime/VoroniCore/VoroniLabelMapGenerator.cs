using VoxelEngine.VoroniCore.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.VoroniCore
{
    public class VoroniLabelMapGenerator
    {
        public delegate void LabelMapGenerated(int[] labelMap);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voxelMap"></param>
        /// <param name="seedLocalSpacePositions">The seeds used for the voroni algorithm.</param>
        /// <param name="callBack"></param>
        /// <param name="voxelMapLocalPosOffset">The position difference between localSpace(0,0,0) and the local space postion of the voxel representing voxelMap.Map[0,0,0].
        /// This information is relevant as the VoxelMap indices are convertet into the localSpace postions in order to determine the closest seed.</param>
        public void GenerateVoroniLabelMap(VoxelMap voxelMap, float3[] seedLocalSpacePositions, LabelMapGenerated callBack, float3? voxelMapLocalPosOffset = null)
        {
            GenerateVoroniLabelMapInternal(voxelMap, seedLocalSpacePositions, callBack, false, voxelMapLocalPosOffset);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voxelMap"></param>
        /// <param name="seedLocalSpacePositions">The seeds used for the voroni algorithm.</param>
        /// <param name="voxelMapLocalPosOffset">The position difference between localSpace(0,0,0) and the local space postion of the voxel representing voxelMap.Map[0,0,0].
        /// This information is relevant as the VoxelMap indices are convertet into the localSpace postions in order to determine the closest seed.</param>
        public void GenerateVoroniLabelMapImmediate(VoxelMap voxelMap, float3[] seedLocalSpacePositions, float3? voxelMapLocalPosOffset = null)
        {
            GenerateVoroniLabelMapInternal(voxelMap, seedLocalSpacePositions, null, true, voxelMapLocalPosOffset);
        }

        private void GenerateVoroniLabelMapInternal(VoxelMap voxelMap, float3[] seedsLocalPos, LabelMapGenerated callBack, bool immediate, float3? voxelMapLocalPosOffset = null)
        {
            if (voxelMapLocalPosOffset.HasValue == false)
            {
                voxelMapLocalPosOffset = new float3();
            }

            VoroniLabelMapGeneratorJob labelMapGeneratorJob = new VoroniLabelMapGeneratorJob()
            {
                voxelMapInput = new NativeArray<VoxelData>(voxelMap.voxelData.Length, Allocator.TempJob),
                voxelMapLocalPosOffset = voxelMapLocalPosOffset.Value,
                voxelMapDimensions = voxelMap.dimensions,
                localPosSeeds = new NativeArray<float3>(seedsLocalPos.Length, Allocator.TempJob),
                labelMapOutput = new NativeArray<int>(voxelMap.voxelData.Length, Allocator.TempJob)
            };
            labelMapGeneratorJob.voxelMapInput.CopyFrom(voxelMap.voxelData);
            labelMapGeneratorJob.localPosSeeds.CopyFrom(seedsLocalPos);

            JobHandle jobHandle = labelMapGeneratorJob.Schedule(voxelMap.voxelData.Length, 15);

            if(immediate)
            {
                jobHandle.Complete();
                onJobCompletion();
            }
            else
            {
                JobCallbackManager.Register(jobHandle, onJobCompletion, dispose);
            }

            void onJobCompletion()
            {
                int[] labelMap = new int[voxelMap.voxelData.Length];
                labelMapGeneratorJob.labelMapOutput.CopyTo(labelMap);

                dispose();

                if(callBack != null)
                {
                    callBack.Invoke(labelMap);
                }
            }

            void dispose()
            {
                labelMapGeneratorJob.voxelMapInput.Dispose();
                labelMapGeneratorJob.localPosSeeds.Dispose();
                labelMapGeneratorJob.labelMapOutput.Dispose();
            }
        }
    }
}
