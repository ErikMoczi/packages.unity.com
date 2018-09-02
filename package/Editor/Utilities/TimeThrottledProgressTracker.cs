using System;
using UnityEditor.Build.Interfaces;

namespace UnityEditor.Build.Utilities
{
    public class TimeThrottledProgressTracker : IProgressTracker, IDisposable
    {
        public int TaskCount { get; set; }

        public float Progress { get { return m_CurrentTask / (float)TaskCount; } }

        protected int m_CurrentTask = 0;
        private long m_minInfoInterval;
        protected string m_CurrentTaskTitle = "";
        public TimeThrottledProgressTracker(long minInterval)
        {
            m_minInfoInterval = minInterval;
        }

        public bool UpdateTask(string taskTitle)
        {
            m_CurrentTask++;
            m_CurrentTaskTitle = taskTitle;
            infoUpdateTimer.Start();
            lastInfoUpdate = -100000;
            return !EditorUtility.DisplayCancelableProgressBar(m_CurrentTaskTitle, "", Progress);
        }

        long lastInfoUpdate = 0;
        System.Diagnostics.Stopwatch infoUpdateTimer = new System.Diagnostics.Stopwatch();
        public bool UpdateInfo(string taskInfo)
        {
            var now = infoUpdateTimer.ElapsedMilliseconds;
            if ((now - lastInfoUpdate) < m_minInfoInterval)
                return true;
            lastInfoUpdate = now;
            return !EditorUtility.DisplayCancelableProgressBar(m_CurrentTaskTitle, taskInfo, Progress);
        }

        public void Dispose()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}