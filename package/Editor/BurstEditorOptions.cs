using UnityEditor;

namespace Unity.Burst.Editor
{
    internal static class BurstEditorOptions
    {
        private const string EnableSafetyChecksName = "BurstSafetyChecks";
        private const string EnableBurstCompilationText = "BurstCompilation";
        private const string EnableBurstShowTimings = "BurstShowTimings";
        private const string EnableBurstCompileSynchronouslyText = "BurstCompileSynchronously";

        public static bool EnableBurstSafetyChecks
        {
            get { return EditorPrefs.GetBool(EnableSafetyChecksName, true); }
            set { EditorPrefs.SetBool(EnableSafetyChecksName, value); }
        }

        public static bool EnableBurstCompilation
        {
            get { return EditorPrefs.GetBool(EnableBurstCompilationText, true); }
            set { EditorPrefs.SetBool(EnableBurstCompilationText, value); }
        }
        public static bool EnableShowBurstTimings
        {
            get { return EditorPrefs.GetBool(EnableBurstShowTimings, false); }
            set { EditorPrefs.SetBool(EnableBurstShowTimings, value); }
        }

        public static bool EnableBurstCompileSynchronously
        {
            get { return EditorPrefs.GetBool(EnableBurstCompileSynchronouslyText, false); }
            set { EditorPrefs.SetBool(EnableBurstCompileSynchronouslyText, value); }
        }
    }
}