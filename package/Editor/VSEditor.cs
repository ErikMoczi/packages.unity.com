using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using UnityEditor;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Unity.CodeEditor;

namespace VisualStudioEditor
{
    internal enum VisualStudioVersion
    {
        Invalid = 0,
        VisualStudio2008 = 9,
        VisualStudio2010 = 10,
        VisualStudio2012 = 11,
        VisualStudio2013 = 12,
        VisualStudio2015 = 14,
        VisualStudio2017 = 15,
        VisualStudio2019 = 16,
    }

    [InitializeOnLoad]
    public class VSEditor : IExternalCodeEditor
    {
        internal class VisualStudioPath
        {
            public string Path { get; set; }
            public string Edition { get; set; }

            public VisualStudioPath(string path, string edition = "")
            {
                Path = path;
                Edition = edition;
            }
        }

        static readonly string k_ExpressNotSupportedMessage = L10n.Tr(
            "Unfortunately Visual Studio Express does not allow itself to be controlled by external applications. " +
            "You can still use it by manually opening the Visual Studio project file, but Unity cannot automatically open files for you when you doubleclick them. " +
            "\n(This does work with Visual Studio Pro)"
        );

        static IEnumerable<string> FindVisualStudioDevEnvPaths()
        {
            var asset = AssetDatabase.FindAssets("VSWhere a:packages").Select(AssetDatabase.GUIDToAssetPath).First(assetPath => assetPath.Contains("vswhere.exe"));
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(asset);
            var progpath = packageInfo.resolvedPath + asset.Substring("Packages/com.unity.ide.visualstudio".Length);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = progpath,
                    Arguments = "-prerelease -property productPath",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            process.Start();
            process.WaitForExit();

            while (!process.StandardOutput.EndOfStream)
            {
                yield return process.StandardOutput.ReadLine();
            }
        }

        static VSEditor()
        {
            try
            {
                InstalledVisualStudios = GetInstalledVisualStudios();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error detecting Visual Studio installations: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);
                InstalledVisualStudios = new Dictionary<VisualStudioVersion, string[]>();
            }
            var editor = new VSEditor(new Discovery(), new ProjectGeneration());
            CodeEditor.Register(editor);
            var current = CodeEditor.CurrentEditorInstallation;
            foreach (string[] paths in InstalledVisualStudios.Values)
            {
                if (paths.Contains(current))
                {
                    editor.CreateIfDoesntExist();
                    return;
                }
            }
        }

        IDiscovery m_Discoverability;
        IGenerator m_Generation;
        CodeEditor.Installation m_Installation;
        VSInitializer m_Initiliazer = new VSInitializer();
        bool m_ExternalEditorSupportsUnityProj;

        public VSEditor(IDiscovery discovery, IGenerator projectGeneration)
        {
            m_Discoverability = discovery;
            m_Generation = projectGeneration;
        }

        internal static Dictionary<VisualStudioVersion, string[]> InstalledVisualStudios { get; private set; }

        static bool IsOSX => Environment.OSVersion.Platform == PlatformID.Unix;
        static bool IsWindows => !IsOSX && Path.DirectorySeparatorChar == '\\' && Environment.NewLine == "\r\n";
        static readonly GUIContent k_AddUnityProjeToSln = EditorGUIUtility.TrTextContent("Add .unityproj's to .sln");

        static string GetRegistryValue(string path, string key)
        {
            try
            {
                return Registry.GetValue(path, key, null) as string;
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Derives the Visual Studio installation path from the debugger path
        /// </summary>
        /// <returns>
        /// The Visual Studio installation path (to devenv.exe)
        /// </returns>
        /// <param name='debuggerPath'>
        /// The debugger path from the windows registry
        /// </param>
        static string DeriveVisualStudioPath(string debuggerPath)
        {
            string startSentinel = DeriveProgramFilesSentinel();
            string endSentinel = "Common7";
            bool started = false;
            string[] tokens = debuggerPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            string path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            // Walk directories in debugger path, chop out "Program Files\INSTALLATION\PATH\HERE\Common7"
            foreach (var token in tokens)
            {
                if (!started && string.Equals(startSentinel, token, StringComparison.OrdinalIgnoreCase))
                {
                    started = true;
                    continue;
                }
                if (started)
                {
                    path = Path.Combine(path, token);
                    if (string.Equals(endSentinel, token, StringComparison.OrdinalIgnoreCase))
                        break;
                }
            }

            return Path.Combine(path, "IDE", "devenv.exe");
        }

        /// <summary>
        /// Derives the program files sentinel for grabbing the VS installation path.
        /// </summary>
        /// <remarks>
        /// From a path like 'c:\Archivos de programa (x86)', returns 'Archivos de programa'
        /// </remarks>
        static string DeriveProgramFilesSentinel()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .LastOrDefault();

            if (!string.IsNullOrEmpty(path))
            {
                // This needs to be the "real" Program Files regardless of 64bitness
                int index = path.LastIndexOf("(x86)");
                if (0 <= index)
                    path = path.Remove(index);
                return path.TrimEnd();
            }

            return "Program Files";
        }

        public CodeEditor.Installation[] Installations => m_Discoverability.PathCallback();

        static void ParseRawDevEnvPaths(string[] rawDevEnvPaths, Dictionary<VisualStudioVersion, string[]> versions)
        {
            if (rawDevEnvPaths == null)
            {
                return;
            }

            var v2017 = rawDevEnvPaths.Where(path => path.Contains("2017")).ToArray();
            var v2019 = rawDevEnvPaths.Where(path => path.Contains("2019")).ToArray();

            versions[VisualStudioVersion.VisualStudio2017] = v2017;
            versions[VisualStudioVersion.VisualStudio2019] = v2019;
        }

        /// <summary>
        /// Detects Visual Studio installations using the Windows registry
        /// </summary>
        /// <returns>
        /// The detected Visual Studio installations
        /// </returns>
        internal static Dictionary<VisualStudioVersion, string[]> GetInstalledVisualStudios()
        {
            var versions = new Dictionary<VisualStudioVersion, string[]>();

            if (IsWindows)
            {
                foreach (VisualStudioVersion version in Enum.GetValues(typeof(VisualStudioVersion)))
                {
                    if (version > VisualStudioVersion.VisualStudio2015)
                        continue;

                    try
                    {
                        // Try COMNTOOLS environment variable first
                        FindLegacyVisualStudio(version, versions);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }

                var raw = FindVisualStudioDevEnvPaths();

                ParseRawDevEnvPaths(raw.ToArray(), versions);
            }

            return versions;
        }

        static void FindLegacyVisualStudio(VisualStudioVersion version, Dictionary<VisualStudioVersion, string[]> versions)
        {
            string key = Environment.GetEnvironmentVariable($"VS{(int)version}0COMNTOOLS");
            if (!string.IsNullOrEmpty(key))
            {
                string path = Path.Combine(key, "..", "IDE", "devenv.exe");
                if (File.Exists(path))
                {
                    versions[version] = new[] { path };
                    return;
                }
            }

            // Try the proper registry key
            key = GetRegistryValue(
                $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\{(int)version}.0", "InstallDir");

            // Try to fallback to the 32bits hive
            if (string.IsNullOrEmpty(key))
                key = GetRegistryValue(
                    $@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\{(int)version}.0", "InstallDir");

            if (!string.IsNullOrEmpty(key))
            {
                string path = Path.Combine(key, "devenv.exe");
                if (File.Exists(path))
                {
                    versions[version] = new[] { path };
                    return;
                }
            }

            // Fallback to debugger key
            key = GetRegistryValue(
                // VS uses this key for the local debugger path
                $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\{(int)version}.0\Debugger", "FEQARuntimeImplDll");
            if (!string.IsNullOrEmpty(key))
            {
                string path = DeriveVisualStudioPath(key);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    versions[version] = new[] { DeriveVisualStudioPath(key) };
            }
        }

        public void CreateIfDoesntExist()
        {
            if (!m_Generation.HasSolutionBeenGenerated())
            {
                m_Generation.Sync();
            }
        }

        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            var lowerCasePath = editorPath.ToLower();
            if (lowerCasePath.EndsWith("vcsexpress.exe"))
            {
                installation = new CodeEditor.Installation
                {
                    Name = "VSExpress",
                    Path = editorPath
                };
                m_Installation = installation;
                return true;
            }

            if (lowerCasePath.EndsWith("devenv.exe"))
            {
                installation = new CodeEditor.Installation
                {
                    Name = "VisualStudio",
                    Path = editorPath
                };
                m_Installation = installation;
                return true;
            }
            var filename = Path.GetFileName(lowerCasePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)).Replace(" ", "");

            if (filename == "visualstudio.app" || lowerCasePath.Contains("monodevelop") || lowerCasePath.Contains("xamarinstudio") || lowerCasePath.Contains("xamarin studio"))
            {
                installation = new CodeEditor.Installation
                {
                    Name = "MonoDevelop",
                    Path = editorPath
                };
                m_Installation = installation;
                return true;
            }

            installation = default;
            m_Installation = installation;
            return false;
        }

        public void OnGUI()
        {
            if (m_Installation.Name.Equals("VSExpress"))
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label("", "CN EntryWarn");
                GUILayout.Label(k_ExpressNotSupportedMessage, "WordWrappedLabel");
                GUILayout.EndHorizontal();
            }

            if (m_Installation.Name.Equals("MonoDevelop"))
            {
                m_ExternalEditorSupportsUnityProj = EditorGUILayout.Toggle(
                    k_AddUnityProjeToSln,
                    m_ExternalEditorSupportsUnityProj);
            }
        }

        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            m_Generation.SyncIfNeeded(addedFiles.Union(deletedFiles).Union(movedFiles).Union(movedFromFiles), importedFiles);
        }

        public void SyncAll()
        {
            m_Generation.Sync();
        }

        public void Initialize(string editorInstallationPath)
        {
            m_Initiliazer.Initialize(editorInstallationPath, InstalledVisualStudios);
        }

        public bool OpenProject(string path, int line, int column)
        {
            var comAssetPath = AssetDatabase.FindAssets("COMIntegration a:packages").Select(AssetDatabase.GUIDToAssetPath).First(assetPath => assetPath.Contains("COMIntegration.exe"));
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(comAssetPath);
            var progpath = packageInfo.resolvedPath + comAssetPath.Substring("Packages/com.unity.ide.visualstudio".Length);
            var solution = GetSolutionFile(path); // TODO: If solution file doesn't exist resync.
            solution = solution == "" ? "" : $"\"{solution}\"";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = progpath,
                    Arguments = $"\"{CodeEditor.CurrentEditorInstallation}\" \"{path}\" {solution} {line}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };
            var result = process.Start();

            while (!process.StandardOutput.EndOfStream)
            {
                if (process.StandardOutput.ReadLine() == "displayProgressBar")
                {
                    EditorUtility.DisplayProgressBar("Opening Visual Studio", "Starting up Visual Studio, this might take some time.", .5f);
                }

                if (process.StandardOutput.ReadLine() == "clearprogressbar")
                {
                    EditorUtility.ClearProgressBar();
                }
            }
            var errorOutput = process.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(errorOutput))
            {
                UnityEngine.Debug.Log("Error: \n" + errorOutput);
            }
            
            process.WaitForExit();
            return result;
        }

        private string GetSolutionFile(string path)
        {
            if (UnityEditor.Unsupported.IsDeveloperBuild())
            {
                var baseFolder = GetBaseUnityDeveloperFolder();
                var lowerPath = path.ToLowerInvariant();
                var isUnitySourceCode = false;

                if (lowerPath.Contains((baseFolder + "/Runtime").ToLowerInvariant()))
                {
                    isUnitySourceCode = true;
                }
                if (lowerPath.Contains((baseFolder + "/Editor").ToLowerInvariant()))
                {
                    isUnitySourceCode = true;
                }

                if (isUnitySourceCode)
                {
                    return Path.Combine(baseFolder, "Projects/CSharp/Unity.CSharpProjects.gen.sln");
                }
            }
            var solutionFile = m_Generation.SolutionFile();
            if (File.Exists(solutionFile))
            {
                return solutionFile;
            }
            return "";
        }

        private string GetBaseUnityDeveloperFolder()
        {
            return Directory.GetParent(EditorApplication.applicationPath).Parent.Parent.FullName;
        }
    }
}
