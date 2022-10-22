using System.Threading.Tasks;
using Unity.Mathematics;

namespace VoxelEngine
{
    public class ArrayLooper3D
    {
        public delegate void CallInLoop(int x, int y, int z);
        public delegate void CallInParallelLoop(int3 index);

        public void LoopThroughBoxIn3dArray(CallInLoop callInLoop, int3 smallestBoxPos, int3 biggestBoxPos, int3 arrayDimensions)
        {
            smallestBoxPos = FixSmallestBoxBounds(smallestBoxPos);
            biggestBoxPos = FixBiggestBoxBounds(biggestBoxPos, arrayDimensions);

            for (int x = smallestBoxPos.x; x != biggestBoxPos.x; x++)
            {
                for (int y = smallestBoxPos.y; y != biggestBoxPos.y; y++)
                {
                    for (int z = smallestBoxPos.z; z != biggestBoxPos.z; z++)
                    {
                        callInLoop.Invoke(x, y, z);
                    }
                }
            }
        }

        public void LoopThroughFull3dArray(CallInLoop callInLoop, int3 arrayDimensions)
        {
            for(int x = 0; x != arrayDimensions.x; x++)
            {
                for(int y = 0; y != arrayDimensions.y; y++)
                {
                    for(int z = 0; z != arrayDimensions.z; z++)
                    {
                        callInLoop.Invoke(x, y, z);
                    }
                }
            }
        }

        public void ParallelBox3dLoop(CallInParallelLoop callInLoop, int3 smallestBoxPos, int3 biggestBoxPos, int3 arrayDimensions)
        {
            ArrayLooper3D arrayLooper3D = new ArrayLooper3D();
            var smallest = arrayLooper3D.FixSmallestBoxBounds(smallestBoxPos);
            var biggest = arrayLooper3D.FixBiggestBoxBounds(biggestBoxPos, arrayDimensions);

            int3 boxDimensions = biggest - smallest;
            int boxVoxelCount = boxDimensions.x * boxDimensions.y * boxDimensions.z;

            Parallel.For(0, boxVoxelCount, (int index) =>
            {
                var index3d = VoxelMap.Get3dMapIndex(index, boxDimensions);
                index3d.x += smallest.x;
                index3d.y += smallest.y;
                index3d.z += smallest.z;

                callInLoop.Invoke(index3d);
            });
        }

        public int3 FixBiggestBoxBounds(int3 biggestBoxPos, int3 arrayDimensions)
        {
            if (biggestBoxPos.x > arrayDimensions.x)
            {
                biggestBoxPos.x = arrayDimensions.x;
            }

            if (biggestBoxPos.y > arrayDimensions.y)
            {
                biggestBoxPos.y = arrayDimensions.y;
            }

            if (biggestBoxPos.z > arrayDimensions.z)
            {
                biggestBoxPos.z = arrayDimensions.z;
            }

            return biggestBoxPos;
        }

        public int3 FixSmallestBoxBounds(int3 smallestBoxPos)
        {
            if (smallestBoxPos.x < 0)
            {
                smallestBoxPos.x = 0;
            }

            if (smallestBoxPos.y < 0)
            {
                smallestBoxPos.y = 0;
            }

            if (smallestBoxPos.z < 0)
            {
                smallestBoxPos.z = 0;
            }

            return smallestBoxPos;
        }
    }
}