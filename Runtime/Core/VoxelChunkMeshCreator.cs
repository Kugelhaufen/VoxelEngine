using VoxelEngine.Jobs;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public class VoxelChunkMeshCreator
    {
        public VoxelMeshData CreateMeshDataImmediate(NativeArray<VoxelData> mapInput, int3 mapDimensions, int3 chunkPositionInVoxelMap)
        {
            var meshJob = CreateMeshCreatorJob(mapInput, mapDimensions, chunkPositionInVoxelMap);
            try
            {
                var jobHandle = StartMeshCreatorJob(meshJob);
                jobHandle.Complete();

                var data = GetMeshDataFromJob(meshJob);
                return data;
            }
            finally
            {
                DisposeMeshCreatorJob(meshJob);
            }
        }

        public void CreateMeshData(NativeArray<VoxelData> mapInput, int3 mapDimensions, int3 chunkPositionInVoxelMap, Action<VoxelMeshData> callBack)
        {
            var meshJob = CreateMeshCreatorJob(mapInput, mapDimensions, chunkPositionInVoxelMap);
            var jobHandle = StartMeshCreatorJob(meshJob);

            if (jobHandle.IsCompleted == false)
            {
                JobCallbackManager.Register(jobHandle, OnMeshJobCompletion, OnJobCanceled);
            }
            else
            {
                OnMeshJobCompletion();
            }

            void OnMeshJobCompletion()
            {
                jobHandle.Complete();
                var data = GetMeshDataFromJob(meshJob);
                DisposeMeshCreatorJob(meshJob);
                callBack(data);
            }

            void OnJobCanceled()
            {
                DisposeMeshCreatorJob(meshJob);
            }
        }

        private MeshCreatorJob CreateMeshCreatorJob(NativeArray<VoxelData> mapInput, int3 mapDimensions, int3 chunkPositionInVoxelMap)
        {
            var _triangles = new NativeArray<int>((int)Mathf.Pow(VoxelObj.chunkSizeInVoxels, 3) * 18, Allocator.TempJob);
            var _verticies = new NativeArray<Vector3>((int)Mathf.Pow(VoxelObj.chunkSizeInVoxels, 3) * 12, Allocator.TempJob);
            var _vertexColors = new NativeArray<Color>(_verticies.Length, Allocator.TempJob);
            var _normals = new NativeArray<Vector3>(_verticies.Length, Allocator.TempJob);

            var meshJob = new MeshCreatorJob()
            {
                voxelCounter = new CounterInterlocked(Allocator.TempJob),
                faceCounter = new CounterInterlocked(Allocator.TempJob),
                chunkStartMapCords = chunkPositionInVoxelMap,
                voxelMapInput = mapInput,
                voxelMapInputDimensions = mapDimensions,
                trianglesOutput = _triangles,
                verticesOutput = _verticies,
                vertexColors = _vertexColors,
                normalsOutput = _normals
            };

            return meshJob;
        }
        private void DisposeMeshCreatorJob(MeshCreatorJob meshCreatorJob)
        {
            meshCreatorJob.voxelCounter.Dispose();
            meshCreatorJob.faceCounter.Dispose();
            meshCreatorJob.trianglesOutput.Dispose();
            meshCreatorJob.verticesOutput.Dispose();
            meshCreatorJob.vertexColors.Dispose();
            meshCreatorJob.normalsOutput.Dispose();
        }

        private VoxelMeshData GetMeshDataFromJob(MeshCreatorJob meshCreatorJob)
        {
            int vertexCount = meshCreatorJob.faceCounter.Count * 4;
            int triangleCount = meshCreatorJob.faceCounter.Count * 6;

            VoxelMeshData meshData = new VoxelMeshData()
            {
                RenderedVoxels = meshCreatorJob.voxelCounter.Count,
                Vertices = meshCreatorJob.verticesOutput.Slice(0, vertexCount).ToArray(),
                VertexColors = meshCreatorJob.vertexColors.Slice(0, vertexCount).ToArray(),
                Triangles = meshCreatorJob.trianglesOutput.Slice(0, triangleCount).ToArray(),
                Normals = meshCreatorJob.normalsOutput.Slice(0, vertexCount).ToArray()
            };

            return meshData;
        }

        private JobHandle StartMeshCreatorJob(MeshCreatorJob meshCreatorJob)
        {
            return meshCreatorJob.Schedule(VoxelObj.chunkSizeInVoxels * VoxelObj.chunkSizeInVoxels * VoxelObj.chunkSizeInVoxels, 50);
        }
    }
}