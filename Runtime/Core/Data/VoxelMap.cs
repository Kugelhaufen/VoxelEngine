using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace VoxelEngine
{
    public class VoxelMap
    {
        public VoxelData[] voxelData { get; private set; }
        public readonly int3 dimensions;

        public VoxelMap(int3 mapDimensions)
        {
            this.dimensions = mapDimensions;
            voxelData = new VoxelData[mapDimensions.x * mapDimensions.y * mapDimensions.z];
        }

        public VoxelMap(VoxelData[] data, int3 mapDimensions)
        {
            this.dimensions = mapDimensions;
            this.voxelData = data;
        }

        public VoxelMap(int sizeX, int sizeY, int sizeZ)
        {
            this.dimensions = new int3(sizeX, sizeY, sizeZ);
            voxelData = new VoxelData[dimensions.x * dimensions.y * dimensions.z];
        }

        public VoxelMap(VoxelData[] data, int sizeX, int sizeY, int sizeZ)
        {
            this.dimensions = new int3(sizeX, sizeY, sizeZ);
            this.voxelData = data;
        }

        #region static (constant) Formulas

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AddToFlatIndex(int index, int3 offset, int3 mapDimensions)
        {
            return index + offset.x * mapDimensions.y * mapDimensions.z + offset.y * mapDimensions.z + offset.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AddToFlatIndex(int index, int offsetX, int offsetY, int offsetZ, int3 mapDimensions)
        {
            return index + offsetX * mapDimensions.y * mapDimensions.z + offsetY * mapDimensions.z + offsetZ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFlatMapIndex(int x, int y, int z, int3 mapDimensions)
        {
            return x * mapDimensions.z * mapDimensions.y + y * mapDimensions.z + z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFlatMapIndex(int3 mapIndex, int3 mapDimensions)
        {
            return mapIndex.x * mapDimensions.z * mapDimensions.y + mapIndex.y * mapDimensions.z + mapIndex.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Index3dOutsideOfBounds(int x, int y, int z, int3 mapDimensions)
        {
            bool smallerAsBounds = (x < 0 || y < 0 || z < 0);
            bool biggerAsBounds = (x >= mapDimensions.x || y >= mapDimensions.y || z >= mapDimensions.z);
            bool outOfBounds = (smallerAsBounds || biggerAsBounds);
            return outOfBounds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Index3dOutsideOfBounds(int3 mapIndex, int3 mapDimensions)
        {
            bool smallerAsBounds = (mapIndex.x < 0 || mapIndex.y < 0 || mapIndex.z < 0);
            bool biggerAsBounds = (mapIndex.x >= mapDimensions.x || mapIndex.y >= mapDimensions.y || mapIndex.z >= mapDimensions.z);
            bool outOfBounds = (smallerAsBounds || biggerAsBounds);
            return outOfBounds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Get3dMapIndex(int index, int3 mapDimensions)
        {
            int3 result;
            result.x = index / (mapDimensions.z * mapDimensions.y);
            result.y = (index - result.x * mapDimensions.z * mapDimensions.y) / mapDimensions.z;
            result.z = index - result.x * mapDimensions.z * mapDimensions.y - result.y * mapDimensions.z;
            return result;
        }
        #endregion
    }
}