using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelEngine.ConnectedComponent
{
    [BurstCompile]
    public struct UnionFindJob : IJob
    {
        public NativeQueue<int2> mergeCalls;

        public NativeArray<int> pointers;
        public NativeArray<int> rootMemberAmmount;

        public void Execute()
        {
            for (int i = 0; i != pointers.Length; i++)
            {
                pointers[i] = i;
                rootMemberAmmount[i] = 1;
            }

            while(mergeCalls.TryDequeue(out int2 mergeCall))
            {
                int rootX = FindRootPointerPathCompression(mergeCall.x);
                int rootY = FindRootPointerPathCompression(mergeCall.y);
                if (rootX == rootY) continue;

                if (rootMemberAmmount[rootX] < rootMemberAmmount[rootY])
                {
                    pointers[rootX] = rootY;
                    rootMemberAmmount[rootY] += rootMemberAmmount[rootX];
                }
                else
                {
                    pointers[rootY] = rootX;
                    rootMemberAmmount[rootX] += rootMemberAmmount[rootY];
                }
            }
        }

        private int FindRootPointerPathCompression(int pointer)
        {
            int rootPointer = pointer;
            while (rootPointer != pointers[rootPointer]) rootPointer = pointers[rootPointer];

            //path compression
            while (pointer != rootPointer)
            {
                int next = pointers[pointer];
                pointers[pointer] = rootPointer;
                pointer = next;
            }

            return rootPointer;
        }
    }
}