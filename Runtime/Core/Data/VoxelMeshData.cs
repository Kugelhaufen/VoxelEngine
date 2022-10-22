using UnityEngine;

namespace VoxelEngine
{
    public struct VoxelMeshData
    {
        public Vector3[] Vertices { get; set; }
        public int[] Triangles { get; set; }
        public Vector3[] Normals { get; set; }
        public Color[] VertexColors { get; set; }
        public int RenderedVoxels { get; set; }
    }
}