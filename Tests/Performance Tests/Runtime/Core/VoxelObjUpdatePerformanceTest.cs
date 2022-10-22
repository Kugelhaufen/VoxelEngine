using VoxelEngine.Tests.CoreTestUtils;
using System;
using System.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoxelEngine.CoreTests
{
    public class VoxelObjUpdatePerformanceTest
    {
        const int mapX = 100;
        const int mapY = 100;
        const int mapZ = 100;

        const int repeatCount = 9;

        VoxelMap testVoxelMap;

        public VoxelObjUpdatePerformanceTest()
        {
            TestVoxelMapGenerator testVoxelMapGenerator = new TestVoxelMapGenerator();
            testVoxelMap = testVoxelMapGenerator.GenerateTestVoxelMap(mapX, mapY, mapZ);
        }

        [UnityTest, Performance]
        public IEnumerator FullVoxelObjUpdateTest()
        {
            Action<VoxelObj> updateVoxelObj = (VoxelObj voxelObj) =>
            {
                voxelObj.FullVoxelObjUpdate();
            };

            return TestUpdateMethod(updateVoxelObj);
        }

        [UnityTest, Performance]
        public IEnumerator FullVoxelObjUpdateImmediateTest()
        {
            Action<VoxelObj> updateVoxelObj = (VoxelObj voxelObj) =>
            {
                voxelObj.FullVoxelObjUpdateImmediate();
            };

            return TestUpdateMethod(updateVoxelObj);
        }

        private IEnumerator TestUpdateMethod(Action<VoxelObj> voxelObjUpdate)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                GameObject voxelGameObj = new GameObject();
                var voxelObj = voxelGameObj.AddComponent<VoxelObj>();
                voxelObj.LoadExistingVoxelMap(testVoxelMap);

                bool updateDone = false;
                voxelObj.UpdateFinished += (VoxelObj sender) =>
                {
                    updateDone = true;
                };

                var frameTimeScope = Measure.Frames().WarmupCount(0).Scope("Frames");
                var totalTimeScope = Measure.Scope("Total Time (not Cpu time)");

                voxelObjUpdate(voxelObj);

                while (updateDone == false)
                {
                    yield return null;
                }

                totalTimeScope.Dispose();

                yield return null;
                frameTimeScope.Dispose();

                GameObject.Destroy(voxelObj.GetVoxelObjHolder().gameObject);
            }
        }
    }
}