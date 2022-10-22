using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    public static class VoxelLookupTable
    {
        public static readonly Vector3[] vertexOffsets = new Vector3[8] //Coordinates (Offsets) for the 8 Corners/Points a Cube consits out of
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1),
        };

        public static readonly int4[] faces = new int4[6] //refrences Vector3's in vertexOffsets in order to create a faces (of a Cube)
        {
            new int4(0, 3, 1, 2), // Back Face
            new int4(5, 6, 4, 7), // Front Face
            new int4(3, 7, 2, 6), // Top Face
            new int4(1, 5, 0, 4), // Bottom Face
            new int4(4, 7, 0, 3), // Left Face
            new int4(1, 2, 5, 6), // Right Face
        };

        public static readonly Vector3[] normalsPerFace = new Vector3[6]
        {
            new Vector3(0, 0, -1), //Back Face
            new Vector3(0, 0, 1), //Front Face
            new Vector3(0, 1, 0), //Top Face
            new Vector3(0, -1, 0), //Bottom Face
            new Vector3(-1, 0, 0), //Left Face
            new Vector3(1, 0, 0), //Right Face
        };

        public static readonly int[] triangleVertexIndexOffsets = new int[6] { 0, 1, 2, 2, 1, 3 }; //order of the vertecies in a face

        public static readonly int3[] neighborVoxelIndexOffsets = new int3[6]
        {
            new int3(0,0,-1), //Back Face neighbor
            new int3(0,0,1), //Front Face neighbor
            new int3(0,1,0), //Top Face neighbor
            new int3(0,-1,0), //Bottom Face neighbor
            new int3(-1,0,0), //Left Face neighbor
            new int3(1,0,0), //Right Face neighbor
        };
    }
}