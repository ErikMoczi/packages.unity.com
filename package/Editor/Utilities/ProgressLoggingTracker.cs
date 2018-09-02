using System;
using UnityEditor.Build.Interfaces;

namespace UnityEditor.Build.Utilities
{
    public class ProgressLoggingTracker : IProgressTracker, IDisposable
    {
        public int TaskCount { get; set; }

        public float Progress { get { return m_CurrentTask / (float)TaskCount; } }

        protected int m_CurrentTask = 0;

        protected string m_CurrentTaskTitle = "";

        public ProgressLoggingTracker()
        {
            BuildLogger.Log(string.Format("[{0}] Progress Tracker Started.", DateTime.Now));
        }

        public bool UpdateTask(string taskTitle)
        {
            m_CurrentTask++;
            m_CurrentTaskTitle = taskTitle;
            BuildLogger.Log(string.Format("[{0}] {1:P2} Running Task: '{2}'", DateTime.Now, Progress, m_CurrentTaskTitle));
            return !EditorUtility.DisplayCancelableProgressBar(m_CurrentTaskTitle, "", Progress);
        }

        public bool UpdateInfo(string taskInfo)
        {
            BuildLogger.Log(string.Format("[{0}] {1:P2} Running Task: '{2}' Information: '{3}'", DateTime.Now, Progress, m_CurrentTaskTitle, taskInfo));
            return !EditorUtility.DisplayCancelableProgressBar(m_CurrentTaskTitle, taskInfo, Progress);
        }

        public void Dispose()
        {
            BuildLogger.Log(string.Format("[{0}] Progress Tracker Completed.", DateTime.Now));
            EditorUtility.ClearProgressBar();
        }
    }
}
