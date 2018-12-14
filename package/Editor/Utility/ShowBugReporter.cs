
using UnityEditor;

namespace Unity.Tiny
{
    internal static class ShowBugReporter
    {
        [MenuItem(TinyConstants.MenuItemNames.BugReportWindow, false, 10000)]
        private static void LaunchBugReporter()
        {
            EditorUtilityBridge.LaunchBugReporter();
        }
    }
}
