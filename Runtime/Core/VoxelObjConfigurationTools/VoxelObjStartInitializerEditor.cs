#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VoxelEngine
{
    [CustomEditor(typeof(VoxelObjStartInitializer))]
    public class VoxelObjStartInitializerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var startInitializer = (VoxelObjStartInitializer)target;

            if(startInitializer.voxelMap == null)
            {
                EditorGUILayout.HelpBox($"{nameof(VoxelObjStartInitializer.voxelMap)} can not be null!", MessageType.Warning);
            }
            
            if (startInitializer.chunkMaterial == null)
            {
                EditorGUILayout.HelpBox($"{nameof(VoxelObjStartInitializer.chunkMaterial)} can not be null!", MessageType.Warning);
            }
            
            if(startInitializer.ShowingPreviewGizmos)
            {
                if (GUILayout.Button("Hide Preview Gizmos"))
                {
                    startInitializer.SetShowingPreviewGizmos(false);
                }
            }
            else
            {
                if (GUILayout.Button("Show Preview Gizmos"))
                {
                    startInitializer.SetShowingPreviewGizmos(true);
                }
            }
        }
    }
}
#endif