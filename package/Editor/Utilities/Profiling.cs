//#define MEMPROFILER_PROFILE_ROOTONLY
//#define MEMPROFILER_PROFILE_NOCUSTOM

#define MEMPROFILER_PROFILE_LOGFILE

using System;

namespace Unity.MemoryProfiler.Editor
{
    internal static class Profiling
    {
        public enum MarkerId
        {
            MemoryProfiler = 0,
            GroupedTable,
            SortedTable,
            ViewColumnNodeTypedGetRowValue,
            Sort,
            ArraySort,
            ColumnMatchQuery,
            ColumnMatchQueryLong,
            ColumnMatchQueryInt,
            ColumnFirstMatchQuery,
            ConstMatcherQuery,
            SubStringMatcherQuery,
            CreateSnapshotSchema,
            LoadCapture,
            LoadViewDefinitionFile,
            BuildViewDefinitionFile,
            Select,
            MergeColumn,
            CrawlManagedData,
            GroupByDuplicate,
            MergeSum,
            MergeSumPositive,
            MergeMin,
            MergeMax,
            MergeAverage,
            MergeMedian,
            MergeDeviation,
            NewDataChunk,
            ExpColumn,
            ExpTypeChange,
            ExpSelect,
            ExpSelectSetConditional,
            ExpFirstMatchSelect,
            ExpColumnMerge,
            ExpColumnGetValue,
            ExpTypeChangeGetValue,
            ExpSelectGetValue,
            ExpSelectSetConditionalGetValue,
            ExpFirstMatchSelectGetValue,
            ExpColumnMergeGetValue,
            ExpColumnGetComparableValue,
            ExpTypeChangeGetComparableValue,
            ExpSelectGetComparableValue,
            ExpSelectSetConditionalGetComparableValue,
            ExpFirstMatchSelectGetComparableValue,
            ExpColumnMergeGetComparableValue,
            ExpColumnGetValueString,
            ExpTypeChangeGetValueString,
            ExpSelectGetValueString,
            ExpSelectSetConditionalGetValueString,
            ExpFirstMatchSelectGetValueString,
            ExpColumnMergeGetValueString,

            // Must be last
            MarkerCount,
        }

        static public void StartProfiling(string filename)
        {
#if MEMPROFILER_PROFILE
#if MEMPROFILER_PROFILE_LOGFILE
            UnityEngine.Profiling.Profiler.logFile = filename;
            UnityEngine.Profiling.Profiler.enableBinaryLog = true;
#endif
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Audio, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU, true);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.GlobalIllumination, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.GPU, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Memory, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.NetworkMessages, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.NetworkOperations, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Physics, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Physics2D, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Rendering, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.UI, true);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.UIDetails, false);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Video, false);

            foreach (var r in AllMarkerRecorders())
            {
                r.Recorder.enabled = true;
            }
            UnityEngine.Profiling.Profiler.enabled = true;
            s_ProfilingStep = k_ProfilingStepMax;
            
            EditorCoroutineUtility.StartCoroutineOwnerless(StepProfiling());
#endif
        }

#if MEMPROFILER_PROFILE
        const int k_ProfilingStepMax = 5;
        static int s_ProfilingStep = 0;

        static IEnumerator StepProfiling()
        {
            do
            {
                yield return null;
                var sb = new System.Text.StringBuilder();
                if(UnityEngine.Event.current != null)
                {
                    sb.AppendFormat("Markers stats, step {0}({1})\n", k_ProfilingStepMax - s_ProfilingStep, UnityEngine.Event.current.type);
			    }
				else
				{
				    sb.AppendFormat("Markers stats, step {0}\n", k_ProfilingStepMax - s_ProfilingStep);
			    }
                foreach (var r in AllMarkerRecorders())
                {
                    sb.AppendFormat("{0}\t{1}\t{2}\n", r.Name, r.Recorder.sampleBlockCount, r.Recorder.elapsedNanoseconds);
                }
                UnityEngine.Debug.Log(sb.ToString());
                --s_ProfilingStep;
            } while (s_ProfilingStep > 0);

            StopProfiling();
        }

        static void StopProfiling()
        {
            UnityEngine.Profiling.Profiler.enabled = false;
            UnityEngine.Profiling.Profiler.enableBinaryLog = false;

            foreach (var r in AllMarkerRecorders())
            {
                r.Recorder.enabled = false;
            }
        }

        public static Unity.Profiling.ProfilerMarker GetMarker(MarkerId id)
        {
            return k_Markers[(int)id];
        }
        public struct MarkerRecorder
        {
            public MarkerId Id;
            public string Name;
            public Unity.Profiling.ProfilerMarker Marker;
            public UnityEngine.Profiling.Recorder Recorder;

        }
        public static IEnumerable<MarkerRecorder> AllMarkerRecorders()
        {
            for (int i = 0; i != (int)MarkerId.MarkerCount; ++i)
            {
                if (k_Recorders[i].isValid)
                {
                    yield return new MarkerRecorder()
                    {
                        Id = (MarkerId)i,
                        Name = k_MarkerNames[i],
                        Marker = k_Markers[i],
                        Recorder = k_Recorders[i]
                    };
                }
            }
        }
        
        static Profiling()
        {
            k_Markers = new Unity.Profiling.ProfilerMarker[(int)MarkerId.MarkerCount];
            k_Recorders = new UnityEngine.Profiling.Recorder[(int)MarkerId.MarkerCount];
            for (int i = 0; i != (int)MarkerId.MarkerCount; ++i)
            {
                k_Markers[i] = new Unity.Profiling.ProfilerMarker(k_MarkerNames[i]);
                k_Recorders[i] = UnityEngine.Profiling.Recorder.Get(k_MarkerNames[i]);
            }
        }
        private static Unity.Profiling.ProfilerMarker[] k_Markers;
        private static UnityEngine.Profiling.Recorder[] k_Recorders;

        private static string[] k_MarkerNames =
        {
            "Memory Profiler",
            "GroupedTable",
            "SortedTable",
            "ViewColumnNodeTyped.GetRowValue",
            "Sort",
            "Array Sort",
            "Column Match Query",
            "Column Match Query Long",
            "Column Match Query Int",
            "Column First Match Query",
            "Const Matcher Query",
            "SubString Matcher Query",
            "Create Snapshot Schema",
            "Load Capture",
            "Load View Definition File",
            "Build View Definition File",
            "Select",
            "Merge Column",
            "Crawl Managed Data",
            "Group By Duplicate",
            "Merge Sum",
            "Merge Sum Positive",
            "Merge Min",
            "Merge Max",
            "Merge Average",
            "Merge Median",
            "Merge Deviation",
            "New DataChunk",
            "ExpColumn",
            "ExpTypeChange",
            "ExpSelect",
            "ExpSelectSetConditional",
            "ExpFirstMatchSelect",
            "ExpColumnMerge",
            "ExpColumnGetValue",
            "ExpTypeChangeGetValue",
            "ExpSelectGetValue",
            "ExpSelectSetConditionalGetValue",
            "ExpFirstMatchSelectGetValue",
            "ExpColumnMergeGetValue",
            "ExpColumnGetComparableValue",
            "ExpTypeChangeGetComparableValue",
            "ExpSelectGetComparableValue",
            "ExpSelectSetConditionalGetComparableValue",
            "ExpFirstMatchSelectGetComparableValue",
            "ExpColumnMergeGetComparableValue",
            "ExpColumnGetValueString",
            "ExpTypeChangeGetValueString",
            "ExpSelectGetValueString",
            "ExpSelectSetConditionalGetValueString",
            "ExpFirstMatchSelectGetValueString",
            "ExpColumnMergeGetValueString",
        };
#else
        public class ScopeStub : System.IDisposable
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
        public struct MarkerStub
        {
            public ScopeStub Auto()
            {
                return null;
            }
        }
        public static MarkerStub GetMarker(MarkerId id)
        {
            return new MarkerStub();
        }
#endif
    }

#if MEMPROFILER_PROFILE
    // Will output to console the task timing
    internal class TaskTimer
    {
        public string Name;
        private Stopwatch m_Stopwatch;
        public TaskTimer(string name)
        {
            m_Stopwatch = new Stopwatch();
            Name = name;
        }
        public void Begin()
        {
            m_Stopwatch.Start();
        }
        public void End()
        {
            m_Stopwatch.Stop();
            UnityEngine.Debug.Log("Task " + Name + " time = " + m_Stopwatch.ElapsedMilliseconds + "ms, " + m_Stopwatch.ElapsedTicks + " ticks");
        }
    }

    
#endif
}
