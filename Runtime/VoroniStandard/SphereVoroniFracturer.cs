using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using VoxelEngine.VoroniCore;
using VoxelEngine.VoroniStandard.Utility;
using System;

namespace VoxelEngine.VoroniStandard
{
    /// <summary>
    /// Implementation of the <see cref="VoroniFracturer"/> that cuts out a sphere from a <see cref="VoxelObj"/> and fractures it
    /// </summary>
    public class SphereVoroniFracturer
    {
        public int RadiusParallelThreshold { get; set; } = 9;

        private readonly VoroniFracturer myVoroniFracturer = new VoroniFracturer();
        public delegate void FracturerCompleteDelegate(VoxelObj[] shrapnelObjs);

        [System.Serializable]
        public class SphereFractureSettings
        {
            public enum VoroniFracturerMode { Standard, Immediate }
            public enum VoxelEdgeDestroyerMode { None, Standard, Immediate }

            public int fractureVoxelRadius;
            public float seedSpawnWorldSpaceRadius;
            public int seedCount;
            public int minVoxelsInShrapnelThreshold;
            public bool shrapnelPhysicsEnabled;
            public VoroniFracturerMode voroniFracturerMode;
            public VoxelEdgeDestroyerMode voxelEdgeDestroyerMode;

            public VoroniVoxelObjUpdaterSettings voroniVoxelObjUpdaterSettings;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fractureObj"></param>
        /// <param name="fractureWorldPos"></param>
        /// <param name="settings">Leave <see cref="VoroniVoxelObjUpdaterSettings.voxelReplaceUpdater"/> null to not update the VoxelObjs</param>
        /// <param name="callOnCompletion"></param>
        public void Fracture(VoxelObj fractureObj, Vector3 fractureWorldPos, SphereFractureSettings settings, FracturerCompleteDelegate callOnCompletion = null)
        {
            int3 fractureMapCords = fractureObj.WorldPosToMapCords(fractureWorldPos);

            VoxelMapSphereCutter sphereCutter = new VoxelMapSphereCutter();
            VoxelMap fractureMap;
            int3 cutVoxelMapOffset;
            if (settings.voroniFracturerMode == SphereFractureSettings.VoroniFracturerMode.Immediate)
            {
                if (settings.fractureVoxelRadius < RadiusParallelThreshold)
                {
                    fractureMap = sphereCutter.CutAndCopy(fractureObj, fractureMapCords, settings.fractureVoxelRadius, out cutVoxelMapOffset);

                }
                else
                {
                    fractureMap = sphereCutter.CutAndCopyParallel(fractureObj, fractureMapCords, settings.fractureVoxelRadius, out cutVoxelMapOffset);
                }
            }
            else
            {
                if (settings.fractureVoxelRadius < RadiusParallelThreshold)
                {
                    fractureMap = sphereCutter.OnlyCopy(fractureObj.VoxelMap, fractureMapCords, settings.fractureVoxelRadius, out cutVoxelMapOffset);
                }
                else
                {
                    fractureMap = sphereCutter.OnlyCopyParallel(fractureObj.VoxelMap, fractureMapCords, settings.fractureVoxelRadius, out cutVoxelMapOffset);
                }
            }

            float3[] seedsLocalSpace = SeedGenerator.GenerateSeedsInSphere(settings.seedCount, settings.seedSpawnWorldSpaceRadius, fractureWorldPos, fractureObj.GetVoxelObjHolder().transform);
            float3 mapOffset = cutVoxelMapOffset;
            mapOffset *= VoxelObj.voxelSize;

            VoroniFracturer.VoroniFractureData fractureData = new VoroniFracturer.VoroniFractureData
            {
                fractureMap = fractureMap,
                fractureMapLocalSpacePositionOffset = mapOffset,
                voxelSize = VoxelObj.voxelSize,
                seedLocalSpacePositions = seedsLocalSpace,
                minVoxelsInBlob = settings.minVoxelsInShrapnelThreshold,
                voxelObjPrefab = fractureObj
            };

            switch(settings.voroniFracturerMode)
            {
                case SphereFractureSettings.VoroniFracturerMode.Standard:
                    myVoroniFracturer.VoroniFracture(fractureData, onVoroniCompletion);
                    break;
                case SphereFractureSettings.VoroniFracturerMode.Immediate:
                    var newVoxelObjs = myVoroniFracturer.VoroniFractureImmediate(fractureData);
                    onVoroniCompletion(newVoxelObjs);
                    break;
                default:
                    throw new System.NotImplementedException();
            }

            void onVoroniCompletion(VoxelObj[] newVoxelObjs)
            {
                foreach (VoxelObj shrapnelObj in newVoxelObjs)
                {
                    var oldPos = shrapnelObj.transform.position;
                    var newPos = fractureObj.GetVoxelObjHolder().transform.TransformPoint(oldPos);

                    shrapnelObj.GetVoxelObjHolder().transform.position = newPos;
                    shrapnelObj.GetVoxelObjHolder().transform.rotation = fractureObj.GetVoxelObjHolder().transform.rotation;

                    if(settings.shrapnelPhysicsEnabled)
                    {
                        shrapnelObj.MyVoxelObjPhysicsManager.EnablePhysics();
                    }
                    else
                    {
                        shrapnelObj.MyVoxelObjPhysicsManager.DisablePhysics();
                    }
                }

                switch (settings.voxelEdgeDestroyerMode)
                {
                    case SphereFractureSettings.VoxelEdgeDestroyerMode.None:
                        updateVoxelObjs(newVoxelObjs);
                        break;

                    case SphereFractureSettings.VoxelEdgeDestroyerMode.Standard:
                        VoxelEdgeDestroyer voxelEdgeDestroyer = new VoxelEdgeDestroyer();
                        var voxelMaps = newVoxelObjs.Select(x => x.VoxelMap);
                        voxelEdgeDestroyer.DestroyVoxelMapEdges(voxelMaps.ToArray(), () => updateVoxelObjs(newVoxelObjs));
                        break;

                    case SphereFractureSettings.VoxelEdgeDestroyerMode.Immediate:
                        voxelEdgeDestroyer = new VoxelEdgeDestroyer();
                        voxelMaps = newVoxelObjs.Select(x => x.VoxelMap);
                        voxelEdgeDestroyer.DestroyVoxelMapEdgesImmediate(voxelMaps.ToArray());
                        updateVoxelObjs(newVoxelObjs);
                        break;
                }
            }

            void updateVoxelObjs(VoxelObj[] newVoxelObjs)
            {
                Action applyExistingVoxelObjChanges;

                if(settings.voroniFracturerMode == SphereFractureSettings.VoroniFracturerMode.Standard)
                {
                    if (settings.fractureVoxelRadius >= RadiusParallelThreshold)
                    {
                        applyExistingVoxelObjChanges = () => sphereCutter.OnlyCutParallel(fractureObj, fractureMapCords, settings.fractureVoxelRadius);
                    }
                    else
                    {
                        applyExistingVoxelObjChanges = () => sphereCutter.OnlyCut(fractureObj, fractureMapCords, settings.fractureVoxelRadius);
                    }
                }
                else
                {
                    applyExistingVoxelObjChanges = () => { };
                }

                if (settings.voroniVoxelObjUpdaterSettings.voxelReplaceUpdater != null)
                {
                    VoroniVoxelObjUpdater voroniVoxelObjUpdater = new VoroniVoxelObjUpdater();
                    voroniVoxelObjUpdater.Settings = settings.voroniVoxelObjUpdaterSettings;
                    voroniVoxelObjUpdater.Update(fractureObj, applyExistingVoxelObjChanges, newVoxelObjs, () => callOnCompletion.Invoke(newVoxelObjs));
                }
                else
                {
                    applyExistingVoxelObjChanges();
                    callOnCompletion(newVoxelObjs);
                }
            }
        }
    }
}