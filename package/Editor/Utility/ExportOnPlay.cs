

using UnityEditor;

namespace Unity.Tiny
{
    internal static class ExportOnPlay
    {
        [TinyInitializeOnLoad]
        public static void Init()
        {
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged += HandlePlayStateChanged;
#else
            EditorApplication.playmodeStateChanged += HandlePlayStateChanged;
#endif
        }

#if UNITY_2017_2_OR_NEWER
        private static void HandlePlayStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                Export();
            }
        }
#else
        private static void HandlePlayStateChanged()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                Export();
            }
        }
#endif

        private static void Export()
        {
            var project = TinyEditorApplication.Project;
            if (null != project)
            {
                EditorApplication.isPlaying = false;
                TinyBuildPipeline.BuildAndLaunch();
            }
        }
    }
}

