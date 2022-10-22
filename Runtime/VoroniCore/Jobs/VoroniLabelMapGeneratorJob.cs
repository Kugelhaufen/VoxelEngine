using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.VoroniCore.Jobs
{
    /// <summary>
    /// Takes a voxelmap and returns a labelmap with a voroni pattern according to the given seeds.
    /// </summary>
    [BurstCompile]
    public struct VoroniLabelMapGeneratorJob : IJobParallelFor
    {
        public const int emptyLabelLabelMapValue = -1;

        [ReadOnly] public NativeArray<VoxelData> voxelMapInput;
        /// <summary>
        /// The position difference between localSpace(0,0,0) and <see cref="voxelMapInput"/>.[0]
        /// </summary>
        [ReadOnly] public float3 voxelMapLocalPosOffset;
        [ReadOnly] public float voxelSize;
        [ReadOnly] public int3 voxelMapDimensions;
        [ReadOnly] public NativeArray<float3> localPosSeeds;

        /// <summary>
        /// <see cref="labelMapOutput"/> must be the same size as <see cref="voxelMapInput"/>.
        /// <para> <see cref="emptyLabelLabelMapValue"/> (-1) = empty label </para>
        /// <para> 0 = label 0 </para>
        /// <para> 1 = label 1 </para>
        /// etc...
        /// </summary>
        [NativeDisableParallelForRestriction] public NativeArray<int> labelMapOutput;

        public void Execute(int index)
        {
            if (voxelMapInput[index].Filled == false)
            {
                labelMapOutput[index] = emptyLabelLabelMapValue;
                return;
            }

            int3 index3d = VoxelMap.Get3dMapIndex(index, voxelMapDimensions);
            float3 thisPos = new float3(index3d.x, index3d.y, index3d.z) * voxelSize;
            thisPos += voxelMapLocalPosOffset;

            int closestSeed = 0;
            for (int curSeed = 1; curSeed != localPosSeeds.Length; curSeed++)
            {
                if (math.distance(localPosSeeds[curSeed], thisPos) < math.distance(localPosSeeds[closestSeed], thisPos)) closestSeed = curSeed;
            }
            labelMapOutput[index] = closestSeed;
        }
    }
}
