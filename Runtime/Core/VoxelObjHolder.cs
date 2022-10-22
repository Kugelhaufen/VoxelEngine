using UnityEngine;
using System;

namespace VoxelEngine
{
    public class VoxelObjHolder : MonoBehaviour
    {
        private VoxelObj _myVoxelObj = null;
        public VoxelObj MyVoxelObj
        {
            get => _myVoxelObj;

            internal set
            {
                if(value.GetVoxelObjHolder() == this)
                {
                    _myVoxelObj = value;
                }
                else
                {
                    string error = string.Format("{0}.{1} is != this {2}. Use {0}.{3} to set all corresponding variables accordingly",
                        nameof(VoxelObj),
                        nameof(VoxelObj.GetVoxelObjHolder),
                        nameof(VoxelObjHolder), 
                        nameof(VoxelObj.SetMyVoxelObjHolder));
                    throw new ArgumentException(error);
                }
            }
        }

        private static VoxelObjHolder _runtimePrefab;
        public static VoxelObjHolder RuntimePrefab
        {
            get
            {
                if (_runtimePrefab == null)
                {
                    _runtimePrefab = new GameObject().AddComponent<VoxelObjHolder>();
                    _runtimePrefab.gameObject.name = "VoxelObjHolder";
                    _runtimePrefab.gameObject.SetActive(false);
                }
                return _runtimePrefab;
            }

            private set { }
        }

        public void SetGeneratedGameObjectName(string VoxelObjName)
        {
            this.gameObject.name = GenerateGameObjectName(VoxelObjName);
        }

        public static string GenerateGameObjectName(string VoxelObjName)
        {
            return string.Format("{0} - {1}", VoxelObjName, nameof(VoxelObjHolder));
        }
    }
}
