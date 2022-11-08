using Unity.Mathematics;

namespace VoxelEngine.EditorTools
{
    public class VoxelMapResizer
    {
        /// <summary>
        /// Resize VoxelMap with nearest neighbor scaling algorithm
        /// </summary>
        public VoxelMap GetResizedVoxelMap(VoxelMap voxelMap, int3 wantedDimensions)
        {
            VoxelMap resizedMap = new VoxelMap(wantedDimensions.x, wantedDimensions.y, wantedDimensions.z);

            ArrayLooper3D looper = new ArrayLooper3D();

            float3 scale = wantedDimensions / (float3)voxelMap.dimensions;

            for (int x = 0; x < wantedDimensions.x; x++)
            {
                for (int y = 0; y < wantedDimensions.y; y++)
                {
                    for (int z = 0; z < wantedDimensions.z; z++)
                    {
                        int3 nearestOldMapIndex = new int3((int)(x / scale.x), (int)(y / scale.y), (int)(z / scale.z));

                        int flatIndexWantedMap = VoxelMap.GetFlatMapIndex(x, y, z, wantedDimensions);
                        int flatIndexOldMap = VoxelMap.GetFlatMapIndex(nearestOldMapIndex, voxelMap.dimensions);
                        resizedMap.voxelData[flatIndexWantedMap] = voxelMap.voxelData[flatIndexOldMap];
                    }
                }
            }

            return resizedMap;
        }
    }
}