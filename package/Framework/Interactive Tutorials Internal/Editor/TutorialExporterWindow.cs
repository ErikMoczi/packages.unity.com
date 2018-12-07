using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.InteractiveTutorials;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Unity.InteractiveTutorials
{
    public class TutorialExporterWindow : EditorWindow
    {
        [SerializeField]
        Tutorial m_Tutorial;

        [SerializeField]
        List<string> m_AdditionalDirectories = new List<string>();

        [SerializeField]
        bool m_IncludeAllScripts = true;

        [SerializeField]
        bool m_IncludeAllShaders = true;

        [SerializeField]
        bool m_AutoOpen = true;

        [MenuItem("Tutorials/Export Tutorial...")]
        public static void OpenWindow()
        {
            var window = GetWindow<TutorialExporterWindow>();
            window.Show();
        }

        [MenuItem("Tutorials/Export all with default settings")]
        public static void ExportAll()
        {
            var guids = AssetDatabase.FindAssets("t:Tutorial", null);
            var projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
            var path = EditorUtility.SaveFolderPanel("Export tutorials", projectPath, "tutorials");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var instanceID = AssetDatabase.GetMainAssetInstanceID(assetPath);
                var tutorial = (Tutorial)EditorUtility.InstanceIDToObject(instanceID);
                var packagePath = Path.Combine(path, tutorial.name + ".unitypackage");
                ExportTutorial(tutorial, packagePath, true, true, new List<string>());
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Tutorial:", EditorStyles.boldLabel);
            m_Tutorial = (Tutorial)EditorGUILayout.ObjectField(m_Tutorial, typeof(Tutorial), false);

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            m_IncludeAllScripts = EditorGUILayout.ToggleLeft("Include all scripts", m_IncludeAllScripts);
            m_IncludeAllShaders = EditorGUILayout.ToggleLeft("Include all shaders", m_IncludeAllShaders);

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Additional folders to include:", EditorStyles.boldLabel);
            if (GUILayout.Button("add", GUILayout.Width(80)))
            {
                var path = EditorUtility.OpenFolderPanel("Select additional folders to include", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    path = Path.GetFullPath(path);
                    m_AdditionalDirectories.Add(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            foreach (var path in m_AdditionalDirectories)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(MakePathRelative(path));
                if (GUILayout.Button("remove", GUILayout.Width(80)))
                {
                    m_AdditionalDirectories.Remove(path);
                    GUIUtility.ExitGUI();
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            m_AutoOpen = EditorGUILayout.ToggleLeft("Open project with exported package when done", m_AutoOpen);

            using (var disabledScope = new EditorGUI.DisabledScope(m_Tutorial == null))
            {
                if (GUILayout.Button(m_Tutorial == null ? "Export package (No tutorial selected)" : "Export package"))
                {
                    ExportTutorial();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            var forceDisableMask = EditorPrefs.GetBool("Unity.InteractiveTutorials.forceDisableMask", false);
            EditorGUI.BeginChangeCheck();
            forceDisableMask = EditorGUILayout.ToggleLeft("Force Disable Masking in this machine", forceDisableMask);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("Unity.InteractiveTutorials.forceDisableMask", forceDisableMask);
            }
        }

        void ExportTutorial()
        {
            var projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
            var packagePath = EditorUtility.SaveFilePanel("Export tutorial", projectPath, m_Tutorial.name, "unitypackage");
            if (string.IsNullOrEmpty(packagePath))
                return;

            if (!ExportTutorial(m_Tutorial, packagePath, m_IncludeAllScripts, m_IncludeAllShaders, m_AdditionalDirectories))
                return;

            if (m_AutoOpen)
                OpenTemporaryProject(packagePath);
        }

        static bool ExportTutorial(Tutorial m_Tutorial, string exportDestination, bool includeAllScripts, bool includeAllShaders, List<string> additionalDirectories)
        {
            if (string.IsNullOrEmpty(exportDestination))
                return false;
            ;
            // Paths are relative to project directory and must use forward slashes as directory separator
            var additionalAssets = new HashSet<string>();
            // Include all scripts if desired
            // Scripts can reference types in other scripts and this won't be picked up as a dependency automatically
            if (includeAllScripts)
            {
                var scriptAssets = AssetDatabase.FindAssets("t:script").Select(AssetDatabase.GUIDToAssetPath);
                additionalAssets.UnionWith(scriptAssets);
            }
            // Include all shaders if desired
            // Shaders are referenced by name and won't be picked up as a dependency automatically
            if (includeAllShaders)
            {
                var shaderAssets = AssetDatabase.FindAssets("t:shader").Select(AssetDatabase.GUIDToAssetPath);
                var cgProgramAssets = AssetDatabase.FindAssets("t:cgprogram").Select(AssetDatabase.GUIDToAssetPath);
                additionalAssets.UnionWith(shaderAssets);
                additionalAssets.UnionWith(cgProgramAssets);
            }
            foreach (var directory in additionalDirectories)
            {
                foreach (var absolutePath in Directory.GetFiles(Path.GetFullPath(directory), "*", SearchOption.AllDirectories))
                    additionalAssets.Add(MakePathRelative(absolutePath));
            }
            if (TutorialExporter.ExportPackageForTutorial(exportDestination, m_Tutorial, additionalAssets))
            {
                Debug.Log(string.Format("Package exported to: {0}", exportDestination));
                return true;
            }

            return false;
        }

        static void OpenTemporaryProject(string packagePath)
        {
            var executablePath = EditorApplication.applicationPath;
            if (Application.platform == RuntimePlatform.OSXEditor)
                executablePath += "/Contents/MacOS/Unity";

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Path.GetFullPath(executablePath),
                    Arguments = string.Format("-temporary -createProject \"{0}\"", packagePath)
                }
            };

            process.Start();
        }

        static string MakePathRelative(string path)
        {
            var fullPath = Path.GetFullPath(path); // Normalize directory separators
            var projectPath = Path.GetFullPath(Application.dataPath);
            if (!fullPath.StartsWith(projectPath))
                return path;
            return path.Substring(projectPath.Length - "Assets".Length).Replace(@"\", "/");
        }
    }
}
