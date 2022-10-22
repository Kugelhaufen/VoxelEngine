namespace VoxelEngine.Tests.CoreTestUtils
{
    public class TestVoxelMapGenerator
    {
        public VoxelMap GenerateTestVoxelMap(int sizeX, int sizeY, int sizeZ)
        {
            VoxelMap voxelMap = new VoxelMap(sizeX, sizeY, sizeZ);

            for (int i = 0; i != voxelMap.voxelData.Length; i++)
            {
                VoxelData voxelData = new VoxelData();
                if (i < voxelMap.voxelData.Length / 2)
                {
                    voxelData.Filled = UnityEngine.Random.value > 0.5f;
                }
                else
                {
                    voxelData.Filled = true;
                }

                voxelMap.voxelData[i] = voxelData;
            }

            return voxelMap;
        }
    }
}