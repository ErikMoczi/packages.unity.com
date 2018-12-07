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
    public static class TutorialExporter
    {
        const string k_FrameworkPath = "Assets/Framework/Interactive Tutorials";
        const string k_FrameworkInternalPath = "Assets/Framework/Interactive Tutorials Internal";
        const string k_AssemblyName = "UnityEditor.InteractiveTutorialsFramework.dll";

        internal static string assemblyName { get { return k_AssemblyName; } }

        // Paths absolute
        static IEnumerable<string> GetAssemblyReferences()
        {
            var references = new[]
            {
                "Managed/UnityEngine.dll",
                "Managed/UnityEditor.dll",
                "Managed/nunit.framework.dll",
                "UnityExtensions/Unity/TestRunner/UnityEngine.TestRunner.dll",
            };
            return references.Select(path => Path.GetFullPath(EditorApplication.applicationContentsPath + "/" + path));
        }

        // Paths relative to project folder
        static IEnumerable<string> GetScriptsUsingInternals()
        {
            var scriptsUsingInternals = new List<string>();

            var internalProxyDirectory = k_FrameworkPath + "/Editor/Internal Proxy";
            foreach (var path in Directory.GetFiles(internalProxyDirectory, "*.cs", SearchOption.AllDirectories))
            {
                scriptsUsingInternals.Add(path.Replace("\\", "/"));
            }

            var frameworkRelativePaths = new[]
            {
                "Editor/Masking/GUIControlSelector.cs",
                "Editor/Masking/MaskingManager.cs",
                "Editor/Masking/UnmaskedView.cs",
                "Editor/ProjectMode.cs", // Not using internals but referenced from this assembly
                "Editor/Property Attributes/SerializedTypeFilterAttribute.cs",
                "Editor/SerializedType.cs",
            };
            scriptsUsingInternals.AddRange(frameworkRelativePaths.Select(path => k_FrameworkPath + "/" + path));

            return scriptsUsingInternals;
        }

        internal static bool ExportPackageForTutorial(string packagePath, Tutorial tutorial, IEnumerable<string> additionalAssets)
        {
            EditorApplication.LockReloadAssemblies();
            AssetDatabase.StartAssetEditing();

            var assemblyPath = k_FrameworkPath + "/" + k_AssemblyName;
            if (!CompileAssemblyWithScriptsUsingInternals(assemblyPath))
            {
                AssetDatabase.StopAssetEditing();
                EditorApplication.UnlockReloadAssemblies();

                return false;
            }

            File.Delete(assemblyPath + ".mdb");

            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();

            var packageAssets = new HashSet<string>();

            // Include tutorial asset and its dependencies
            var tutorialAssetPath = AssetDatabase.GetAssetPath(tutorial);
            var tutorialDependencies = AssetDatabase.GetDependencies(tutorialAssetPath);
            packageAssets.Add(tutorialAssetPath);
            packageAssets.UnionWith(tutorialDependencies);

            // Include assembly with framework scripts that requires access to internals
            packageAssets.Add(assemblyPath);

            // Add all assets from framework (scripts using internals are excluded later)
            foreach (var path in Directory.GetFiles(k_FrameworkPath, "*", SearchOption.AllDirectories))
                packageAssets.Add(path.Replace(@"\", "/"));

            // Include any additional assets
            packageAssets.UnionWith(additionalAssets);

            // Exclude scripts already in assembly
            packageAssets.ExceptWith(GetScriptsUsingInternals());

            // Exclude assets internal to the framework
            packageAssets.RemoveWhere(path => path.StartsWith(k_FrameworkInternalPath + "/"));

            AssetDatabase.ExportPackage(packageAssets.ToArray(), packagePath, ExportPackageOptions.Default);
            AssetDatabase.DeleteAsset(assemblyPath);

            EditorApplication.UnlockReloadAssemblies();

            return true;
        }

        internal static bool CompileAssemblyWithScriptsUsingInternals(string assemblyPath)
        {
            string stdout;
            string stderr;
            if (!CompileAssembly(Path.GetFullPath(assemblyPath), GetScriptsUsingInternals().Select(Path.GetFullPath), GetAssemblyReferences(), out stdout, out stderr))
            {
                Debug.LogError(stdout);
                Debug.LogError(stderr);

                return false;
            }

            return true;
        }

        static bool CompileAssembly(string assemblyOutputPath, IEnumerable<string> sourceFilePaths, IEnumerable<string> references, out string stdout, out string stderr)
        {
            var monoExecutablePath = Path.GetFullPath(EditorApplication.applicationContentsPath + "/MonoBleedingEdge/bin/mono");
            if (Application.platform == RuntimePlatform.WindowsEditor)
                monoExecutablePath = monoExecutablePath + ".exe";

            var mcsExecutablePath = Path.GetFullPath(EditorApplication.applicationContentsPath + "/MonoBleedingEdge/lib/mono/4.5/mcs.exe");

            var arguments = new StringBuilder();
            arguments.AppendFormat("\"{0}\" ", mcsExecutablePath);
            arguments.Append("-debug ");
            arguments.Append("-target:library ");
            arguments.Append("-nowarn:0169 ");
            arguments.Append("-langversion:4 ");
            arguments.AppendFormat("-out:\"{0}\" ", assemblyOutputPath);
            arguments.Append("-unsafe ");
            arguments.Append("-sdk:2.0 ");
            foreach (var reference in references)
                arguments.AppendFormat("-r:\"{0}\" ", reference);
            foreach (var sourceFilePath in sourceFilePaths)
                arguments.AppendFormat("\"{0}\" ", sourceFilePath);

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = Path.GetFullPath(Application.dataPath + "/.."),
                    FileName = monoExecutablePath,
                    Arguments = arguments.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            stdout = process.StandardOutput.ReadToEnd();
            stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
    }
}
