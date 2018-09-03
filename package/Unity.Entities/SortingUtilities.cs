using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Entities
{
    struct SortingUtilities
    {
        public static unsafe void InsertSorted(ComponentType* data, int length, ComponentType newValue)
        {
            while (length > 0 && newValue < data[length-1])
            {
                data[length] = data[length-1];
                --length;
            }
            data[length] = newValue;
        }

        public static unsafe void InsertSorted(ComponentTypeInArchetype* data, int length, ComponentType newValue)
        {
            var newVal= new ComponentTypeInArchetype(newValue);
            while (length > 0 && newVal < data[length-1])
            {
                data[length] = data[length-1];
                --length;
            }
            data[length] = newVal;
        }

    }

    /// <summary>
    /// Merge sort index list referencing NativeArray values.
    /// Provide list of shared values, indices to shared values, and lists of source i
    /// value indices with identical shared value.
    /// 
    /// As an example:
    /// Given Source NativeArray: AAABBCCAB
    /// 
    /// Provides:
    ///     Shared value indices: 000112201
    ///     Shared value counts: 432
    ///     Shared values: ABC
    ///     Sorted indices: 012734856
    ///     Shared value start offsets (into sorted indices): 047
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct NativeArraySharedValues<T> : IDisposable
        where T : struct, IComparable<T>
    {
        private NativeArray<int> m_Buffer;
        [ReadOnly] private NativeArray<T> m_Source;
        private int m_SortedBuffer;

        public NativeArraySharedValues(NativeArray<T> source,Allocator allocator)
        {
            m_Buffer = new NativeArray<int>((source.Length*4)+1,allocator);
            m_Source = source;
            m_SortedBuffer = 0;
        }

        [ComputeJobOptimization]
        struct InitializeIndices : IJobParallelFor
        {
            public NativeArray<int> buffer;

            public void Execute(int index)
            {
                buffer[index] = index;
            }
        }

        [ComputeJobOptimization]
        struct MergeSortedPairs : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<int> buffer;
            [ReadOnly] public NativeArray<T> source;
            public int sortedCount;
            public int outputBuffer;

            public void Execute(int index)
            {
                int mergedCount = sortedCount * 2;
                int offset = (index * mergedCount);
                int inputOffset = (outputBuffer ^ 1) * source.Length;
                int outputOffset = (outputBuffer) * source.Length;
                int leftCount = sortedCount;
                int rightCount = sortedCount;
                var leftNext = 0;
                var rightNext = 0;

                for (int i = 0; i < mergedCount; i++)
                {
                    if ((leftNext < leftCount) && (rightNext < rightCount))
                    {
                        var leftIndex = buffer[inputOffset + offset + leftNext];
                        var rightIndex = buffer[inputOffset + offset + leftCount + rightNext];
                        var leftValue = source[leftIndex];
                        var rightValue = source[rightIndex];

                        if (rightValue.CompareTo(leftValue) < 0)
                        {
                            buffer[outputOffset+ offset + i] = rightIndex;
                            rightNext++;
                        }
                        else
                        {
                            buffer[outputOffset + offset + i] = leftIndex;
                            leftNext++;
                        }
                    }
                    else if (leftNext < leftCount)
                    {
                        var leftIndex = buffer[inputOffset + offset + leftNext];
                        buffer[outputOffset + offset + i] = leftIndex;
                        leftNext++;
                    }
                    else
                    {
                        var rightIndex = buffer[inputOffset + offset + leftCount + rightNext];
                        buffer[outputOffset+ offset + i] = rightIndex;
                        rightNext++;
                    }
                }
            }
        }

        [ComputeJobOptimization]
        struct MergeLeft : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<int> buffer;
            [ReadOnly] public NativeArray<T> source;
            public int leftCount;
            public int rightCount;
            public int startIndex;
            public int outputBuffer;
            
            // On left, equal is equivalent to less-than
            int FindInsertNext(int startOffset, int minNext, int maxNext, T testValue)
            {
                if (minNext == maxNext)
                {
                    var index = buffer[startOffset + minNext];
                    var value = source[index];
                    var compare = testValue.CompareTo(value);
                    if (compare <= 0)
                    {
                        return minNext;
                    }
                    return minNext + 1;
                }
                int midNext = minNext + (maxNext - minNext) / 2;
                {
                    var index = buffer[startOffset + midNext];
                    var value = source[index];
                    var compare = testValue.CompareTo(value);
                    if (compare <= 0)
                    {
                        return FindInsertNext(startOffset, minNext, math.max(midNext - 1,minNext), testValue);
                    }
                }
                return FindInsertNext(startOffset,math.min(midNext+1,maxNext),maxNext,testValue);
            }
            
            public void Execute(int leftNext)
            {
                int inputOffset  = (outputBuffer ^ 1) * source.Length;
                int outputOffset = (outputBuffer) * source.Length;
                var leftIndex    = buffer[inputOffset + startIndex + leftNext];
                var leftValue    = source[leftIndex];
                var rightNext    = FindInsertNext(inputOffset + startIndex + leftCount, 0, rightCount - 1, leftValue);
                var mergeNext    = leftNext + rightNext;
                
                buffer[outputOffset + startIndex + mergeNext] = leftIndex;
            }
        }
        
        [ComputeJobOptimization]
        struct MergeRight : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<int> buffer;
            [ReadOnly] public NativeArray<T> source;
            public int leftCount;
            public int rightCount;
            public int startIndex;
            public int outputBuffer;

            // On right, equal is equivalent to greater-than
            int FindInsertNext(int startOffset, int minNext, int maxNext, T testValue)
            {
                if (minNext == maxNext)
                {
                    var index = buffer[startOffset + minNext];
                    var value = source[index];
                    var compare = testValue.CompareTo(value);
                    if (compare < 0)
                    {
                        return minNext;
                    }
                    return minNext + 1;
                }
                int midNext = minNext + (maxNext - minNext) / 2;
                {
                    var index = buffer[startOffset + midNext];
                    var value = source[index];
                    var compare = testValue.CompareTo(value);
                    if (compare < 0)
                    {
                        return FindInsertNext(startOffset, minNext, math.max(midNext - 1,minNext), testValue);
                    }
                }
                return FindInsertNext(startOffset,math.min(midNext+1,maxNext),maxNext,testValue);
            }

            public void Execute(int rightNext)
            {
                int inputOffset   = (outputBuffer ^ 1) * source.Length;
                int outputOffset  = (outputBuffer) * source.Length;
                var rightIndex    = buffer[inputOffset + startIndex + leftCount + rightNext];
                var rightValue    = source[rightIndex];
                var leftNext      = FindInsertNext(inputOffset + startIndex, 0, leftCount - 1, rightValue);
                var mergeNext     = rightNext + leftNext;
                
                buffer[outputOffset + startIndex + mergeNext] = rightIndex;
            }
        }

        [ComputeJobOptimization]
        struct CopyRemainder : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<int> buffer;
            [ReadOnly] public NativeArray<T> source;
            public int startIndex;
            public int outputBuffer;

            public void Execute(int index)
            {
                int inputOffset = (outputBuffer ^ 1) * source.Length;
                int outputOffset = (outputBuffer) * source.Length;
                int outputIndex = outputOffset + startIndex + index;
                int inputIndex = inputOffset + startIndex + index;
                int valueIndex = buffer[inputIndex];
                buffer[outputIndex] = valueIndex;
            }
        }

        JobHandle MergeSortedLists(JobHandle inputDeps, int sortedCount, int outputBuffer)
        {
            var pairCount = m_Source.Length / (sortedCount*2);

            JobHandle mergeSortedPairsJobHandle = inputDeps;
            
            if (pairCount <= 4)
            {
                for (int i = 0; i < pairCount; i++)
                {
                    var mergeRemainderLeftJob = new MergeLeft
                    {
                        startIndex = i * sortedCount * 2,
                        buffer = m_Buffer,
                        source = m_Source,
                        leftCount = sortedCount,
                        rightCount = sortedCount,
                        outputBuffer = outputBuffer
                    };
                    // There's no overlap, but write to the same array, so extra dependency:
                    mergeSortedPairsJobHandle = mergeRemainderLeftJob.Schedule(sortedCount, 64, mergeSortedPairsJobHandle);
                
                    var mergeRemainderRightJob = new MergeRight
                    {
                        startIndex = i * sortedCount * 2,
                        buffer = m_Buffer,
                        source = m_Source,
                        leftCount = sortedCount,
                        rightCount = sortedCount,
                        outputBuffer = outputBuffer
                    };
                    // There's no overlap, but write to the same array, so extra dependency:
                    mergeSortedPairsJobHandle = mergeRemainderRightJob.Schedule(sortedCount, 64, mergeSortedPairsJobHandle);
                }
            }
            else
            {
                var mergeSortedPairsJob = new MergeSortedPairs
                {
                    buffer = m_Buffer,
                    source = m_Source,
                    sortedCount = sortedCount,
                    outputBuffer = outputBuffer
                };
                mergeSortedPairsJobHandle = mergeSortedPairsJob.Schedule(pairCount, (pairCount + 1) / 8, inputDeps);
            }
            
            var remainder = m_Source.Length - (pairCount * sortedCount * 2);
            if (remainder > sortedCount)
            {
                var mergeRemainderLeftJob = new MergeLeft
                {
                    startIndex = pairCount * sortedCount * 2,
                    buffer = m_Buffer,
                    source = m_Source,
                    leftCount = sortedCount,
                    rightCount = remainder-sortedCount,
                    outputBuffer = outputBuffer
                };
                // There's no overlap, but write to the same array, so extra dependency:
                var mergeLeftJobHandle = mergeRemainderLeftJob.Schedule(sortedCount, 64, mergeSortedPairsJobHandle);
                
                var mergeRemainderRightJob = new MergeRight
                {
                    startIndex = pairCount * sortedCount * 2,
                    buffer = m_Buffer,
                    source = m_Source,
                    leftCount = sortedCount,
                    rightCount = remainder-sortedCount,
                    outputBuffer = outputBuffer
                };
                // There's no overlap, but write to the same array, so extra dependency:
                var mergeRightJobHandle = mergeRemainderRightJob.Schedule(remainder-sortedCount, 64, mergeLeftJobHandle);
                return mergeRightJobHandle;
            }

            if (remainder > 0)
            {
                var copyRemainderPairJob = new CopyRemainder
                {
                    startIndex = pairCount * sortedCount * 2,
                    buffer = m_Buffer,
                    source = m_Source,
                    outputBuffer = outputBuffer
                };

                // There's no overlap, but write to the same array, so extra dependency:
                var copyRemainderPairJobHandle = copyRemainderPairJob.Schedule(remainder,(pairCount+1)/8,mergeSortedPairsJobHandle);
                return copyRemainderPairJobHandle;
            }

            return mergeSortedPairsJobHandle;
        }

        [ComputeJobOptimization]
        struct AssignSharedValues : IJob
        {
            public NativeArray<int> buffer;
            [ReadOnly] public NativeArray<T> source;
            public int sortedBuffer;

            public void Execute()
            {
                int sortedIndicesOffset = sortedBuffer * source.Length;
                int sharedValueIndicesOffset = (sortedBuffer ^ 1) * source.Length;
                int sharedValueIndexCountOffset = 2 * source.Length;
                int sharedValueStartIndicesOffset = 3 * source.Length;
                int sharedValueCountOffset = 4 * source.Length;

                int index = 0;
                int valueIndex = buffer[sortedIndicesOffset + index];
                var sharedValue = source[valueIndex];
                int sharedValueCount = 1;
                buffer[sharedValueIndicesOffset+valueIndex] = 0;
                buffer[sharedValueStartIndicesOffset + (sharedValueCount-1)] = index;
                buffer[sharedValueIndexCountOffset + (sharedValueCount - 1)] = 1;
                index++;

                while (index < source.Length)
                {
                    valueIndex = buffer[sortedIndicesOffset + index];
                    var value = source[valueIndex];
                    if (value.CompareTo(sharedValue) != 0)
                    {
                        sharedValueCount++;
                        sharedValue = value;
                        buffer[sharedValueStartIndicesOffset + (sharedValueCount-1)] = index;
                        buffer[sharedValueIndexCountOffset + (sharedValueCount - 1)] = 1;
                        buffer[sharedValueIndicesOffset + valueIndex] = sharedValueCount - 1;
                    }
                    else
                    {
                        buffer[sharedValueIndexCountOffset + (sharedValueCount - 1)]++;
                        buffer[sharedValueIndicesOffset + valueIndex] = sharedValueCount - 1;
                    }

                    index++;
                }

                buffer[sharedValueCountOffset] = sharedValueCount;
            }
        }

        JobHandle Sort(JobHandle inputDeps)
        {
            int sortedCount = 1;
            int outputBuffer = 1;
            do
            {
                inputDeps = MergeSortedLists(inputDeps, sortedCount, outputBuffer);
                sortedCount *= 2;
                outputBuffer ^= 1;
            } while (sortedCount < m_Source.Length);
            m_SortedBuffer = outputBuffer ^ 1;

            return inputDeps;
        }

        JobHandle ResolveSharedGroups(JobHandle inputDeps)
        {
            var assignSharedValuesJob = new AssignSharedValues
            {
                buffer = m_Buffer,
                source = m_Source,
                sortedBuffer = m_SortedBuffer
            };
            var assignSharedValuesJobHandle = assignSharedValuesJob.Schedule(inputDeps);
            return assignSharedValuesJobHandle;
        }

        /// <summary>
        /// Schedule jobs to collect and sort shared values.
        /// </summary>
        /// <param name="inputDeps">Dependent JobHandle</param>
        /// <returns>JobHandle</returns>
        public JobHandle Schedule(JobHandle inputDeps)
        {
            if (m_Source.Length <= 1)
            {
                return inputDeps;
            }
            var initializeIndicesJob = new InitializeIndices
            {
                buffer = m_Buffer
            };
            var initializeIndicesJobHandle = initializeIndicesJob.Schedule(m_Source.Length, (m_Source.Length+1)/8, inputDeps);
            var sortJobHandle = Sort(initializeIndicesJobHandle);
            var resolveSharedGroupsJobHandle = ResolveSharedGroups(sortJobHandle);
            return resolveSharedGroupsJobHandle;
        }

        /// <summary>
        /// Indices into source NativeArray sorted by value
        /// </summary>
        /// <returns>Index NativeArray where each element refers to alement ini source NativeArray</returns>
        public unsafe NativeArray<int> GetSortedIndices()
        {
            int* rawIndices = ((int*) m_Buffer.GetUnsafeReadOnlyPtr()) + (m_SortedBuffer * m_Source.Length);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(rawIndices,m_Source.Length,Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            SortedIndicesSetSafetyHandle(ref arr);
#endif
            return arr;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Uncomment when NativeArrayUnsafeUtility includes these conditional checks
        // [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void SortedIndicesSetSafetyHandle(ref NativeArray<int> arr)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_Buffer));
        }
#endif

        /// <summary>
        /// Number of shared (unique) values in source NativeArray
        /// </summary>
        public int SharedValueCount => m_Buffer[m_Source.Length * 4];

        /// <summary>
        /// Index of shared value
        /// </summary>
        /// <param name="index">Index of source value</param>
        /// <returns></returns>
        public int GetSharedIndexBySourceIndex(int index)
        {
            int sharedValueIndicesOffset = (m_SortedBuffer^ 1) * m_Source.Length;
            int sharedValueIndex = m_Buffer[sharedValueIndicesOffset + index];
            return sharedValueIndex;
        }
        
        public unsafe NativeArray<int> GetSharedIndexArray()
        {
            // Capacity cannot be changed, so offset is valid.
            int* rawIndices = (int*) NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Buffer) + ((m_SortedBuffer^1) * m_Source.Length);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(rawIndices,m_Source.Length,Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            SharedIndexArraySetSafetyHandle(ref arr);
#endif
            return arr;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Uncomment when NativeArrayUnsafeUtility includes these conditional checks
        // [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void SharedIndexArraySetSafetyHandle(ref NativeArray<int> arr)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_Buffer));
        }
#endif

        /// <summary>
        /// Array of indices into source NativeArray which share the same source value
        /// </summary>
        /// <param name="index">Index of source value</param>
        /// <returns></returns>
        public NativeArray<int> GetSharedValueIndicesBySourceIndex(int index)
        {
            int sharedValueIndicesOffset = (m_SortedBuffer^ 1) * m_Source.Length;
            int sharedValueIndex = m_Buffer[sharedValueIndicesOffset + index];
            return GetSharedValueIndicesBySharedIndex(sharedValueIndex);
        }

        /// <summary>
        /// Number of values which share the same value.
        /// </summary>
        /// <param name="index">Number of values which share the same value.</param>
        /// <returns></returns>
        public int GetSharedValueIndexCountBySourceIndex(int index)
        {
            int sharedValueIndex = GetSharedIndexBySourceIndex(index);
            int sharedValueIndexCountOffset = 2 * m_Source.Length;
            int sharedValueIndexCount = m_Buffer[sharedValueIndexCountOffset + sharedValueIndex];
            return sharedValueIndexCount;
        }
        
        public unsafe NativeArray<int> GetSharedValueIndexCountArray()
        {
            // Capacity cannot be changed, so offset is valid.
            int* rawIndices = (int*) NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Buffer) + (2 * m_Source.Length);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(rawIndices,m_Source.Length,Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            SharedValueIndexCountArraySetSafetyHandle(ref arr);
#endif
            return arr;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Uncomment when NativeArrayUnsafeUtility includes these conditional checks
        // [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void SharedValueIndexCountArraySetSafetyHandle(ref NativeArray<int> arr)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_Buffer));
        }
#endif

        /// <summary>
        /// Array of indices into source NativeArray which share the same source value
        /// </summary>
        /// <param name="index">Index of shared value</param>
        /// <returns></returns>
        public unsafe NativeArray<int> GetSharedValueIndicesBySharedIndex(int index)
        {
            int sharedValueIndexCountOffset = 2 * m_Source.Length;
            int sharedValueIndexCount = m_Buffer[sharedValueIndexCountOffset + index];
            int sharedValueStartIndicesOffset = 3 * m_Source.Length;
            int sharedValueStartIndex = m_Buffer[sharedValueStartIndicesOffset + index];
            int sortedValueOffset = m_SortedBuffer * m_Source.Length;

            // Capacity cannot be changed, so offset is valid.
            int* rawIndices = (int*) NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Buffer) + (sortedValueOffset + sharedValueStartIndex);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(rawIndices,sharedValueIndexCount,Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            SharedValueIndicesSetSafetyHandle(ref arr);
#endif
            return arr;
        }
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Uncomment when NativeArrayUnsafeUtility includes these conditional checks
        // [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void SharedValueIndicesSetSafetyHandle(ref NativeArray<int> arr)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_Buffer));
        }
#endif

        /// <summary>
        /// Get internal buffer for disposal
        /// </summary>
        /// <returns></returns>
        public NativeArray<int> GetBuffer()
        {
            return m_Buffer;
        }

        /// <summary>
        /// Dispose internal buffer
        /// </summary>
        public void Dispose()
        {
            m_Buffer.Dispose();
        }
    }
}
