using Unity.Burst.LowLevel;
using UnityEditor;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Burst.Editor
{
    /// <summary>
    /// Register all menu entries for burst to the Editor
    /// </summary>
    internal static class BurstMenu
    {
        private const string UseBurstText = "Jobs/Use Burst Jobs";
        private const string EnableSafetyChecksText = "Jobs/Enable Burst Safety Checks";
        private const string BurstInspectorText = "Jobs/Burst Inspector";
        private const string EnableBurstCompilationText = "Jobs/Enable Burst Compilation";
        private const string ShowBurstTimingsText = "Jobs/Show Burst Timings";

        [MenuItem(UseBurstText, false)]
        private static void UseBurst()
        {
            JobsUtility.JobCompilerEnabled = !JobsUtility.JobCompilerEnabled;
        }

        [MenuItem(UseBurstText, true)]
        static bool UseBurstValidate()
        {
            Menu.SetChecked(UseBurstText, BurstCompilerService.IsInitialized && JobsUtility.JobCompilerEnabled);
            return BurstCompilerService.IsInitialized && BurstEditorOptions.EnableBurstCompilation;
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem(BurstInspectorText)]
        private static void BurstInspector()
        {
            // Get existing open window or if none, make a new one:
            BurstInspectorGUI window = EditorWindow.GetWindow<BurstInspectorGUI>("Burst Inspector");
            window.Show();
        }

        [MenuItem(EnableSafetyChecksText, false)]
        private static void EnableBurstSafetyChecks()
        {
            BurstEditorOptions.EnableBurstSafetyChecks = !BurstEditorOptions.EnableBurstSafetyChecks;
        }

        [MenuItem(EnableSafetyChecksText, true)]
        private static bool EnableBurstSafetyChecksValidate()
        {
            Menu.SetChecked(EnableSafetyChecksText, BurstEditorOptions.EnableBurstSafetyChecks);
            return BurstCompilerService.IsInitialized;
        }

        [MenuItem(EnableBurstCompilationText, false)]
        private static void EnableBurstCompilation()
        {
            BurstEditorOptions.EnableBurstCompilation = !BurstEditorOptions.EnableBurstCompilation;
        }

        [MenuItem(EnableBurstCompilationText, true)]
        private static bool EnableBurstCompilationValidate()
        {
            Menu.SetChecked(EnableBurstCompilationText, BurstEditorOptions.EnableBurstCompilation);
            return BurstCompilerService.IsInitialized;
        }

        [MenuItem(ShowBurstTimingsText, false)]
        private static void ShowBurstTimings()
        {
            BurstEditorOptions.EnableShowBurstTimings = !BurstEditorOptions.EnableShowBurstTimings;
        }

        [MenuItem(ShowBurstTimingsText, true)]
        private static bool ShowBurstTimingsValidate()
        {
            Menu.SetChecked(ShowBurstTimingsText, BurstEditorOptions.EnableShowBurstTimings);
            return BurstCompilerService.IsInitialized;
        }
    }
}
