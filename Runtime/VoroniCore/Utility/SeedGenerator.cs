using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine.VoroniCore
{
    /// <summary>
    /// Can be used to generate seeds for Voroni-fracture.
    /// </summary>
    public static class SeedGenerator
    {
        /// <summary>
        /// For every voxel that is != EmtpyVoxelValue and inside the defined Box, a seeds might be created at the voxels position (seeds are in (VoxelObj's) LocalSpace)
        /// </summary>
        /// <param name="seedSpawnProbability">min value = 0f; max value = 1f</param>
        public static float3[] GenerateSeedsOnVoxelsInBox(float seedSpawnProbability, int3 smallestBoxIndex, int3 biggestBoxIndex, VoxelMap voxelMap)
        {
            Queue<float3> seeds = new Queue<float3>();
            void loop(int x, int y, int z)
            {
                int flatIndex = VoxelMap.GetFlatMapIndex(x, y, z, voxelMap.dimensions);
                if (voxelMap.voxelData[flatIndex].Filled == false) return;

                float random = UnityEngine.Random.Range(0f, 1f);
                if (random <= seedSpawnProbability)
                {
                    seeds.Enqueue(new float3(x * VoxelObj.voxelSize, y * VoxelObj.voxelSize, z * VoxelObj.voxelSize));
                }
            }
            ArrayLooper3D looper = new ArrayLooper3D();
            looper.LoopThroughBoxIn3dArray(loop, smallestBoxIndex, biggestBoxIndex, voxelMap.dimensions);

            return seeds.ToArray();
        }

        /// <summary>
        /// Generates "seedCount" amount of seeds in the radius
        /// </summary>
        public static float3[] GenerateSeedsInSphere(int seedCount, float radius, Vector3 SphereCenterPos)
        {
            float3[] seeds = new float3[seedCount];
            for (int i = 0; i != seeds.Length; i++)
            {
                seeds[i] = UnityEngine.Random.insideUnitSphere * radius + SphereCenterPos;
            }
            return seeds;
        }

        /// <summary>
        /// Generates "seedCount" amount of seeds in the radius and converts their position to the localSpace of the given Transform
        /// </summary>
        public static float3[] GenerateSeedsInSphere(int seedCount, float radius, Vector3 SphereCenterPos, Transform convertToLocalSpaceTransform)
        {
            float3[] seeds = new float3[seedCount];
            for (int i = 0; i != seeds.Length; i++)
            {
                seeds[i] = convertToLocalSpaceTransform.InverseTransformPoint(UnityEngine.Random.insideUnitSphere * radius + SphereCenterPos);
            }
            return seeds;
        }
    }
}
