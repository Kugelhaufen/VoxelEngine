using System;
using UnityEngine;

namespace VoxelEngine.ConnectedComponent
{
    public abstract class ConnectedComponentExtractor : ScriptableObject
    {
        public delegate void ExtractionCallBack(VoxelObj originalVoxelObj, bool originalVoxelObjEdited, Action applyVoxelObjMapEdit, VoxelObj[] voxelObjs);
        public abstract void ConnectedComponentExtraction(VoxelObj voxelObj, int minBlobVoxelCountForExtraction, ExtractionCallBack callBack = null);
        public abstract void ConnectedComponentExtractionImmediate(VoxelObj voxelObj, int minBlobVoxelCountForExtraction, ExtractionCallBack callBack = null);
    }
}