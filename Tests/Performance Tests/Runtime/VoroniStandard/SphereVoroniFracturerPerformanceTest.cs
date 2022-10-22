using VoxelEngine.Tests.CoreTestUtils;
using VoxelEngine.VoroniStandard;
using System.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace VoxelEngine.VoroniStandardTests
{
    public class SphereVoroniFracturerPerformanceTest
    {
        const int mapX = 150;
        const int mapY = 150;
        const int mapZ = 150;

        VoxelMap testVoxelMap;

        public SphereVoroniFracturerPerformanceTest()
        {
            TestVoxelMapGenerator testVoxelMapGenerator = new TestVoxelMapGenerator();
            testVoxelMap = testVoxelMapGenerator.GenerateTestVoxelMap(mapX, mapY, mapZ);
        }

        private VoxelMap GetVoxelMapCopy()
        {
            VoxelData[] voxelData = new VoxelData[testVoxelMap.voxelData.Length];
            testVoxelMap.voxelData.CopyTo(voxelData, 0);

            return new VoxelMap(voxelData, mapX, mapY, mapZ);
        }

        [UnityTest, Performance]
        public IEnumerator SphereFractureTest()
        {
            VoxelMap voxelMap = GetVoxelMapCopy();

            GameObject voxelGameObj = new GameObject();
            VoxelObj voxelObj = voxelGameObj.AddComponent<VoxelObj>();
            voxelObj.LoadExistingVoxelMap(GetVoxelMapCopy());

            Vector3 fracturePos = voxelGameObj.transform.position + new Vector3(
                (mapX * VoxelObj.voxelSize) / 2,
                (mapY * VoxelObj.voxelSize) / 2,
                (mapZ * VoxelObj.voxelSize) / 2);


            var settings = new SphereVoroniFracturer.SphereFractureSettings
            {
                voxelEdgeDestroyerMode = SphereVoroniFracturer.SphereFractureSettings.VoxelEdgeDestroyerMode.None,
                fractureVoxelRadius = 50,
                minVoxelsInShrapnelThreshold = 5,
                seedCount = 150,
                seedSpawnWorldSpaceRadius = 20,
                shrapnelPhysicsEnabled = false,
                voroniFracturerMode = SphereVoroniFracturer.SphereFractureSettings.VoroniFracturerMode.Standard,
                voroniVoxelObjUpdaterSettings = new VoroniVoxelObjUpdaterSettings()
                {
                    existingVoxelObjUpdateHandler = new StandardVoxelObjUpdateHandler(),
                    shrapnelVoxelObjUpdateHandler = new StandardFullVoxelObjUpdateHandler(),
                    voxelReplaceUpdater = new AntiFlickerVoxelReplaceUpdater()
                }
            };

            SphereVoroniFracturer fracturer = new SphereVoroniFracturer();

            bool isDone = false;
            SphereVoroniFracturer.FracturerCompleteDelegate onDone = (VoxelObj[] newVoxelObjs) =>
            {
                foreach (VoxelObj newVoxelObj in newVoxelObjs)
                {
                    GameObject.Destroy(newVoxelObj.GetVoxelObjHolder().gameObject);
                }

                isDone = true;
            };

            var frameTimeScope = Measure.Frames().WarmupCount(0).Scope("Frames");
            var totalTimeScope = Measure.Scope("Total Time (not Cpu time)");

            fracturer.Fracture(voxelObj, fracturePos, settings, onDone);

            while (isDone == false)
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