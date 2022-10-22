using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VoxelEngine
{
    public unsafe struct CounterInterlocked : IDisposable
    {
        private readonly Allocator myAllocator;

        [NativeDisableUnsafePtrRestriction] private int* counter;

        public int Count
        {
            get
            {
                return *counter;
            }

            set
            {
                *counter = value;
            }
        }

        public CounterInterlocked(Allocator _allocator)
        {
            myAllocator = _allocator;
            counter = (int*)UnsafeUtility.Malloc(sizeof(int), 4, myAllocator);
            Count = 0;
        }

        public int Increment()
        {
            return Interlocked.Increment(ref *counter);
        }

        public int Add(int value)
        {
            return Interlocked.Add(ref *counter, value);
        }

        public void Dispose()
        {
            UnsafeUtility.Free(counter, myAllocator);
        }
    }
}