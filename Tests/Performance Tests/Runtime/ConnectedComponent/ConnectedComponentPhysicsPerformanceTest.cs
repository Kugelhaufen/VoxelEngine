using VoxelEngine.ConnectedComponent;
using VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm;
using VoxelEngine.Tests.CoreTestUtils;
using System;
using System.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoxelEngine.ConnectedComponentTests
{
    public class ConnectedComponentPhysicsPerformanceTest
    {
        const int mapX = 100;
        const int mapY = 100;
        const int mapZ = 100;

        const int repeatCount = 9;

        const bool newVoxelObjsPhysicsEnabled = false;
        const int minVoxelsForNewExtraction = 10;
        const bool ImmediateCcl = false;

        ConnectedComponentExtractor extractor = new ParallelInterruptedExtractor();
        VoxelReplaceUpdater replaceUpdater = new AntiFlickerVoxelReplaceUpdater();

        VoxelMap testVoxelMap;

        public ConnectedComponentPhysicsPerformanceTest()
        {
            TestVoxelMapGenerator voxelMapGenerator = new TestVoxelMapGenerator();
            testVoxelMap = voxelMapGenerator.GenerateTestVoxelMap(mapX, mapY, mapZ);
        }

        private VoxelMap GetVoxelMapCopy()
        {
            VoxelData[] voxelData = new VoxelData[testVoxelMap.voxelData.Length];
            testVoxelMap.voxelData.CopyTo(voxelData, 0);

            return new VoxelMap(voxelData, mapX, mapY, mapZ);
        }

        [UnityTest, Performance]
        public IEnumerator TestCclPhysics()
        {
            for (int i = 0; i < repeatCount; i++)
            {
                GameObject voxelGameObj = new GameObject();
                VoxelObj voxelObj = voxelGameObj.AddComponent<VoxelObj>();

                voxelObj.LoadExistingVoxelMap(GetVoxelMapCopy());

                ConnectedComponentPhysics ccl = voxelGameObj.AddComponent<ConnectedComponentPhysics>();

                ccl.extractor = extractor;
                ccl.voxelReplaceUpdater = replaceUpdater;

                ccl.minVoxelsForNewExtraction = minVoxelsForNewExtraction;
                ccl.newVoxelObjsPhysicsEnabled = newVoxelObjsPhysicsEnabled;

                bool cclDone = false;
                Action onCclDone = () => { cclDone = true; };

                ConnectedComponentPhysics.anyCclNewVoxelObjsUpdateCompletion += (VoxelObj[] newVoxelObjs) =>
                {
                    foreach (VoxelObj obj in newVoxelObjs)
                    {
                        GameObject.Destroy(obj.GetVoxelObjHolder().gameObject);
                    }
                };

                var frameTimeScope = Measure.Frames().WarmupCount(0).Scope("Frames");
                var totalTimeScope = Measure.Scope("Total Time (not Cpu time)");

                if (ImmediateCcl)
                {
                    ccl.ConnectedComponentLabelingImmediate(onCclDone);
                }
                else
                {
                    ccl.ConnectedComponentLabeling(onCclDone);
                }

                while (cclDone == false)
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