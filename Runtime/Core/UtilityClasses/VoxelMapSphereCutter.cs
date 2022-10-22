using System.Collections.Concurrent;
using Unity.Mathematics;

namespace VoxelEngine
{
    public class VoxelMapSphereCutter
    {
        private delegate void SphereLoopHandler(int3 originalMapIndex, int originalMapFlatIndex);

        public void OnlyCut(VoxelObj cutFrom, int3 sphereCenterMapCord, int sphereVoxelRadius)
        {
            int3 editBoxStartPos = GetEditBoxStartPos(sphereCenterMapCord, sphereVoxelRadius);
            int3 editBoxEndPos = GetEditBoxEndPos(sphereCenterMapCord, sphereVoxelRadius);

            VoxelMapSphereLoop(cutFrom.VoxelMap, sphereCenterMapCord, sphereVoxelRadius, editBoxStartPos, editBoxEndPos, OnVoxelMapSphereLoop);
            void OnVoxelMapSphereLoop(int3 index, int originalMapFlatIndex)
            {
                cutFrom.SetVoxelFilledValue(index.x, index.y, index.z, false);
            }
        }

        public void OnlyCutParallel(VoxelObj cutFrom, int3 sphereCenterMapCord, int sphereVoxelRadius)
        {
            int3 editBoxStartPos = GetEditBoxStartPos(sphereCenterMapCord, sphereVoxelRadius);
            int3 editBoxEndPos = GetEditBoxEndPos(sphereCenterMapCord, sphereVoxelRadius);

            ConcurrentBag<int3> dirtyChunks = new ConcurrentBag<int3>();
            VoxelMap voxelMap = cutFrom.VoxelMap;

            ParallelVoxelMapSphereLoop(voxelMap, sphereCenterMapCord, sphereVoxelRadius, editBoxStartPos, editBoxEndPos, CallInLoop);
            void CallInLoop(int3 index, int flatIndex)
            {
                cutFrom.VoxelMap.voxelData[flatIndex].Filled = false;
                MarkDirty(index);

                var neighbors = cutFrom.GetChunkBorderVoxelNeighbor(index.x, index.y, index.z);

                foreach (var neighbor in neighbors)
                {
                    MarkDirty(neighbor);
                }

                void MarkDirty(int3 dirtyIndex)
                {
                    dirtyIndex.x /= VoxelObj.chunkSizeInVoxels;
                    dirtyIndex.y /= VoxelObj.chunkSizeInVoxels;
                    dirtyIndex.z /= VoxelObj.chunkSizeInVoxels;
                    dirtyChunks.Add(dirtyIndex);
                }
            }

            foreach (int3 dirtyChunkIndex in dirtyChunks)
            {
                cutFrom.MarkChunkAsDirty(dirtyChunkIndex);
            }
        }

        public VoxelMap OnlyCopy(VoxelMap copyFrom, int3 sphereCenterMapCord, int sphereVoxelRadius)
        {
            return OnlyCopy(copyFrom, sphereCenterMapCord, sphereVoxelRadius, out int3 cutVoxelMapOffset);
        }

        public VoxelMap OnlyCopy(VoxelMap copyFrom, int3 sphereCenterMapCord, int sphereVoxelRadius, out int3 cutVoxelMapOffset)
        {
            int3 editBoxStartPos = GetEditBoxStartPos(sphereCenterMapCord, sphereVoxelRadius);
            int3 editBoxEndPos = GetEditBoxEndPos(sphereCenterMapCord, sphereVoxelRadius);

            VoxelMap resultMap = new VoxelMap(editBoxEndPos - editBoxStartPos + new int3(1, 1, 1));

            VoxelMapSphereLoop(copyFrom, sphereCenterMapCord, sphereVoxelRadius, editBoxStartPos, editBoxEndPos, OnVoxelMapSphereLoop);
            void OnVoxelMapSphereLoop(int3 index, int originalMapFlatIndex)
            {
                int3 cutMapIndex3d = index - editBoxStartPos;
                int newMapFlatIndex = VoxelMap.GetFlatMapIndex(cutMapIndex3d, resultMap.dimensions);

                resultMap.voxelData[newMapFlatIndex] = copyFrom.voxelData[originalMapFlatIndex];
            }

            cutVoxelMapOffset = editBoxStartPos;
            return resultMap;
        }

        public VoxelMap OnlyCopyParallel(VoxelMap copyFrom, int3 sphereCenterMapCord, int sphereVoxelRadius)
        {
            return OnlyCopyParallel(copyFrom, sphereCenterMapCord, sphereVoxelRadius, out int3 cutVoxelMapOffset);
        }

        public VoxelMap OnlyCopyParallel(VoxelMap copyFrom, int3 sphereCenterMapCord, int sphereVoxelRadius, out int3 cutVoxelMapOffset)
        {
            int3 editBoxStartPos = GetEditBoxStartPos(sphereCenterMapCord, sphereVoxelRadius);
            int3 editBoxEndPos = GetEditBoxEndPos(sphereCenterMapCord, sphereVoxelRadius);

            VoxelMap resultMap = new VoxelMap(editBoxEndPos - editBoxStartPos + new int3(1, 1, 1));

            ArrayLooper3D looper = new ArrayLooper3D();

            ParallelVoxelMapSphereLoop(copyFrom, sphereCenterMapCord, sphereVoxelRadius, editBoxStartPos, editBoxEndPos, OnLoop);
            void OnLoop(int3 index, int originalMapFlatIndex)
            {
                int3 cutMapIndex3d = index - editBoxStartPos;
                int newMapFlatIndex = VoxelMap.GetFlatMapIndex(cutMapIndex3d, resultMap.dimensions);
                resultMap.voxelData[newMapFlatIndex] = copyFrom.voxelData[originalMapFlatIndex];
            }

            cutVoxelMapOffset = editBoxStartPos;
            return resultMap;
        }

        /// <summary>
        /// Cuts out a sphere from a <see cref="VoxelObj"/> and copies the <see cref="VoxelData"/> to a new <see cref="VoxelMap"/>;
        /// </summary>
        public VoxelMap CutAndCopy(VoxelObj cutFrom, int3 sphereCenterMapCord, int sphereVoxelRadius)
        {
            return CutAndCopy(cutFrom, sphereCenterMapCord, sphereVoxelRadius, out int3 cutVoxelMapOffset);
        }

        /// <summary>
        /// Cuts out a sphere from a <see cref="VoxelObj"/> and copies the <see cref="VoxelData"/> to a new <see cref="VoxelMap"/>;
        /// </summary>
        public VoxelMap CutAndCopy(VoxelObj cutFrom, int3 sphereCenterMapCord, int sphereVoxelRadius, out int3 cutVoxelMapOffset)
        {
            int3 editBoxStartPos = GetEditBoxStartPos(sphereCenterMapCord, sphereVoxelRadius);
            int3 editBoxEndPos = GetEditBoxEndPos(sphereCenterMapCord, sphereVoxelRadius);

            VoxelMap resultMap = new VoxelMap(editBoxEndPos - editBoxStartPos + new int3(1, 1, 1));
            VoxelMapSphereLoop(cutFrom.VoxelMap, sphereCenterMapCord, sphereVoxelRadius, editBoxStartPos, editBoxEndPos, OnVoxelMapSphereLoop);

            void OnVoxelMapSphereLoop(int3 index, int originalMapFlatIndex)
            {
                int3 cutMapIndex3d = index - editBoxStartPos;
                int newMapFlatIndex = VoxelMap.GetFlatMapIndex(cutMapIndex3d, resultMap.dimensions);

                resultMap.voxelData[newMapFlatIndex] = cutFrom.VoxelMap.voxelData[originalMapFlatIndex];
                cutFrom.SetVoxelFilledValue(index.x, index.y, index.z, false);
            }

            cutVoxelMapOffset = editBoxStartPos;
            return resultMap;
        }

        public VoxelMap CutAndCopyParallel(VoxelObj cutFrom, int3 sphereCenterMapCord, int sphereVoxelRadius)
        {
            return CutAndCopyParallel(cutFrom, sphereCenterMapCord, sphereVoxelRadius, out int3 cutVoxelMapOffset);
        }

        public VoxelMap CutAndCopyParallel(VoxelObj cutFrom, int3 sphereCenterMapCord, int sphereVoxelRadius, out int3 cutVoxelMapOffset)
        {
            int3 editBoxStartPos = GetEditBoxStartPos(sphereCenterMapCord, sphereVoxelRadius);
            int3 editBoxEndPos = GetEditBoxEndPos(sphereCenterMapCord, sphereVoxelRadius);

            ConcurrentBag<int3> dirtyChunks = new ConcurrentBag<int3>();
            VoxelMap resultMap = new VoxelMap(editBoxEndPos - editBoxStartPos + new int3(1, 1, 1));

            ParallelVoxelMapSphereLoop(cutFrom.VoxelMap, sphereCenterMapCord, sphereVoxelRadius, editBoxStartPos, editBoxEndPos, CallInLoop);
            void CallInLoop(int3 index, int flatIndex)
            {
                int3 cutMapIndex3d = index - editBoxStartPos;
                int newMapFlatIndex = VoxelMap.GetFlatMapIndex(cutMapIndex3d, resultMap.dimensions);
                resultMap.voxelData[newMapFlatIndex] = cutFrom.VoxelMap.voxelData[flatIndex];

                cutFrom.VoxelMap.voxelData[flatIndex].Filled = false;
                MarkDirty(index);

                var neighbors = cutFrom.GetChunkBorderVoxelNeighbor(index.x, index.y, index.z);

                foreach (var neighbor in neighbors)
                {
                    MarkDirty(neighbor);
                }

                void MarkDirty(int3 dirtyIndex)
                {
                    dirtyIndex.x /= VoxelObj.chunkSizeInVoxels;
                    dirtyIndex.y /= VoxelObj.chunkSizeInVoxels;
                    dirtyIndex.z /= VoxelObj.chunkSizeInVoxels;
                    dirtyChunks.Add(dirtyIndex);
                }
            }

            foreach (int3 dirtyChunkIndex in dirtyChunks)
            {
                cutFrom.MarkChunkAsDirty(dirtyChunkIndex);
            }

            cutVoxelMapOffset = editBoxStartPos;
            return resultMap;
        }

        private void VoxelMapSphereLoop(VoxelMap originalVoxelMap, int3 sphereCenterMapCord, int sphereVoxelRadius, int3 editBoxStartPos, int3 editBoxEndPos, SphereLoopHandler handler)
        {
            ArrayLooper3D looper = new ArrayLooper3D();
            void CutLoop(int x, int y, int z)
            {
                int indexFlat = VoxelMap.GetFlatMapIndex(x, y, z, originalVoxelMap.dimensions);
                if (originalVoxelMap.voxelData[indexFlat].Filled == false) return;

                int3 index = new int3(x, y, z);
                if (math.distance(index, sphereCenterMapCord) <= sphereVoxelRadius)
                {
                    handler(index, indexFlat);
                }
            }
            looper.LoopThroughBoxIn3dArray(CutLoop, editBoxStartPos, editBoxEndPos, originalVoxelMap.dimensions);
        }

        private void ParallelVoxelMapSphereLoop(VoxelMap originalVoxelMap, int3 sphereCenterMapCord, int sphereVoxelRadius, int3 editBoxStartPos, int3 editBoxEndPos, SphereLoopHandler handler)
        {
            ArrayLooper3D looper = new ArrayLooper3D();
            looper.ParallelBox3dLoop(CallInLoop, editBoxStartPos, editBoxEndPos, originalVoxelMap.dimensions);

            void CallInLoop(int3 index)
            {
                int indexFlat = VoxelMap.GetFlatMapIndex(index.x, index.y, index.z, originalVoxelMap.dimensions);
                if (originalVoxelMap.voxelData[indexFlat].Filled == false) return;

                if (math.distance(index, sphereCenterMapCord) <= sphereVoxelRadius)
                {
                    handler(index, indexFlat);
                }
            }
        }

        private int3 GetEditBoxStartPos(int3 sphereCenterMapCord, int sphereVoxelRadius)
        {
            int3 editBoxStartPos = new int3(
                sphereCenterMapCord.x - sphereVoxelRadius,
                sphereCenterMapCord.y - sphereVoxelRadius,
                sphereCenterMapCord.z - sphereVoxelRadius);

            return editBoxStartPos;
        }

        private int3 GetEditBoxEndPos(int3 sphereCenterMapCord, int sphereVoxelRadius)
        {
            int3 editBoxEndPos = new int3(
                sphereCenterMapCord.x + sphereVoxelRadius,
                sphereCenterMapCord.y + sphereVoxelRadius,
                sphereCenterMapCord.z + sphereVoxelRadius);

            return editBoxEndPos;
        }
    }
}