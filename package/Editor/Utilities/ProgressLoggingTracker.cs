using System;

namespace UnityEditor.Build.Pipeline.Utilities
{
    public class ProgressLoggingTracker : ProgressTracker
    {
        public ProgressLoggingTracker()
        {
            BuildLogger.Log(string.Format("[{0}] Progress Tracker Started.", DateTime.Now));
        }

        public override bool UpdateTask(string taskTitle)
        {
            BuildLogger.Log(string.Format("[{0}] {1:P2} Running Task: '{2}'", DateTime.Now, Progress, m_CurrentTaskTitle));
            return base.UpdateInfo(taskTitle);
        }

        public override bool UpdateInfo(string taskInfo)
        {
            BuildLogger.Log(string.Format("[{0}] {1:P2} Running Task: '{2}' Information: '{3}'", DateTime.Now, Progress, m_CurrentTaskTitle, taskInfo));
            return base.UpdateInfo(taskInfo);
        }

        public override void Dispose()
        {
            BuildLogger.Log(string.Format("[{0}] Progress Tracker Completed.", DateTime.Now));
            base.Dispose();
        }
    }
}
