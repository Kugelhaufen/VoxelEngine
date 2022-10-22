using UnityEngine;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm
{
    [CreateAssetMenu(fileName = "Parallel Interruped Extractor", menuName = "Voxel Engine/ConnectedComponent/Connected Component Extractors/Parallel Interruped Extractor", order = 1)]
    public class ParallelInterruptedExtractor : ConnectedComponentExtractor
    {
        ParallelInterruptedAlgorithm parallelInterrupted = new ParallelInterruptedAlgorithm(new PiBlobExtractor());

        public override void ConnectedComponentExtraction(VoxelObj voxelObj, int minBlobVoxelCountForExtraction, ExtractionCallBack callBack = null)
        {
            parallelInterrupted.ConnectedComponentExtraction(voxelObj, minBlobVoxelCountForExtraction, callBack);
        }

        public override void ConnectedComponentExtractionImmediate(VoxelObj voxelObj, int minBlobVoxelCountForExtraction, ExtractionCallBack callBack = null)
        {
            parallelInterrupted.ConnectedComponentExtractionImmediate(voxelObj, minBlobVoxelCountForExtraction, callBack);
        }
    }
}