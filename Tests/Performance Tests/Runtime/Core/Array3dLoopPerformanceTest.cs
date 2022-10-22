using NUnit.Framework;
using Unity.Mathematics;
using Unity.PerformanceTesting;

namespace VoxelEngine.CoreTests
{
    public class Array3dLoopPerformanceTest
    {
        const int mapX = 500;
        const int mapY = 500;
        const int mapZ = 500;

        readonly int3 arrayDimensions = new int3(mapX, mapY, mapZ);


        readonly int3 smallestBoxPos = new int3(0, 0, 0);
        readonly int3 biggestBoxPos = new int3(mapX, mapY, mapZ);

        [Test, Performance]
        public void ArrayLooper3dFull()
        {
            var arrayLooper = new ArrayLooper3D();
            Measure.Method(() => arrayLooper.LoopThroughFull3dArray(
                (x, y, z) => { },
                arrayDimensions)).MeasurementCount(20).Run();
        }

        [Test, Performance]
        public void ArrayLooper3dBox()
        {
            var arrayLooper = new ArrayLooper3D();
            Measure.Method(() => arrayLooper.LoopThroughBoxIn3dArray(
                (x, y, z) => { },
                smallestBoxPos,
                biggestBoxPos,
                arrayDimensions)).MeasurementCount(20).Run();
        }

        [Test, Performance]
        public void ParallelArrayLooper3dBox()
        {
            ArrayLooper3D looper = new ArrayLooper3D();
            Measure.Method(() => looper.ParallelBox3dLoop((int3 index) => { }, smallestBoxPos, biggestBoxPos, arrayDimensions)).MeasurementCount(20).Run();
        }
    }
}