using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.Jobs
{
    [BurstCompile]
    public struct BoxColliderOptimisationJob : IJob
    {
        //Input
        /// <summary>
        /// VoxelMap input
        /// </summary>
        [ReadOnly] public NativeArray<VoxelData> voxelMap;
        /// <summary>
        /// VoxelMapDimensions input
        /// </summary>
        [ReadOnly] public int3 voxelMapDimensions;

        //Output
        /// <summary>
        /// Counts the ammount of non-empty voxels
        /// </summary>
        public NativeArray<int> voxelCount;
        /// <summary>
        /// The amount of generated colliders
        /// </summary>
        public NativeArray<int> colliderCount;
        /// <summary>
        /// The start coordinates of the boxcolliders given in unflattened(3D) <see cref="voxelMap"/> indexes
        /// </summary>
        public NativeArray<float3> boxColliderStartPosByMapIndex;
        /// <summary>
        /// The end coordinates of the boxcolliders given in unflattened(3D) <see cref="voxelMap"/> indexes
        /// </summary>
        public NativeArray<float3> boxColliderEndPosByMapIndex;

        //Other (private)
       // [DeallocateOnJobCompletion] private NativeArray<int> colliderLabelMap;
        [DeallocateOnJobCompletion] private NativeArray<bool> newColliderLabelMap;

        /// <summary>
        /// This constructor takes care of allocation all the output NativeArrays such as <see cref="boxColliderEndPosByMapIndex"/> etc...
        /// (Recommended)
        /// </summary>
        public BoxColliderOptimisationJob(NativeArray<VoxelData> voxelMap, int3 voxelMapDimensions)
        {
            this.voxelMap = voxelMap;
            this.voxelMapDimensions = voxelMapDimensions;

            voxelCount = new NativeArray<int>(1, Allocator.Persistent);
            colliderCount = new NativeArray<int>(1, Allocator.Persistent);
            boxColliderStartPosByMapIndex = new NativeArray<float3>(voxelMap.Length / 2 + 1, Allocator.Persistent);
            boxColliderEndPosByMapIndex = new NativeArray<float3>(voxelMap.Length / 2 + 1, Allocator.Persistent);

            newColliderLabelMap = new NativeArray<bool>(voxelMap.Length, Allocator.TempJob);
        }

        public void Execute()
        {
            if (newColliderLabelMap == null)
            {
                newColliderLabelMap = new NativeArray<bool>(voxelMap.Length, Allocator.TempJob);
            }

            for (int x = 0; x < voxelMapDimensions.x; x++)
            {
                for (int y = 0; y < voxelMapDimensions.y; y++)
                {
                    for (int z = 0; z < voxelMapDimensions.z; z++)
                    {
                        int flatIndex = VoxelMap.GetFlatMapIndex(x, y, z, voxelMapDimensions);

                        bool emtpy = voxelMap[flatIndex].Filled == false;
                        if (emtpy) continue;

                        voxelCount[0] += 1;

                        bool alreadyPartOfCollider = newColliderLabelMap[flatIndex] == true;
                        if (alreadyPartOfCollider) continue;

                        int3 currentBoxColliderStartIndex = new int3(x, y, z);
                        int3 currentBoxColliderEndIndex = currentBoxColliderStartIndex;

                        //find boxColliderMaxIndex z
                        int outerLoopStartIndex = z + 1; // "z + 1" because "z" is the current voxel (x,y,z) and has already been checked
                        for (int _z = outerLoopStartIndex; _z < voxelMapDimensions.z; _z++)
                        {
                            int scanFlatIndex = VoxelMap.GetFlatMapIndex(x, y, _z, voxelMapDimensions);
                            bool emptyOrUsedVoxel = (voxelMap[scanFlatIndex].Filled == false || newColliderLabelMap[scanFlatIndex] == true);
                            if (emptyOrUsedVoxel) break;
                            currentBoxColliderEndIndex.z = _z;
                        }

                        //find boxColliderMaxindex y
                        outerLoopStartIndex = y + 1; // "y + 1" because "y" has already been done in the previous loop
                        for (int _y = outerLoopStartIndex; _y < voxelMapDimensions.y; _y++)
                        {
                            for (int _z = z; _z <= currentBoxColliderEndIndex.z; _z++)
                            {
                                int scanFlatIndex = VoxelMap.GetFlatMapIndex(x, _y, _z, voxelMapDimensions);
                                bool emptyOrUsedVoxel = (voxelMap[scanFlatIndex].Filled == false || newColliderLabelMap[scanFlatIndex] == true);
                                if (emptyOrUsedVoxel) goto BreakYLoop;
                            }
                            currentBoxColliderEndIndex.y = _y;
                        }

                    BreakYLoop:
                        //find boxColliderMaxindex x
                        outerLoopStartIndex = x + 1; // "x + 1" because "x" has already been done in the previous loop
                        for (int _x = outerLoopStartIndex; _x < voxelMapDimensions.x; _x++)
                        {
                            for (int _y = y; _y <= currentBoxColliderEndIndex.y; _y++)
                            {
                                for (int _z = z; _z <= currentBoxColliderEndIndex.z; _z++)
                                {
                                    int scanFlatIndex = VoxelMap.GetFlatMapIndex(_x, _y, _z, voxelMapDimensions);
                                    bool emptyOrUsedVoxel = (voxelMap[scanFlatIndex].Filled == false || newColliderLabelMap[scanFlatIndex] == true);
                                    if (emptyOrUsedVoxel) goto BreakXLoop;
                                }
                            }
                            currentBoxColliderEndIndex.x = _x;
                        }

                    BreakXLoop:
                        boxColliderStartPosByMapIndex[colliderCount[0]] = currentBoxColliderStartIndex;
                        boxColliderEndPosByMapIndex[colliderCount[0]] = currentBoxColliderEndIndex;
                        colliderCount[0] += 1;

                        //mark collider in labelMap

                        for (int _x = x; _x <= currentBoxColliderEndIndex.x; _x++)
                        {
                            for (int _y = y; _y <= currentBoxColliderEndIndex.y; _y++)
                            {
                                for (int _z = z; _z <= currentBoxColliderEndIndex.z; _z++)
                                {
                                    int labelMapIndex = VoxelMap.GetFlatMapIndex(_x, _y, _z, voxelMapDimensions);
                                    newColliderLabelMap[labelMapIndex] = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}