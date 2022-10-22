using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine.Jobs
{
    [BurstCompile]
    public struct MeshCreatorJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction, ReadOnly] public NativeArray<VoxelData> voxelMapInput;
        [NativeDisableParallelForRestriction, ReadOnly] public int3 voxelMapInputDimensions;
        [NativeDisableParallelForRestriction, ReadOnly] public int3 chunkStartMapCords;

        [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<Vector3> verticesOutput;
        [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<Color> vertexColors;
        [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<int> trianglesOutput;
        [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<Vector3> normalsOutput;
        [WriteOnly] public CounterInterlocked voxelCounter;
        [WriteOnly] public CounterInterlocked faceCounter;

        public void Execute(int index)
        {
            int xMapIndex = index / (VoxelObj.chunkSizeInVoxels * VoxelObj.chunkSizeInVoxels);
            int yMapIndex = (index - xMapIndex * VoxelObj.chunkSizeInVoxels * VoxelObj.chunkSizeInVoxels) / VoxelObj.chunkSizeInVoxels;
            int zMapIndex = index - xMapIndex * VoxelObj.chunkSizeInVoxels * VoxelObj.chunkSizeInVoxels - yMapIndex * VoxelObj.chunkSizeInVoxels;
            xMapIndex += chunkStartMapCords.x;
            yMapIndex += chunkStartMapCords.y;
            zMapIndex += chunkStartMapCords.z;

            int voxelMapIndex = VoxelMap.GetFlatMapIndex(xMapIndex, yMapIndex, zMapIndex, voxelMapInputDimensions);

            bool outOfBounds = (xMapIndex < 0 || xMapIndex >= voxelMapInputDimensions.x || yMapIndex < 0 || yMapIndex >= voxelMapInputDimensions.y || zMapIndex < 0 || zMapIndex >= voxelMapInputDimensions.z);
            if (outOfBounds) return;
            if (voxelMapInput[voxelMapIndex].Filled == false) return;

            voxelCounter.Increment();

            Vector3 worldSpacePos = new Vector3(xMapIndex * VoxelObj.voxelSize, yMapIndex * VoxelObj.voxelSize, zMapIndex * VoxelObj.voxelSize);

            for (int face = 0; face < 6; face++) // a Cube has 6 Faces (loops through all faces)
            {
                int xNeighbor = xMapIndex + VoxelLookupTable.neighborVoxelIndexOffsets[face].x;
                int yNeighbor = yMapIndex + VoxelLookupTable.neighborVoxelIndexOffsets[face].y;
                int zNeighbor = zMapIndex + VoxelLookupTable.neighborVoxelIndexOffsets[face].z;
                int neighborIndex = VoxelMap.GetFlatMapIndex(xNeighbor, yNeighbor, zNeighbor, voxelMapInputDimensions);

                //only draw a face when there is no neighbor voxel
                if (xNeighbor >= 0 && xNeighbor < voxelMapInputDimensions.x)
                {
                    if (yNeighbor >= 0 && yNeighbor < voxelMapInputDimensions.y)
                    {
                        if (zNeighbor >= 0 && zNeighbor < voxelMapInputDimensions.z)
                        {
                            if (voxelMapInput[neighborIndex].Filled) continue;
                        }
                    }
                }

                int facesDrawn = (faceCounter.Increment() - 1);
                for (int vertex = 0; vertex < 4; vertex++) // A face is a squre and consits out of different 4 Points
                {
                    int vertOffsetIndex = 0;

                    switch (vertex)
                    {
                        case 0:
                            vertOffsetIndex = VoxelLookupTable.faces[face].x;
                            break;

                        case 1:
                            vertOffsetIndex = VoxelLookupTable.faces[face].y;
                            break;

                        case 2:
                            vertOffsetIndex = VoxelLookupTable.faces[face].z;
                            break;

                        case 3:
                            vertOffsetIndex = VoxelLookupTable.faces[face].w;
                            break;
                    }

                    var vertexIndex = facesDrawn * 4 + vertex;
                    verticesOutput[vertexIndex] = (VoxelLookupTable.vertexOffsets[vertOffsetIndex] * VoxelObj.voxelSize + worldSpacePos);
                    normalsOutput[vertexIndex] = VoxelLookupTable.normalsPerFace[face];

                    VoxelData voxelData = voxelMapInput[voxelMapIndex];
                    Color vertexColor = new Color((float)voxelData.r / 255, (float)voxelData.g / 255, (float)voxelData.b / 255, (float)voxelData.a / 255);
                    vertexColors[facesDrawn * 4 + vertex] = vertexColor;
                }

                for (int i = 0; i < 6; i++) //A face Consits out of 2 Triangles (6 points) though 2 of the Points(Verts) a identical and thus get reused
                {
                    trianglesOutput[facesDrawn * 6 + i] = (VoxelLookupTable.triangleVertexIndexOffsets[i] + facesDrawn * 4);
                }
            }
        }
    }
}