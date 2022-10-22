using UnityEngine;

namespace VoxelEngine.ConnectedComponent.ParallelInterruptedAlgorithm
{
    [CreateAssetMenu(fileName = "Parallel Interruped Gravity Extractor", menuName = "Voxel Engine/ConnectedComponent/Connected Component Extractors/Parallel Interruped Gravity Extractor", order = 2)]
    public class ParallelInterruptedGravityExtractor : ConnectedComponentExtractor
    {
        ParallelInterruptedAlgorithm parallelInterrupted = new ParallelInterruptedAlgorithm(new PiGravityBlobExtractor());

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