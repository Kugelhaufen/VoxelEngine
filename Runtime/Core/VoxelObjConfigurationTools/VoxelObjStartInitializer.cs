using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine
{
    [AddComponentMenu("VoxelEngine/" + nameof(VoxelObjStartInitializer))]
    public class VoxelObjStartInitializer : MonoBehaviour
    {
        public bool immediateUpdate;
        public bool physics;
        public bool destroyComponentAfterInitialisation = true;
        public SerializedVoxelMap voxelMap;
        public Material chunkMaterial;

        private void Start()
        {
            if (destroyComponentAfterInitialisation)
            {
                Destroy(this);
            }

            if (voxelMap != null)
            {
                var voxelObjs = this.GetComponents<VoxelObj>();

                if (voxelObjs.Length > 0)
                {
                    foreach (VoxelObj voxelObj in voxelObjs)
                    {
                        if (chunkMaterial != null)
                        {
                            voxelObj.SetChunkMaterial(chunkMaterial);
                        }

                        voxelObj.LoadExistingVoxelMap(voxelMap.GetVoxelMap());

                        if (physics)
                        {
                            voxelObj.MyVoxelObjPhysicsManager.EnablePhysics();
                        }

                        if (immediateUpdate)
                        {
                            voxelObj.FullVoxelObjUpdateImmediate();
                        }
                        else
                        {
                            voxelObj.FullVoxelObjUpdate();
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"No {nameof(VoxelObj)}s found", this);
                }
            }
            else
            {
                Debug.LogWarning($"{nameof(VoxelObjStartInitializer)}.{nameof(voxelMap)} can not be null", this);
            }
        }

        #region PreviewGizmos
#if UNITY_EDITOR
        public bool ShowingPreviewGizmos
        {
            get => showPreviewGizmos;
        }
        [SerializeField, HideInInspector] private bool showPreviewGizmos;
        private static readonly float3 gizmosRgb = new float3(255, 0, 0);
        private int3? previewVoxelMapDimensions = null;
        private Mesh[] previewMeshes;
        private int currentlyPreviewingVoxelMapId;
        VoxelPreviewMeshCreator voxelPreviewMeshCreator = new VoxelPreviewMeshCreator();

        internal void SetShowingPreviewGizmos(bool value)
        {
            showPreviewGizmos = value;
            EditorUtility.SetDirty(this);
            SceneView.RepaintAll();
        }

        private void GeneratePreviewGizmos()
        {
            if (voxelMap == null)
            {
                return;
            }

            EditorUtility.DisplayProgressBar($"Generating Preview Gizmos", "Generating Preview Gizmos", 0);

            VoxelMap previewVoxelMap = voxelMap.GetVoxelMap();
            previewVoxelMapDimensions = previewVoxelMap.dimensions;
            currentlyPreviewingVoxelMapId = voxelMap.GetInstanceID();

            previewMeshes = voxelPreviewMeshCreator.GetPreviewMeshes(previewVoxelMap);

            SceneView.RepaintAll();
            EditorUtility.ClearProgressBar();
        }

        private void OnDrawGizmos()
        {
            if (showPreviewGizmos ==  false)
            {
                return;
            }

            if(voxelMap == null)
            {
                return;
            }

            if(previewVoxelMapDimensions == null)
            {
                GeneratePreviewGizmos();
            }

            bool voxelMapWasChanged = voxelMap.GetInstanceID() != currentlyPreviewingVoxelMapId;
            if(voxelMapWasChanged)
            {
                GeneratePreviewGizmos();
            }

            var voxelSize = VoxelObj.voxelSize;

            var mapOutlineSize = new Vector3(
               previewVoxelMapDimensions.Value.x * voxelSize,
               previewVoxelMapDimensions.Value.y * voxelSize,
               previewVoxelMapDimensions.Value.z * voxelSize);

            var mapOutlineCenter = new Vector3(
                mapOutlineSize.x / 2,
                mapOutlineSize.y / 2,
                mapOutlineSize.z / 2);


            Gizmos.color = new Color(gizmosRgb.x, gizmosRgb.y, gizmosRgb.z, 1);

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(mapOutlineCenter, mapOutlineSize);

            Gizmos.color = new Color(gizmosRgb.x, gizmosRgb.y, gizmosRgb.z, 0.5f);
            foreach (Mesh mesh in previewMeshes)
            {
                Gizmos.DrawMesh(mesh);
            }
        }
#endif
        #endregion     
    }
}