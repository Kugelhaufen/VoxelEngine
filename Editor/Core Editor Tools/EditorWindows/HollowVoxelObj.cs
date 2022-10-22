using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

namespace VoxelEngine.EditorTools
{
#if UNITY_EDITOR
    public class HollowVoxelObj : EditorWindow
    {
        private SerializedVoxelMap voxelData;
        private int wallThickness = 1;
        private string _assetName = "NewVoxelObj(Hollow)";

        [MenuItem("Tools/VoxelEngine/Hollow VoxelObj")]
        public static void ShowWindow()
        {
            GetWindow(typeof(HollowVoxelObj));
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");

            voxelData = (SerializedVoxelMap)EditorGUILayout.ObjectField("Obj: ", voxelData, typeof(SerializedVoxelMap), true);

            EditorGUILayout.Space();

            wallThickness = EditorGUILayout.IntField("Wall thickness", wallThickness);
            _assetName = EditorGUILayout.TextField("Name: ", _assetName);

            EditorGUILayout.Space();

            if (GUILayout.Button("Convert"))
            {
                if (voxelData != null)
                {
                    if (wallThickness < 1)
                    {
                        EditorUtility.DisplayDialog("Warning", "Wall thickness must be greater than 0", "Ok");
                    }
                    else
                    {
                        if (System.IO.File.Exists(Application.dataPath + "/" + _assetName + ".asset"))
                        {
                            bool replace = ReplaceDialogOpener.AskIfReplace(_assetName);
                            if (replace == false) return;
                        }

                        Hollow(voxelData, wallThickness, _assetName);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static void Hollow(SerializedVoxelMap serializedVoxelMap, int wallThickness, string assetName)
        {
            double startTime = EditorApplication.timeSinceStartup;

            VoxelMap voxelMap = serializedVoxelMap.GetVoxelMap();
            //VoxelMap voxelMap = new VoxelMap(serializedVoxelMap.VoxelMap, serializedVoxelMap.VoxelMapDimensions);
            bool[] labelMap = new bool[voxelMap.voxelData.Length];

            for (int i = 0; i != voxelMap.voxelData.Length; i++)
            {
                if (voxelMap.voxelData[i].Filled == false) continue;

                int3 index3d = VoxelMap.Get3dMapIndex(i, voxelMap.dimensions);

                for (int n = 0; n != 6; n++)
                {
                    int xNeighbor = index3d.x + VoxelLookupTable.neighborVoxelIndexOffsets[n].x;
                    int yNeighbor = index3d.y + VoxelLookupTable.neighborVoxelIndexOffsets[n].y;
                    int zNeighbor = index3d.z + VoxelLookupTable.neighborVoxelIndexOffsets[n].z;

                    if (xNeighbor < 0 || xNeighbor >= voxelMap.dimensions.x ||
                        yNeighbor < 0 || yNeighbor >= voxelMap.dimensions.y ||
                        zNeighbor < 0 || zNeighbor >= voxelMap.dimensions.z)
                    {
                        labelMap[i] = true;
                        break;
                    }

                    int neighborIndex = VoxelMap.GetFlatMapIndex(xNeighbor, yNeighbor, zNeighbor, voxelMap.dimensions);

                    if (voxelMap.voxelData[neighborIndex].Filled == false)
                    {
                        labelMap[i] = true;
                        break;
                    }
                }
            }

            for (int iteration = 0; iteration < wallThickness - 1; iteration++)
            {
                bool[] labelMapAdditions = new bool[labelMap.Length];

                for (int i = 0; i != voxelMap.voxelData.Length; i++)
                {
                    if (voxelMap.voxelData[i].Filled == false || labelMap[i] == true) continue;

                    int3 index3d = VoxelMap.Get3dMapIndex(i, voxelMap.dimensions);

                    for (int n = 0; n != 6; n++)
                    {
                        int xNeighbor = index3d.x + VoxelLookupTable.neighborVoxelIndexOffsets[n].x;
                        int yNeighbor = index3d.y + VoxelLookupTable.neighborVoxelIndexOffsets[n].y;
                        int zNeighbor = index3d.z + VoxelLookupTable.neighborVoxelIndexOffsets[n].z;
                        if (xNeighbor < 0 || xNeighbor >= voxelMap.dimensions.x || yNeighbor < 0 || yNeighbor >= voxelMap.dimensions.y || zNeighbor < 0 || zNeighbor >= voxelMap.dimensions.z)
                        {
                            continue;
                        }

                        int neighborIndex = VoxelMap.GetFlatMapIndex(xNeighbor, yNeighbor, zNeighbor, voxelMap.dimensions);

                        if (labelMap[neighborIndex] == true)
                        {
                            labelMapAdditions[i] = true;
                            break;
                        }
                    }
                }

                for (int i = 0; i != labelMapAdditions.Length; i++)
                {
                    if (labelMapAdditions[i] == true) labelMap[i] = true;
                }
            }

            VoxelData[] hollowMap = new VoxelData[voxelMap.voxelData.Length];
            for (int i = 0; i != hollowMap.Length; i++)
            {
                if (labelMap[i] == true) hollowMap[i] = voxelMap.voxelData[i];
                else hollowMap[i].Filled = false;
            }

            SerializedVoxelMap newVoxelData = (SerializedVoxelMap)ScriptableObject.CreateInstance(typeof(SerializedVoxelMap).Name);
            newVoxelData.SetData(voxelMap.dimensions, hollowMap);
            AssetDatabase.CreateAsset(newVoxelData, "Assets/" + assetName + ".asset");
            AssetDatabase.SaveAssets();

            Debug.Log("HollowVoxelObj done after: " + (EditorApplication.timeSinceStartup - startTime) + " seconds");
        }
    }
#endif
}