using System;

namespace VoxelEngine
{
    [Serializable]
    public struct VoxelData
    {
        /// <summary>
        /// Information whether  voxel is filled or empty. A voxel should only be rendered when it is "filled".
        /// </summary>
        public bool Filled
        {
            get => filled == 1;
            set
            {
                if(value == true)
                {
                    filled = 1;
                }
                else
                {
                    filled = 0;
                }
            }
        }
        /// <summary>
        /// 1 = filled, 0 = empty.
        /// (byte instead of bool for compatibility with IL2CPP)
        /// </summary>
        private byte filled;
        /// <summary>
        /// R value of the RGBA color space
        /// </summary>
        public byte r;
        /// <summary>
        /// G value of the RGBA color space
        /// </summary>
        public byte g;
        /// <summary>
        /// B value of the RGBA color space
        /// </summary>
        public byte b;
        /// <summary>
        /// A value of the RGBA color space
        /// </summary>
        public byte a;
    }
}