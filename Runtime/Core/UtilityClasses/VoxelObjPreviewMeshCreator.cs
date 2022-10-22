using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public class VoxelPreviewMeshCreator
    {
        VoxelChunkMeshCreator meshCreator = new VoxelChunkMeshCreator();

        public Mesh[] GetPreviewMeshes(VoxelMap voxelMap)
        {
            List<Mesh> meshList = new List<Mesh>();

            int chunkArraySizeX = (int)Mathf.Ceil((float)voxelMap.dimensions.x / (float)VoxelObj.chunkSizeInVoxels);
            int chunkArraySizeY = (int)Mathf.Ceil((float)voxelMap.dimensions.y / (float)VoxelObj.chunkSizeInVoxels);
            int chunkArraySizeZ = (int)Mathf.Ceil((float)voxelMap.dimensions.z / (float)VoxelObj.chunkSizeInVoxels);

            NativeArray<VoxelData> nativePreviewMap = new NativeArray<VoxelData>(voxelMap.voxelData, Allocator.TempJob);

            for (int y = 0; y < chunkArraySizeY; y++)
            {
                for (int x = 0; x < chunkArraySizeX; x++)
                {
                    for (int z = 0; z < chunkArraySizeZ; z++)
                    {
                        int3 positionInVoxelMap = new int3(x * VoxelObj.chunkSizeInVoxels, y * VoxelObj.chunkSizeInVoxels, z * VoxelObj.chunkSizeInVoxels);
                        var meshData = meshCreator.CreateMeshDataImmediate(nativePreviewMap, voxelMap.dimensions, positionInVoxelMap);

                        bool emptyChunk = meshData.Vertices.Length == 0;
                        if (emptyChunk) continue;

                        var mesh = new Mesh();
                        mesh.vertices = meshData.Vertices;
                        mesh.triangles = meshData.Triangles;
                        mesh.colors = meshData.VertexColors;
                        mesh.normals = meshData.Normals;

                        meshList.Add(mesh);
                    }
                }
            }

            nativePreviewMap.Dispose();
            return meshList.ToArray();
        }
    }
}