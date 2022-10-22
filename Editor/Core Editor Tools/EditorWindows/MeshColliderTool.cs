using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine.EditorTools
{
#if UNITY_EDITOR
    public class MeshColliderTool : EditorWindow
    {
        private GameObject gameObject;
        private bool removeExistingColliders = true;
        private bool onChildren = true;

        [MenuItem("Tools/VoxelEngine/MeshColliderTool")]
        public static void ShowWindow()
        {
            GetWindow(typeof(MeshColliderTool));
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.HelpBox($"You can use this tool to add Meshcolliders to gameobjects.", MessageType.Info);

            gameObject = (GameObject)EditorGUILayout.ObjectField("Obj: ", gameObject, typeof(GameObject), true);
            removeExistingColliders = EditorGUILayout.Toggle("Remove existing MeshColliders", removeExistingColliders);
            onChildren = EditorGUILayout.Toggle("Include ChildObjects", onChildren);

            if(GUILayout.Button("Apply"))
            {
                List<GameObject> gameObjects = new List<GameObject>();
                gameObjects.Add(gameObject);

                if(onChildren) AddAllGameObjChildrenToList(ref gameObjects, gameObject);

                if (removeExistingColliders) RemoveAllMeshColliders(gameObjects);

                AddMeshCollider(gameObjects);
                EditorUtility.DisplayDialog("Success", "Success", "Ok");
            }

            EditorGUILayout.EndVertical();
        }
        private static void AddAllGameObjChildrenToList(ref List<GameObject> addToList, GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                if (child == parent.transform) continue;

                addToList.Add(child.gameObject);
                AddAllGameObjChildrenToList(ref addToList, child.gameObject);
            }
        }

        private static void AddMeshCollider(IEnumerable<GameObject> gameObjs)
        {
            foreach(GameObject obj in gameObjs)
            {
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                if (meshFilter == null) continue;
                if (meshFilter.sharedMesh == null) continue;

                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        private static void RemoveAllMeshColliders(IEnumerable<GameObject> gameObjs)
        {
            foreach (GameObject obj in gameObjs)
            {
                MeshCollider[] allObjColliders = obj.GetComponents<MeshCollider>();
                foreach (MeshCollider collider in allObjColliders)
                {
                    DestroyImmediate(collider);
                }
            }
        }
    }
#endif
}