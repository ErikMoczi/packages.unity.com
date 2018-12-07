using UnityEditor;

namespace Unity.Tiny
{
    internal static class SetupProjectSettings
    {
        [InitializeOnLoadMethod]
        public static void Setup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying)
                {
                    return;
                }

#if !UNITY_2018_3_OR_NEWER
				EditorUtility.DisplayDialog(
					"2018.3 required",
					$"The minimal version required by the Tiny Editor is 2018.3",
					"OK");
#endif

                if (PlayerSettings.scriptingRuntimeVersion != ScriptingRuntimeVersion.Latest)
                {
                    if (EditorUtility.DisplayDialog(
                        "Reimport required",
                        "Tiny Editor requires .NET 4.x or equivalent. Do you want to change this Player setting and re-import the current project?",
                        "Yes", "No"))
                    {
                        // change runtime version
                        PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;

                        // purge
                        EditorApplication.ExecuteMenuItem("Assets/Reimport All");
                    }
                }

                if (EditorSettings.spritePackerMode != SpritePackerMode.BuildTimeOnlyAtlas && EditorSettings.spritePackerMode != SpritePackerMode.AlwaysOnAtlas)
                {
                    if (EditorUtility.DisplayDialog(
                        "Non-legacy sprite atlas packing mode required",
                        "Tiny Editor requires non-legacy sprite atlas packing mode. Do you want to change this Editor setting?",
                        "Yes", "No"))
                    {
                        // change sprite packing mode to "Enabled for Builds"
                        EditorSettings.spritePackerMode = SpritePackerMode.BuildTimeOnlyAtlas;
                    }
                }
            };
        }
    }
}
