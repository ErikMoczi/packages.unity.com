

using UnityEditor;

namespace Unity.Tiny
{
    internal static class TinyEditorPrefs
    {
        private const string k_LastWorkspaceKey = "Tiny.Editor.Workspace.LastWorkspace";
        
        private static string GetWorkspaceKey(string persistenceId)
        {
            const string kWorkspaceKey = "Tiny.Editor.Workspace.{0}";
            return string.Format(kWorkspaceKey, persistenceId);
        }

        /// <summary>
        /// Sets the workspace
        /// </summary>
        /// <param name="workspace">Workspace to save</param>
        /// <param name="persistenceId">The persistenceId for this workspace</param>
        public static void SaveWorkspace(TinyEditorWorkspace workspace, string persistenceId = null)
        {
            if (string.IsNullOrEmpty(persistenceId))
            {
                persistenceId = "Temp";
            }
            
            EditorPrefs.SetString(GetWorkspaceKey(persistenceId), workspace.ToJson());
            EditorPrefs.SetString(k_LastWorkspaceKey, persistenceId);
        }

        /// <summary>
        /// Loads the workspace for the given id
        /// </summary>
        /// <returns>Workspace for the given Id or an empty workspace</returns>
        public static TinyEditorWorkspace LoadWorkspace(string persistenceId)
        {
            var workspace = new TinyEditorWorkspace();
            var json = EditorPrefs.GetString($"{GetWorkspaceKey(persistenceId)}", string.Empty);
            workspace.FromJson(json);
            return workspace;
        }

        /// <summary>
        /// Loads the last saved workspace
        /// </summary>
        public static TinyEditorWorkspace LoadLastWorkspace()
        {
            var workspace = new TinyEditorWorkspace();
            var persistenceId = EditorPrefs.GetString(k_LastWorkspaceKey, string.Empty);
            var json = EditorPrefs.GetString(GetWorkspaceKey(persistenceId), string.Empty);
            workspace.FromJson(json);
            return workspace;
        }
    }
}

