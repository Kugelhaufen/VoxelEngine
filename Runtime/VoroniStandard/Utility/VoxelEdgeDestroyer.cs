using VoxelEngine.VoroniStandard.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace VoxelEngine.VoroniStandard.Utility
{
    /// <summary>
    /// Simple implentation of the <see cref="VoxelMapEdgeDestroyerJob"/>
    /// </summary>
    public class VoxelEdgeDestroyer
    {
        public void DestroyVoxelMapEdgesImmediate(VoxelMap voxelMap)
        {
            DestroyVoxelMapEdgesInternal(voxelMap, true);
        }

        public void DestroyVoxelMapEdges(VoxelMap voxelMap, Action callOnCompletion)
        {
            DestroyVoxelMapEdgesInternal(voxelMap, false, callOnCompletion);
        }

        private void DestroyVoxelMapEdgesInternal(VoxelMap voxelMap, bool immediate, Action callOnCompletion = null)
        {
            NativeArray<VoxelData> nativeVoxelMap = new NativeArray<VoxelData>(voxelMap.voxelData.Length, Allocator.Persistent);
            nativeVoxelMap.CopyFrom(voxelMap.voxelData);

            VoxelMapEdgeDestroyerJob edgeDestroyerJob = new VoxelMapEdgeDestroyerJob
            {
                voxelMapInput = nativeVoxelMap,
                voxelMapMapDimensions = voxelMap.dimensions,
                voxelMapOutput = new NativeArray<VoxelData>(nativeVoxelMap.Length, Allocator.Persistent)
            };

            IDisposable[] disposables = new IDisposable[2]
            {
                nativeVoxelMap,
                edgeDestroyerJob.voxelMapOutput
            };

            JobHandle jobHandle = edgeDestroyerJob.Schedule(nativeVoxelMap.Length, 15);

            if(immediate)
            {
                jobHandle.Complete();
                onJobCompletion();
            }
            else
            {
                JobCallbackManager.Register(jobHandle, onJobCompletion, disposables);
            }

            void onJobCompletion()
            {
                edgeDestroyerJob.voxelMapOutput.CopyTo(voxelMap.voxelData);
                edgeDestroyerJob.voxelMapOutput.Dispose();
                nativeVoxelMap.Dispose();

                if (callOnCompletion != null)
                {
                    callOnCompletion.Invoke();
                }
            }
        }

        public void DestroyVoxelMapEdgesImmediate(ICollection<VoxelMap> voxelMaps)
        {
            foreach(VoxelMap voxelMap in voxelMaps)
            {
                DestroyVoxelMapEdgesImmediate(voxelMap);
            }
        }

        public void DestroyVoxelMapEdges(ICollection<VoxelMap> voxelMaps, Action callOnCompletion)
        {
            int workingJobCount = voxelMaps.Count;
            foreach(VoxelMap voxelMap in voxelMaps)
            {
                DestroyVoxelMapEdges(voxelMap, delegate ()
                {
                    workingJobCount -= 1;
                    if(workingJobCount == 0)
                    {
                        callOnCompletion.Invoke();
                    }
                });
            }
        }
    }
}
