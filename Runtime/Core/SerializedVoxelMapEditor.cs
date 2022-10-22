#if UNITY_EDITOR
using UnityEditor;

namespace VoxelEngine
{
    [CustomEditor(typeof(SerializedVoxelMap))]
    public class SerializedVoxelMapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SerializedVoxelMap map = (SerializedVoxelMap)target;
            EditorGUILayout.LabelField("Dimensions " + map.VoxelMapDimensions.ToString());
        }
    }
}
#endif