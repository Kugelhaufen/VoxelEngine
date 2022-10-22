using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    [RequireComponent(typeof(MeshCollider), typeof(MeshFilter), typeof(MeshRenderer))]
    public class Chunk : MonoBehaviour
    {
        public delegate void ReportUpdateCompletion(Chunk senderChunk);

        public bool Initialized { get; private set; } = false;
        public VoxelObj ParentVoxelObj { get; private set; }
        public int3 PositionInVoxelMap { get; private set; }

        public int CurrentlyRenderedVoxelCount { get; private set; }

        public Renderer MyRenderer { get; private set; }
        public MeshCollider MyMeshCollider { get; private set; }
        private MeshFilter myMeshFilter;

        private bool currentlyUpdating;
        private bool pendingUpdate;
        private NativeArray<VoxelData> pendingUpdateMapInput;
        private int3 pendingUpdateMapDimensions;
        private bool pendingUpdateMeshCollider;
        private List<ReportUpdateCompletion> pendingCallOnCompletionList = new List<ReportUpdateCompletion>();

        VoxelChunkMeshCreator meshCreator = new VoxelChunkMeshCreator();

        #region RuntimePrefab
        private static Chunk _runtimePrefab;
        public static Chunk RuntimePrefab
        {
            get
            {
                if (_runtimePrefab == null)
                {
                    GameObject obj = new GameObject();
                    obj.SetActive(false);
                    obj.name = "Chunk";

                    obj.AddComponent<MeshCollider>();
                    obj.AddComponent<MeshFilter>();
                    obj.AddComponent<MeshRenderer>();

                    _runtimePrefab = obj.AddComponent<Chunk>();
                }

                return _runtimePrefab;
            }
            private set { }
        }
        #endregion

        private void Awake()
        {
            MyRenderer = gameObject.GetComponent<Renderer>();
            myMeshFilter = gameObject.GetComponent<MeshFilter>();
            MyMeshCollider = gameObject.GetComponent<MeshCollider>();
            MyMeshCollider.enabled = false;
        }

        internal void Initialize(VoxelObj parentVoxelObj, int3 positionInVoxelMap)
        {
            if (Initialized)
            {
                throw new InvalidOperationException("Chunk has already been initialized");
            }
            this.PositionInVoxelMap = positionInVoxelMap;
            this.ParentVoxelObj = parentVoxelObj;
            Initialized = true;
        }

        public void UpdateChunkImmediate(NativeArray<VoxelData> mapInput, int3 mapDimensions, bool generateMeshCollider)
        {
            UpdateChunkInternal(mapInput, mapDimensions, generateMeshCollider, true);
        }

        public void UpdateChunk(NativeArray<VoxelData> mapInput, int3 mapDimensions, bool generateMeshCollider, ReportUpdateCompletion callOnCompletion = null)
        {
            UpdateChunkInternal(mapInput, mapDimensions, generateMeshCollider, false, callOnCompletion);
        }

        private void UpdateChunkInternal(NativeArray<VoxelData> mapInput, int3 mapDimensions, bool generateMeshCollider, bool immediate, ReportUpdateCompletion callOnCompletion = null)
        {
            if (Initialized == false)
            {
                throw new InvalidOperationException($"Could not update chunk: Not initialized. Call {nameof(Initialize)} method first.");
            }

            if (this == null)
            {
                throw new InvalidOperationException($"You have called {nameof(UpdateChunk)} or {nameof(UpdateChunkImmediate)} on a {nameof(Chunk)} that has already been destroyed");
            }

            //The "pendingUpdate" functionality is not mandatory but exists for performance reasons
            if (currentlyUpdating)
            {
                pendingUpdate = true;
                pendingUpdateMapInput = mapInput;
                pendingUpdateMapDimensions = mapDimensions;
                pendingUpdateMeshCollider = generateMeshCollider;
                pendingCallOnCompletionList.Add(callOnCompletion);
                return;
            }
            currentlyUpdating = true;

            if (immediate)
            {
                var meshData = meshCreator.CreateMeshDataImmediate(mapInput, mapDimensions, PositionInVoxelMap);
                applyMesh(meshData);
            }
            else
            {
                Action<VoxelMeshData> onCompletion = (VoxelMeshData meshData) => { applyMesh(meshData); };
                meshCreator.CreateMeshData(mapInput, mapDimensions, PositionInVoxelMap, onCompletion);
            }
            return;

            void applyMesh(VoxelMeshData voxelMeshData)
            {
                bool chunkHasBeenDestroyed = (this == false);
                if (chunkHasBeenDestroyed)
                {
                    if(callOnCompletion != null) callOnCompletion.Invoke(this);
                    foreach (ReportUpdateCompletion report in pendingCallOnCompletionList) report.Invoke(this);
                    pendingCallOnCompletionList.Clear();

                    return;
                }

                CurrentlyRenderedVoxelCount = voxelMeshData.RenderedVoxels;

                myMeshFilter.mesh.Clear();
                myMeshFilter.mesh.vertices = voxelMeshData.Vertices;
                myMeshFilter.mesh.triangles = voxelMeshData.Triangles;
                myMeshFilter.mesh.colors = voxelMeshData.VertexColors;
                myMeshFilter.mesh.normals = voxelMeshData.Normals;

                if (generateMeshCollider)
                {
                    if (myMeshFilter.mesh.vertices.Length > 0)
                    {
                        MyMeshCollider.enabled = true;
                        MyMeshCollider.sharedMesh = myMeshFilter.mesh;
                    }
                    else
                    {
                        MyMeshCollider.enabled = false;
                    }
                }
                else MyMeshCollider.enabled = false;


                currentlyUpdating = false;
                if (callOnCompletion != null) callOnCompletion.Invoke(this);

                if (pendingUpdate)
                {
                    pendingUpdate = false;

                    IEnumerable<ReportUpdateCompletion> pendingCallOnCompletions = pendingCallOnCompletionList;
                    UpdateChunkInternal(pendingUpdateMapInput, pendingUpdateMapDimensions, pendingUpdateMeshCollider, immediate, pendingCallOnCompletions);
                    pendingCallOnCompletionList = new List<ReportUpdateCompletion>();
                }
            }
        }

        private void UpdateChunkInternal(NativeArray<VoxelData> mapInput, int3 mapDimensions, bool generateMeshCollider, bool immediate, IEnumerable<ReportUpdateCompletion> callOnCompletion)
        {
            UpdateChunkInternal(mapInput, mapDimensions, generateMeshCollider, immediate, onUpdateCompletion);

            void onUpdateCompletion(Chunk sender)
            {
                foreach (ReportUpdateCompletion del in callOnCompletion)
                {
                    del.Invoke(sender);
                }
            }
        }
    }
}