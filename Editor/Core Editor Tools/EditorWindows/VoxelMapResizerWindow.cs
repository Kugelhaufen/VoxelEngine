using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace VoxelEngine.EditorTools
{
    public class VoxelMapResizerWindow : EditorWindow
    {
        SerializedVoxelMap serializedVoxelMap = null;
        int xWanted = 0;
        int yWanted = 0;
        int zWanted = 0;
        double globalScale;
        string newName = "";
        bool keepRatio = false;

        [MenuItem("Tools/VoxelEngine/VoxelMap Resizer")]
        public static void ShowWindow()
        {
            GetWindow(typeof(VoxelMapResizerWindow));
        }

        private void OnGUI()
        {
            var oldSerializedVoxelMap = serializedVoxelMap;
            serializedVoxelMap = (SerializedVoxelMap)EditorGUILayout.ObjectField("Serialized VoxelMap: ", serializedVoxelMap, typeof(SerializedVoxelMap), true);
            bool mapChanged = oldSerializedVoxelMap != serializedVoxelMap;
            if(mapChanged)
            {
                xWanted = serializedVoxelMap.VoxelMapDimensions.x;
                yWanted = serializedVoxelMap.VoxelMapDimensions.y;
                zWanted = serializedVoxelMap.VoxelMapDimensions.z;
            }

            int3 currentSize = int3.zero;
            if (serializedVoxelMap != null)
            {
                currentSize = serializedVoxelMap.VoxelMapDimensions;
            }
            EditorGUILayout.LabelField("Current Dimensions: " + currentSize);

            bool oldKeepRatio = keepRatio;
            keepRatio = EditorGUILayout.Toggle("Keep Ratio", keepRatio);
            bool keepRatioWasEnabled = oldKeepRatio == false && keepRatio == true;
            if (keepRatioWasEnabled && serializedVoxelMap != null)
            {
                globalScale = xWanted / (double)serializedVoxelMap.VoxelMapDimensions.x;
                yWanted = (int)(serializedVoxelMap.VoxelMapDimensions.y * globalScale);
                zWanted = (int)(serializedVoxelMap.VoxelMapDimensions.z * globalScale);
            }

            if(keepRatio && serializedVoxelMap == null)
            {
                EditorGUILayout.HelpBox($"'Serialized VoxelMap' must not be null in order for 'Keep Ratio' to work", MessageType.Warning);
            }

            int oldX = xWanted;
            xWanted = EditorGUILayout.IntField("New X: ", xWanted);
            bool xChanged = oldX != xWanted;
            if (xChanged && keepRatio && serializedVoxelMap != null)
            {
                globalScale = xWanted / (double)serializedVoxelMap.VoxelMapDimensions.x;
                yWanted = (int)(serializedVoxelMap.VoxelMapDimensions.y * globalScale);
                zWanted = (int)(serializedVoxelMap.VoxelMapDimensions.z * globalScale);
            }

            int oldY = yWanted;
            yWanted = EditorGUILayout.IntField("New Y: ", yWanted);
            bool yChanged = oldY != yWanted;
            if (yChanged && keepRatio && serializedVoxelMap != null)
            {
                globalScale = yWanted / (double)serializedVoxelMap.VoxelMapDimensions.y;
                xWanted = (int)(serializedVoxelMap.VoxelMapDimensions.x * globalScale);
                zWanted = (int)(serializedVoxelMap.VoxelMapDimensions.z * globalScale);
            }

            int oldZ = zWanted;
            zWanted = EditorGUILayout.IntField("New Z: ", zWanted);
            bool zChanged = oldZ != zWanted;
            if (zChanged && keepRatio && serializedVoxelMap != null)
            {
                globalScale = zWanted / (double)serializedVoxelMap.VoxelMapDimensions.z;
                yWanted = (int)(serializedVoxelMap.VoxelMapDimensions.y * globalScale);
                xWanted = (int)(serializedVoxelMap.VoxelMapDimensions.x * globalScale);
            }

            newName = EditorGUILayout.TextField("Name: ", newName);


            if (GUILayout.Button("Resize") == false)
            {
                return;
            }

            if (serializedVoxelMap == null)
            {
                EditorUtility.DisplayDialog("Error", "No VoxelMap selected", "Ok");
                return;
            }

            if (xWanted < 1 || yWanted < 1 || zWanted < 1)
            {
                EditorUtility.DisplayDialog("Error", "Dimensions must be greater than 0", "Ok");
                return;
            }

            if (newName.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Name must be set", "Ok");
                return;
            }

            int3 wantedSize = new int3(xWanted, yWanted, zWanted);
            VoxelMap oldMap = serializedVoxelMap.GetVoxelMap();

            VoxelMapResizer voxelMapResizer = new VoxelMapResizer();
            VoxelMap newMap = voxelMapResizer.GetResizedVoxelMap(oldMap, wantedSize);

            SerializedVoxelMap newSerializedVoxelMap = ScriptableObject.CreateInstance<SerializedVoxelMap>();
            newSerializedVoxelMap.SetData(newMap.dimensions, newMap.voxelData);

            if (System.IO.File.Exists(Application.dataPath + "/" + newName + ".asset"))
            {
                bool replace = ReplaceDialogOpener.AskIfReplace(newName);
                if (replace == false) return;
            }

            AssetDatabase.CreateAsset(newSerializedVoxelMap, "Assets/" + newName + ".asset");
            AssetDatabase.SaveAssets();
        }
    }
}
#endif