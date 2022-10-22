using VoxelEngine.Tests.CoreTestUtils;
using System;
using System.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoxelEngine.CoreTests
{
    public class VoxelObjPhysicsPerformanceTest
    {
        const int mapX = 30;
        const int mapY = 30;
        const int mapZ = 30;

        const int repeatCount = 9;

        VoxelMap testVoxelMap;

        public VoxelObjPhysicsPerformanceTest()
        {
            TestVoxelMapGenerator testMapGen = new TestVoxelMapGenerator();
            testVoxelMap = testMapGen.GenerateTestVoxelMap(mapX, mapY, mapZ);
        }

        [UnityTest, Performance]
        public IEnumerator GenerateBoxCollidersTest()
        {
            Action<VoxelObjPhysicsManager> GenerateBoxColliders = (VoxelObjPhysicsManager physicsManager) =>
            {
                physicsManager.GenerateBoxColliders();
            };
            return TestGenerationMethod(GenerateBoxColliders);
        }

        [UnityTest, Performance]
        public IEnumerator GenerateBoxCollidersImmediateTest()
        {
            Action<VoxelObjPhysicsManager> GenerateBoxCollidersImmediate = (VoxelObjPhysicsManager physicsManager) =>
            {
                physicsManager.GenerateBoxCollidersImmediate();
            };
            return TestGenerationMethod(GenerateBoxCollidersImmediate);
        }

        private IEnumerator TestGenerationMethod(Action<VoxelObjPhysicsManager> generateBoxColliders)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                GameObject voxelGameObj = new GameObject();
                var voxelObj = voxelGameObj.AddComponent<VoxelObj>();

                voxelObj.LoadExistingVoxelMap(testVoxelMap);

                var physicsManager = voxelObj.MyVoxelObjPhysicsManager;

                bool isGenerated = false;
                physicsManager.BoxCollidersGenerated += () =>
                {
                    isGenerated = true;
                };

                physicsManager.EnablePhysics();

                var frameTimeScope = Measure.Frames().WarmupCount(0).Scope("Frames");
                var totalTimeScope = Measure.Scope("Total Time (not Cpu time)");

                generateBoxColliders(physicsManager);
                while (!isGenerated)
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