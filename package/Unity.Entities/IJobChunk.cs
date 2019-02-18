﻿using System;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [JobProducerType(typeof(JobChunkExtensions.JobChunkLiveFilter_Process<>))]
    public interface IJobChunk
    {
        void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex);
    }

    public static class JobChunkExtensions
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [NativeContainer]
        internal struct EntitySafetyHandle
        {
            public AtomicSafetyHandle m_Safety;
        }
#endif

        internal struct JobDataLiveFilter<T> where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#pragma warning disable 414
            [ReadOnly] public EntitySafetyHandle safety;
#pragma warning restore
#endif
            public ComponentChunkIterator iterator;
            public T data;
        }

        public static unsafe JobHandle Schedule<T>(this T jobData, ComponentGroup group, JobHandle dependsOn = default(JobHandle))
            where T : struct, IJobChunk
        {
            return ScheduleInternal(ref jobData, group, dependsOn, ScheduleMode.Batched);
        }

        public static void Run<T>(this T jobData, ComponentGroup group)
            where T : struct, IJobChunk
        {
            ScheduleInternal(ref jobData, group, default(JobHandle), ScheduleMode.Run);
        }

        internal static unsafe JobHandle ScheduleInternal<T>(ref T jobData, ComponentGroup group, JobHandle dependsOn, ScheduleMode mode)
            where T : struct, IJobChunk
        {
            ComponentChunkIterator iterator = group.GetComponentChunkIterator();

            JobDataLiveFilter<T> fullData = new JobDataLiveFilter<T>
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // All IJobChunk jobs have a safety handle for the Entity type to ensure that BeforeStructuralChange throws an error if
                // jobs without any other safety handles are still running (haven't been synced).
                safety = new EntitySafetyHandle{m_Safety = group.SafetyManager.GetSafetyHandle(TypeManager.GetTypeIndex<Entity>(), true)},
#endif
                data = jobData,
                iterator = iterator,
            };
            var totalChunks = group.CalculateNumberOfChunksWithoutFiltering();

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref fullData),
                JobChunkLiveFilter_Process<T>.Initialize(),
                dependsOn,
                mode);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, totalChunks, 1);
        }

        internal struct JobChunkLiveFilter_Process<T>
            where T : struct, IJobChunk
        {
            public static IntPtr jobReflectionData;

            public static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobDataLiveFilter<T>),
                        typeof(T), JobType.ParallelFor, (ExecuteJobFunction)Execute);

                return jobReflectionData;
            }
            public delegate void ExecuteJobFunction(ref JobDataLiveFilter<T> data, System.IntPtr additionalPtr, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public unsafe static void Execute(ref JobDataLiveFilter<T> jobData, System.IntPtr additionalPtr, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                ExecuteInternal(ref jobData, ref ranges, jobIndex);
            }

            internal unsafe static void ExecuteInternal(ref JobDataLiveFilter<T> jobData, ref JobRanges ranges, int jobIndex)
            {
                int chunkIndex, end;
                while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out chunkIndex, out end))
                {
                    jobData.iterator.MoveToChunkWithoutFiltering(chunkIndex);
                    if (!jobData.iterator.MatchesFilter())
                        continue;

                    jobData.iterator.GetFilteredChunkAndEntityIndices(out var filteredChunkIndex, out var entityOffset);

                    var chunk = jobData.iterator.GetCurrentChunk();
                    jobData.data.Execute(chunk, filteredChunkIndex, entityOffset);
                }
            }
        }

    }
}
