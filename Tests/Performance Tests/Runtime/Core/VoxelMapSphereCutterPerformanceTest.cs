using VoxelEngine.Tests.CoreTestUtils;
using NUnit.Framework;
using System;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEngine;

namespace VoxelEngine.CoreTests
{
    public class VoxelMapSphereCutterPerformanceTest
    {
        const int mapX = 100;
        const int mapY = 100;
        const int mapZ = 100;
        const int radius = 15;

        readonly int3 testVoxelMapCenter = new int3(mapX / 2, mapY / 2, mapZ / 2);

        VoxelMap testVoxelMap;

        public VoxelMapSphereCutterPerformanceTest()
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

        [Test, Performance]
        public void TestCutAndCopySphere()
        {
            GameObject voxelGameObj = new GameObject();
            var voxelObj = voxelGameObj.AddComponent<VoxelObj>();
            voxelObj.LoadExistingVoxelMap(GetVoxelMapCopy());

            VoxelMapSphereCutter voxelMapSphereCutter = new VoxelMapSphereCutter();
            Action cut = () => { voxelMapSphereCutter.CutAndCopy(voxelObj, testVoxelMapCenter, radius); };

            Measure.Method(cut).Run();

            GameObject.Destroy(voxelObj.GetVoxelObjHolder().gameObject);
        }

        [Test, Performance]
        public void TestOnlyCopySphere()
        {
            var voxelMap = GetVoxelMapCopy();
            VoxelMapSphereCutter voxelMapSphereCutter = new VoxelMapSphereCutter();
            Action cut = () => { var copy = voxelMapSphereCutter.OnlyCopy(voxelMap, testVoxelMapCenter, radius); };

            Measure.Method(cut).Run();
        }

        [Test, Performance]
        public void TestOnlyCutSphere()
        {
            GameObject voxelGameObj = new GameObject();
            var voxelObj = voxelGameObj.AddComponent<VoxelObj>();
            voxelObj.LoadExistingVoxelMap(GetVoxelMapCopy());

            VoxelMapSphereCutter voxelMapSphereCutter = new VoxelMapSphereCutter();
            Action cut = () => { voxelMapSphereCutter.OnlyCut(voxelObj, testVoxelMapCenter, radius); };

            Measure.Method(cut).Run();

            GameObject.Destroy(voxelObj.GetVoxelObjHolder().gameObject);
        }

        [Test, Performance]
        public void TestParallelOnlyCutSphere()
        {
            GameObject voxelGameObj = new GameObject();
            var voxelObj = voxelGameObj.AddComponent<VoxelObj>();
            voxelObj.LoadExistingVoxelMap(GetVoxelMapCopy());

            VoxelMapSphereCutter voxelMapSphereCutter = new VoxelMapSphereCutter();
            Action cut = () => { voxelMapSphereCutter.OnlyCutParallel(voxelObj, testVoxelMapCenter, radius); };
            Measure.Method(cut).Run();

            GameObject.Destroy(voxelObj.GetVoxelObjHolder().gameObject);
        }

        [Test, Performance]
        public void TestParallelOnlyCopy()
        {
            GameObject voxelGameObj = new GameObject();
            var voxelObj = voxelGameObj.AddComponent<VoxelObj>();
            voxelObj.LoadExistingVoxelMap(GetVoxelMapCopy());

            VoxelMapSphereCutter voxelMapSphereCutter = new VoxelMapSphereCutter();
            Action cut = () => { voxelMapSphereCutter.OnlyCopyParallel(voxelObj.VoxelMap, testVoxelMapCenter, radius); };
            Measure.Method(cut).Run();

            GameObject.Destroy(voxelObj.GetVoxelObjHolder().gameObject);
        }

        [Test, Performance]
        public void TestParallelCutAndCopy()
        {
            GameObject voxelGameObj = new GameObject();
            var voxelObj = voxelGameObj.AddComponent<VoxelObj>();
            voxelObj.LoadExistingVoxelMap(GetVoxelMapCopy());

            VoxelMapSphereCutter voxelMapSphereCutter = new VoxelMapSphereCutter();
            Action cut = () => { voxelMapSphereCutter.CutAndCopyParallel(voxelObj, testVoxelMapCenter, radius); };

            Measure.Method(cut).Run();

            GameObject.Destroy(voxelObj.GetVoxelObjHolder().gameObject);
        }
    }
}