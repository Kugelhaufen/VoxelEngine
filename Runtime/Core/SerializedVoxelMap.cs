using System.IO;
using System.IO.Compression;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngine
{
    /// <summary>
    /// This class can be used to serialize a <see cref="VoxelMap"/> using Unity's scriptableObjects.
    /// </summary>
    public class SerializedVoxelMap : ScriptableObject
    {
        public int3 VoxelMapDimensions => voxelMapDimensions;
        [SerializeField, HideInInspector] private int3 voxelMapDimensions;

        // The VoxelData struct is not serialized directly as this is very inefficient.
        // Instead the struct is represented by a byte array that can be comporessed more easily
        /// <summary>
        /// Contrains the values of the <see cref="VoxelData"/> structs.
        /// The array is compressed using the <see cref="DeflateStream"/> class form <see cref="System.IO.Compression"/>
        /// The array contains 5 entries per voxel (in the following order):
        /// <br>Filled (1 = true; 0 = false)</br> 
        /// <br>R</br> 
        /// <br>G</br> 
        /// <br>B</br> 
        /// <br>A</br> 
        /// </summary>
        [SerializeField, HideInInspector] private byte[] compressedVoxelDataValues;

        public VoxelMap GetVoxelMap()
        {
            VoxelData[] voxelData = ParseVoxelDataArray();

            VoxelMap voxelMap = new VoxelMap(voxelData, voxelMapDimensions);
            return voxelMap;
        }

        public void SetData(int3 voxelMapDimension, VoxelData[] voxelMap)
        {
            this.voxelMapDimensions = voxelMapDimension;
            SetVoxelDataArray(voxelMap);
        }

        private void SetVoxelDataArray(VoxelData[] voxelMap)
        {
            var uncompressedVoxelDataArray = new byte[voxelMap.Length * 5];

            for (int voxelIndex = 0; voxelIndex != voxelMap.Length; voxelIndex++)
            {
                VoxelData currentData = voxelMap[voxelIndex];

                byte filledValue = 0;
                if (currentData.Filled) filledValue = 1;

                int startIndex = voxelIndex * 5;
                uncompressedVoxelDataArray[startIndex + 0] = filledValue;
                uncompressedVoxelDataArray[startIndex + 1] = currentData.r;
                uncompressedVoxelDataArray[startIndex + 2] = currentData.g;
                uncompressedVoxelDataArray[startIndex + 3] = currentData.b;
                uncompressedVoxelDataArray[startIndex + 4] = currentData.a;
            }

            MemoryStream compressedRgbaValues = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(compressedRgbaValues, System.IO.Compression.CompressionLevel.Optimal))
            {
                dstream.Write(uncompressedVoxelDataArray, 0, uncompressedVoxelDataArray.Length);
            }
            compressedVoxelDataValues = compressedRgbaValues.ToArray();
        }

        private VoxelData[] ParseVoxelDataArray()
        {
            MemoryStream input = new MemoryStream(compressedVoxelDataValues);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            byte[] uncompressedVoxelDataValues = output.ToArray();

            int voxelCount = uncompressedVoxelDataValues.Length / 5;
            VoxelData[] parsedVoxelData = new VoxelData[voxelCount];

            for (int voxelIndex = 0; voxelIndex != voxelCount; voxelIndex++)
            {
                VoxelData currentVoxelData = new VoxelData();

                int arrayStartIndex = voxelIndex * 5;

                currentVoxelData.Filled = (uncompressedVoxelDataValues[arrayStartIndex] == 1);
                currentVoxelData.r = uncompressedVoxelDataValues[arrayStartIndex + 1];
                currentVoxelData.g = uncompressedVoxelDataValues[arrayStartIndex + 2];
                currentVoxelData.b = uncompressedVoxelDataValues[arrayStartIndex + 3];
                currentVoxelData.a = uncompressedVoxelDataValues[arrayStartIndex + 4];

                parsedVoxelData[voxelIndex] = currentVoxelData;
            }

            return parsedVoxelData;
        }
    }
}