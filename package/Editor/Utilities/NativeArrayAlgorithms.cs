using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.MemoryProfiler.Containers
{
    namespace Unsafe
    {
        internal static class NativeArrayAlgorithms
        {
            /// <summary>
            /// Port of MSDN's internal method for QuickSort, roughly 10% slower than it's counterpart, can work with native array containers inside a jobified environment.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="array"></param>
            /// <param name="startIndex"></param>
            /// <param name="length"></param>
            public static void IntrospectiveSort<T>(NativeArray<T> array, int startIndex, int length) where T : struct, IComparable<T>
            {
#if MEM_PROFILER_ALGORITHM_CHECK
                if (length < 0 || length > array.Length)
                    throw new ArgumentOutOfRangeException("length should be in the range [0, array.Length].");
                if (startIndex < 0 || startIndex > length - 1)
                    throw new ArgumentOutOfRangeException("startIndex should in the range [0, length).");
#endif
                if (length < 2)
                    return;

                unsafe
                {
                    NativeArrayData<T> data = new NativeArrayData<T>((byte*)array.GetUnsafePtr());
                    IntroSortInternal(ref data, startIndex, length + startIndex - 1, GetMaxDepth(array.Length), GetPartitionThreshold());
                }
            }

            unsafe struct NativeArrayData<T> where T : struct
            {
                [NativeDisableUnsafePtrRestriction]
                public readonly byte* ptr;
                public T aux_first;
                public T aux_second;
                public T aux_third;
                public NativeArrayData(byte * nativeArrayPtr)
                {
                    aux_first = default(T);
                    aux_second = aux_first;
                    aux_third = aux_first;

                    ptr = nativeArrayPtr;
                }

            }

            static void IntroSortInternal<T>(ref NativeArrayData<T> array, int low, int high, int depth, int partitionThreshold) where T : struct, IComparable<T>
            {
                while (high > low)
                {
                    int partitionSize = high - low + 1;
                    if(partitionSize <= partitionThreshold)
                    {
                        switch (partitionSize)
                        {
                            case 1:
                                return;
                            case 2:
                                SwapIfGreater(ref array, low, high);
                                return;
                            case 3:
                                SwapSortAscending(ref array, low, high - 1, high);
                                return;
                            default:
                                InsertionSort(ref array, low, high);
                                return;

                        }
                    }
                    else if (depth == 0)
                    {
                        Heapsort(ref array, low, high);
                        return;
                    }
                    --depth;

                    int pivot = PartitionRangeAndPlacePivot(ref array, low, high);
                    IntroSortInternal(ref array, pivot + 1, high, depth, partitionThreshold);
                    high = pivot - 1;
                }
            }
#if NET_4_6
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            static void Heapsort<T>(ref NativeArrayData<T> array, int low, int high) where T : struct, IComparable<T>
            {
                int rangeSize = high - low + 1;
                for (int i = rangeSize / 2; i >= 1; --i)
                {
                    DownHeap(ref array, i, rangeSize, low);
                }
                for (int i = rangeSize; i > 1; --i)
                {
                    Swap(ref array, low, low + i - 1);

                    DownHeap(ref array, 1, i - 1, low);
                }
            }
#if NET_4_6
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            unsafe static void DownHeap<T>(ref NativeArrayData<T> array, int i, int n, int low) where T : struct, IComparable<T>
            {
                var typeSize = UnsafeUtility.SizeOf<T>();
                UnsafeUtility.CopyPtrToStructure(array.ptr + ((low + i - 1) * typeSize), out array.aux_first);

                int child;
                while (i <= n / 2)
                {
                    child = 2 * i;
                    void* cChildAddr = array.ptr + ((low + child - 1) * typeSize);
                    void* nChildAddr = array.ptr + ((low + child) * typeSize);

                    UnsafeUtility.CopyPtrToStructure(cChildAddr, out array.aux_second);
                    UnsafeUtility.CopyPtrToStructure(nChildAddr, out array.aux_third);

                    if (child < n && array.aux_second.CompareTo(array.aux_third) < 0)
                    {
                        ++child;
                        cChildAddr = nChildAddr;
                        if (!(array.aux_first.CompareTo(array.aux_third) < 0))
                            break;
                    }
                    else
                    {
                        if (!(array.aux_first.CompareTo(array.aux_second) < 0))
                            break;
                    }

                    UnsafeUtility.MemCpy(array.ptr + ((low + i - 1) * typeSize), cChildAddr, typeSize);
                    i = child;
                }
                UnsafeUtility.CopyStructureToPtr(ref array.aux_first, array.ptr + ((low + i - 1) * typeSize));
            }

#if NET_4_6
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            unsafe static void InsertionSort<T>(ref NativeArrayData<T> array, int low, int high) where T : struct, IComparable<T>
            {
                int i, j;
                var typeSize = UnsafeUtility.SizeOf<T>();
                for (i = low; i < high; ++i)
                {
                    j = i;
                    UnsafeUtility.CopyPtrToStructure(array.ptr + ((i + 1) * typeSize), out array.aux_first);
                    while (j >= low)
                    {
                        UnsafeUtility.CopyPtrToStructure(array.ptr + (j * typeSize), out array.aux_second);
                        if (!(array.aux_first.CompareTo(array.aux_second) < 0))
                            break;

                        UnsafeUtility.CopyStructureToPtr(ref array.aux_second, array.ptr + ((j + 1) * typeSize));
                        j--;
                    }
                    UnsafeUtility.CopyStructureToPtr(ref array.aux_first, array.ptr + ((j + 1) * typeSize));
                }
            }

            unsafe static int PartitionRangeAndPlacePivot<T>(ref NativeArrayData<T> array ,int low, int high) where T : struct, IComparable<T>
            {
                int mid = low + (high - low) / 2;
                var typeSize = UnsafeUtility.SizeOf<T>();
                // Sort low/high/mid in order to have the correct pivot.
                SwapSortAscending(ref array, low, mid, high);

                UnsafeUtility.CopyPtrToStructure(array.ptr + (mid * typeSize), out array.aux_second); // we use for swap only aux_first thus second and third are free to use

                Swap(ref array, mid, high - 1);
                int left = low, right = high - 1;

                while (left < right)
                {
                    while (true)
                    {
                        UnsafeUtility.CopyPtrToStructure(array.ptr + (++left * typeSize), out array.aux_first);
                        if (!(array.aux_first.CompareTo(array.aux_second) < 0))
                            break;

                    }

                    while (true)
                    {
                        UnsafeUtility.CopyPtrToStructure(array.ptr + (--right * typeSize), out array.aux_first);
                        if (!(array.aux_second.CompareTo(array.aux_first) < 0))
                            break;
                    }

                    if (left >= right)
                        break;

                    Swap(ref array, left, right);
                }

                Swap(ref array, left, (high - 1));
                return left;
            }

#if NET_4_6
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            unsafe static void SwapSortAscending<T>(ref NativeArrayData<T> array, int left, int mid, int right) where T : struct, IComparable<T>
            {
                var typeSize = UnsafeUtility.SizeOf<T>();
                void* leftAddr = array.ptr + (typeSize * left);
                void* midAddr = array.ptr + (typeSize * mid);
                void* rightAddr = array.ptr + (typeSize * right);

                UnsafeUtility.CopyPtrToStructure(leftAddr, out array.aux_first);
                UnsafeUtility.CopyPtrToStructure(midAddr, out array.aux_second);
                UnsafeUtility.CopyPtrToStructure(rightAddr, out array.aux_third);

                int bitmask = 0;
                if (array.aux_first.CompareTo(array.aux_second) > 0)
                    bitmask = 1;
                if (array.aux_first.CompareTo(array.aux_third) > 0)
                    bitmask |= 1 << 1;
                if (array.aux_second.CompareTo(array.aux_third) > 0)
                    bitmask |= 1 << 2;

                switch (bitmask)
                {

                    case 1:
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_second, leftAddr);
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_first, midAddr);
                        return;
                    case 3:
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_second, leftAddr);
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_third, midAddr);
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_first, rightAddr);
                        return;
                    case 4:
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_third, midAddr);
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_second, rightAddr);
                        return;
                    case 6:
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_third, leftAddr);
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_first, midAddr);
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_second, rightAddr);
                        return;
                    case 7:
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_third, leftAddr);
                        UnsafeUtility.CopyStructureToPtr(ref array.aux_first, rightAddr);
                        return;
                    default: //we are already ordered
                        return;
                }
            }

#if NET_4_6
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            static void SwapIfGreater<T>(ref NativeArrayData<T> array, int lhs, int rhs) where T : struct, IComparable<T>
            {
                if (lhs != rhs)
                {
                    unsafe
                    {
                        var typeSize = UnsafeUtility.SizeOf<T>();

                        void* leftAddr = array.ptr + (typeSize * lhs);
                        void* rightAddr = array.ptr + (typeSize * rhs);

                        UnsafeUtility.CopyPtrToStructure(leftAddr, out array.aux_first);
                        UnsafeUtility.CopyPtrToStructure(rightAddr, out array.aux_second);

                        if (array.aux_first.CompareTo(array.aux_second) > 0)
                        {
                            UnsafeUtility.MemCpy(rightAddr, leftAddr, typeSize);
                            UnsafeUtility.CopyStructureToPtr(ref array.aux_second, leftAddr);
                        }
                    }
                }
            }

#if NET_4_6
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            unsafe static void Swap<T>(ref NativeArrayData<T> array, int lhs, int rhs) where T : struct, IComparable<T>
            {
                var typeSize = UnsafeUtility.SizeOf<T>();
                void* leftAddr = array.ptr + (typeSize * lhs);
                void* rightAddr = array.ptr + (typeSize * rhs);

                UnsafeUtility.CopyPtrToStructure(leftAddr, out array.aux_first);
                UnsafeUtility.MemCpy(leftAddr, rightAddr, typeSize);
                UnsafeUtility.CopyStructureToPtr(ref array.aux_first, rightAddr);
            }

            static int GetMaxDepth(int length)
            {
                return 2 * UnityEngine.Mathf.FloorToInt((float)Math.Log(length, 2));
            }

            static int GetPartitionThreshold()
            {
                return 16;
            }
        }
    }
}