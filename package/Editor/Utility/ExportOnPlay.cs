using UnityEditor;

namespace Unity.Tiny
{
    internal static class ExportOnPlay
    {
        private static bool m_EnteredPlayMode;

        [InitializeOnLoadMethod]
        public static void Init()
        {
            EditorApplication.playModeStateChanged += HandlePlayStateChanged;
        }

        private static void HandlePlayStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredEditMode:
                    if (m_EnteredPlayMode)
                    {
                        m_EnteredPlayMode = false;
                        EditorApplication.delayCall += () => TinyEditorApplication.LoadTemp(ContextUsage.Edit);
                    }
                    break;

                case PlayModeStateChange.ExitingEditMode:
                    if (TinyEditorApplication.Project != null)
                    {
                        TinyBuildPipeline.BuildAndLaunch();
                        if (!PreferencesMenuItems.PlayModeEnabled ||
                            TinyBuildPipeline.WorkspaceBuildOptions.Configuration == TinyBuildConfiguration.Release)
                        {
                            EditorApplication.isPlaying = false;
                        }
                    }
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    if (!m_EnteredPlayMode)
                    {
                        m_EnteredPlayMode = true;
                        EditorApplication.delayCall += () => TinyEditorApplication.LoadTemp(ContextUsage.LiveLink);
                    }
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    WebSocketServer.Instance?.SendResume();
                    if (m_EnteredPlayMode)
                    {
                        TinyEditorApplication.Close(false);
                    }
                    break;

                default:
                    break;
            }
        }
    }
}
