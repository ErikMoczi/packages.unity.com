using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Utilities
{
    public class ProgressTracker : IProgressTracker, IDisposable
    {
        public int TaskCount { get; set; }

        public float Progress { get { return m_CurrentTask / (float)TaskCount; } }

        public uint UpdatesPerSecond
        {
            get { return (uint)(k_TicksPerSecond / m_UpdateFrequency); }
            set { m_UpdateFrequency = k_TicksPerSecond / Math.Max(value, 1); }
        }

        protected int m_CurrentTask = 0;

        protected string m_CurrentTaskTitle = "";

        protected long m_TimeStamp = 0;

        protected long m_UpdateFrequency = k_TicksPerSecond / 100;

        const long k_TicksPerSecond = 10000000;

        public virtual bool UpdateTask(string taskTitle)
        {
            m_CurrentTask++;
            m_CurrentTaskTitle = taskTitle;
            m_TimeStamp = 0;
            return !EditorUtility.DisplayCancelableProgressBar(m_CurrentTaskTitle, "", Progress);
        }

        public virtual bool UpdateInfo(string taskInfo)
        {
            var currentTicks = DateTime.Now.Ticks;
            if (currentTicks - m_TimeStamp < m_UpdateFrequency)
                return true;

            m_TimeStamp = currentTicks;
            return !EditorUtility.DisplayCancelableProgressBar(m_CurrentTaskTitle, taskInfo, Progress);
        }

        public virtual void Dispose()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
