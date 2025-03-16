using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine.EditorTools
{
#if UNITY_EDITOR
    public class Voxelizer : EditorWindow
    {
        private static readonly Vector3[] directions = new Vector3[6] { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.left, Vector3.right };

        private Transform voxelizeTransform;
        private bool includeChildren = true;
        private float _voxelSize = VoxelObj.voxelSize;
        private string _assetName = "NewVoxelObj";

        private string newMapSizeLabelText = "";

        [MenuItem("Tools/VoxelEngine/Voxelizer")]
        public static void ShowWindow()
        {
            GetWindow(typeof(Voxelizer));
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            try 
            {
                EditorGUILayout.HelpBox($"A GameObject must have a MeshCollider in order to be voxelized.{Environment.NewLine}You can use the {nameof(MeshColliderTool)} to add Meshcolliders to gameobjects.", MessageType.Info);

                _assetName = EditorGUILayout.TextField("Name: ", _assetName);

                EditorGUI.BeginChangeCheck();
                voxelizeTransform = (Transform)EditorGUILayout.ObjectField("Transform: ", voxelizeTransform, typeof(Transform), true);
                includeChildren = EditorGUILayout.Toggle("Include Transform Children", includeChildren);
                _voxelSize = EditorGUILayout.FloatField("Scan VoxelSize: ", _voxelSize);
                if (EditorGUI.EndChangeCheck())
                {
                    Bounds bounds = GetTotalBounds(GetMeshColliders());
                    int3 mapSize = GetMapDimension(bounds, _voxelSize);
                    newMapSizeLabelText = "(" + mapSize.x + "," + mapSize.y + "," + mapSize.z + ")";
                }        
                EditorGUILayout.LabelField("VoxelMap size: " + newMapSizeLabelText);

                EditorGUILayout.Space();

                if (GUILayout.Button("Convert"))
                {
                    MeshCollider[] meshColliders = GetMeshColliders();

                    if(meshColliders.Length == 0)
                    {
                        EditorUtility.DisplayDialog("Warning", "No MeshColliders found", "Ok");
                        return;
                    }
                    
                    foreach(MeshCollider meshCollider in meshColliders)
                    {
                        if(GameObject.Find(meshCollider.gameObject.name) == null)
                        {
                            EditorUtility.DisplayDialog("Warning", "All GameObjects must be in the scene in order to voxelize them!", "Ok");
                            return;
                        }
                    }

                    List<string> disabledTextures;
                    if (!CheckTextureReadWriteAccess(meshColliders, out disabledTextures))
                    {
                        string message = "The following textures have Read/Write access disabled:\n\n";
                        message += string.Join("\n", disabledTextures);
                        message += "\n\nPlease enable Read/Write access in the texture import settings for these textures.\n(Inspector -> Advanced -> Read/Write)";
                        EditorUtility.DisplayDialog("Texture Read/Write Access Disabled", message, "Ok");
                        return;
                    }

                    if (System.IO.File.Exists(Application.dataPath + "/" + _assetName + ".asset"))
                    {
                        bool replace = ReplaceDialogOpener.AskIfReplace(_assetName);
                        if (replace == false) return;
                    }

                    VoxelMap voxelMap = Voxelize(meshColliders, _voxelSize);
                    if (voxelMap != null) SaveVoxelMap(voxelMap, _assetName);
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }

            MeshCollider[] GetMeshColliders()
            {
                if (voxelizeTransform == null) 
                {
                    return new MeshCollider[0];
                }

                MeshCollider[] returnMeshColliders;
                if (includeChildren) returnMeshColliders = voxelizeTransform.GetComponentsInChildren<MeshCollider>();
                else returnMeshColliders = voxelizeTransform.GetComponents<MeshCollider>();
                return returnMeshColliders;
            }
        }

        private bool CheckTextureReadWriteAccess(MeshCollider[] meshColliders, out List<string> disabledTextures)
        {
            disabledTextures = new List<string>();
            
            foreach (MeshCollider collider in meshColliders)
            {
                Renderer renderer = collider.gameObject.GetComponent<Renderer>();
                if (renderer == null || renderer.sharedMaterial == null) continue;
                
                if (renderer.sharedMaterial.mainTexture != null)
                {
                    Texture2D texture = renderer.sharedMaterial.mainTexture as Texture2D;
                    if (texture != null && !texture.isReadable)
                    {
                        disabledTextures.Add($"{texture.name} (on {collider.gameObject.name})");
                    }
                }
            }
            
            return disabledTextures.Count == 0;
        }

        private static Bounds GetTotalBounds(MeshCollider[] colliders)
        {
            Vector3 smallestBound;
            Vector3 biggestBound;

            if (colliders.Length != 0)
            {
                smallestBound = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                biggestBound = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                foreach (MeshCollider collider in colliders)
                {
                    smallestBound = Vector3.Min(smallestBound, collider.bounds.min);
                    biggestBound = Vector3.Max(biggestBound, collider.bounds.max);
                }
            }
            else
            {
                smallestBound = Vector3.zero;
                biggestBound = Vector3.zero;
            }

            Bounds returnBounds = new Bounds();
            returnBounds.min = smallestBound;
            returnBounds.max = biggestBound;
            return returnBounds;
        }

        private static int3 GetMapDimension(Bounds bounds, float voxelSize)
        {
            int3 mapDimensions = new int3
                (Mathf.CeilToInt(bounds.size.x / voxelSize),
                Mathf.CeilToInt(bounds.size.y / voxelSize),
                Mathf.CeilToInt(bounds.size.z / voxelSize));

            return mapDimensions;
        }

        public static VoxelMap Voxelize(MeshCollider[] voxelizeColls, float voxelSize)
        {
            double startTime = EditorApplication.timeSinceStartup;

            Bounds bounds = GetTotalBounds(voxelizeColls);
            int3 mapDimensions = GetMapDimension(bounds, voxelSize);
            VoxelMap voxelMap = new VoxelMap(mapDimensions);

            Vector3 scanStartPos = bounds.min + new Vector3(voxelSize / 2, voxelSize / 2, voxelSize / 2);
            float maxDist = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayCancelableProgressBar("Voxelizing...", "Progress:", 0f);
            float progress = 1;
            float finalProgress = voxelMap.voxelData.Length;

            try
            {
                for (float x = scanStartPos.x; x < bounds.max.x; x += voxelSize)
                {
                    for (float y = scanStartPos.y; y < bounds.max.y; y += voxelSize)
                    {
                        for (float z = scanStartPos.z; z < bounds.max.z; z += voxelSize)
                        {
                            bool pointContainedInMesh = checkIfPointContained(new Vector3(x, y, z), out RaycastHit closestHit, out RaycastHit[] allHits);

                            //Voxelize Mehs's "walls" (voxelize RaycastHit positions)
                            foreach (RaycastHit meshHit in allHits)
                            {
                                int xHitIndex = Mathf.FloorToInt((meshHit.point.x -bounds.min.x) / voxelSize);
                                int yHitIndex = Mathf.FloorToInt((meshHit.point.y - bounds.min.y) / voxelSize);
                                int zHitIndex = Mathf.FloorToInt((meshHit.point.z - bounds.min.z) / voxelSize);
                                if (VoxelMap.Index3dOutsideOfBounds(new int3(xHitIndex, yHitIndex, zHitIndex), mapDimensions)) continue;
                                
                                int flatHitindex = VoxelMap.GetFlatMapIndex(xHitIndex, yHitIndex, zHitIndex, mapDimensions);
                                if (voxelMap.voxelData[flatHitindex].Filled) continue;

                                Color color = getHitPointColor(meshHit);
                                VoxelData voxelData = new VoxelData()
                                {
                                    Filled = true,
                                    r = (byte)(color.r * 255),
                                    g = (byte)(color.g * 255),
                                    b = (byte)(color.b * 255),
                                    a = (byte)(color.a * 255)
                                };
                                voxelMap.voxelData[flatHitindex] = voxelData;
                            }

                            //Voxelize Mesh's inner space (check if pos is inside of mesh and voxelize)
                            int xScanPosIndex = (int)((x - scanStartPos.x) / voxelSize);
                            int yScanPosIndex = (int)((y - scanStartPos.y) / voxelSize);
                            int zScanPosIndex = (int)((z - scanStartPos.z) / voxelSize);
                            int flatScanPosindex = VoxelMap.GetFlatMapIndex(xScanPosIndex, yScanPosIndex, zScanPosIndex, mapDimensions);
                            if (voxelMap.voxelData[flatScanPosindex].Filled) continue;

                            if (pointContainedInMesh)
                            {
                                Color color = getHitPointColor(closestHit);
                                VoxelData voxelData = new VoxelData()
                                {
                                    Filled = true,
                                    r = (byte)(color.r * 255),
                                    g = (byte)(color.g * 255),
                                    b = (byte)(color.b * 255),
                                    a = (byte)(color.a * 255)
                                };
                                voxelMap.voxelData[flatScanPosindex] = voxelData;
                            }

                            progress++;
                        }
                    }

                    if (EditorUtility.DisplayCancelableProgressBar("Voxelizing Mesh", "Progress", progress / finalProgress))
                    {
                        EditorUtility.ClearProgressBar();
                        return null;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log("Voxelizer done after: " + (EditorApplication.timeSinceStartup - startTime) + " seconds");
            return voxelMap;

            bool checkIfPointContained(Vector3 point, out RaycastHit closestHit, out RaycastHit[] allHits)
            {
                bool[] directionContained = new bool[directions.Length];

                closestHit = new RaycastHit
                {
                    point = Vector3.positiveInfinity
                };

                List<RaycastHit> allHitsList = new List<RaycastHit>();

                for(int i = 0; i != directions.Length; i++)
                {
                    Vector3 direction = directions[i];
                    int outHitCount = 0;
                    int inHitCount = 0;

                    foreach (RaycastHit hit in Physics.RaycastAll(point, direction, maxDist))
                    {
                        foreach (MeshCollider collider in voxelizeColls)
                        {
                            if (hit.collider == collider)
                            {
                                allHitsList.Add(hit);
                                outHitCount += 1;
                                if (Vector3.Distance(point, hit.point) < Vector3.Distance(point, closestHit.point)) closestHit = hit;
                                break;
                            }
                        }
                    }

                    foreach (RaycastHit hit in Physics.RaycastAll(point + direction * maxDist, -direction, maxDist))
                    {
                        foreach (MeshCollider collider in voxelizeColls)
                        {
                            if (hit.collider == collider)
                            {
                                allHitsList.Add(hit);
                                inHitCount += 1;
                                if (Vector3.Distance(point, hit.point) < Vector3.Distance(point, closestHit.point)) closestHit = hit;
                                break;
                            }
                        }
                    }
                    directionContained[i] = !(outHitCount >= inHitCount);
                }

                allHits = allHitsList.ToArray();
                foreach(bool contained in directionContained)
                {
                    if (contained == false) return false;
                }
                return true;
            }

            Color getHitPointColor(RaycastHit hit)
            {
                Material sharedMaterial = hit.collider.gameObject.GetComponent<Renderer>().sharedMaterial;
                Texture2D mainTexture = (Texture2D)sharedMaterial.mainTexture;

                if (mainTexture == null)
                {
                    return sharedMaterial.color;
                }

                Vector2 pixelUV = hit.textureCoord;
                pixelUV.x *= mainTexture.width;
                pixelUV.y *= mainTexture.height;

                Color c = mainTexture.GetPixel((int)pixelUV.x, (int)pixelUV.y);
                return c;
            }
        }

        private static void SaveVoxelMap(VoxelMap voxelMap, string assetName)
        {
            SerializedVoxelMap serializedVoxelMap = (SerializedVoxelMap)ScriptableObject.CreateInstance(typeof(SerializedVoxelMap).Name);
            serializedVoxelMap.SetData(voxelMap.dimensions, voxelMap.voxelData);

            AssetDatabase.CreateAsset(serializedVoxelMap, "Assets/" + assetName + ".asset");
            AssetDatabase.SaveAssets();
        }
    }
#endif
}