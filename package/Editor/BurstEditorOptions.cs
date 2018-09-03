using UnityEditor;

namespace Unity.Burst.Editor
{
    internal static class BurstEditorOptions
    {
        private const string EnableSafetyChecksName = "BurstSafetyChecks";
        private const string EnableBurstCompilationText = "BurstCompilation";

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
    }
}