using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal static class TinyRuntimeInstaller
    {
        private const string RuntimeVariantFull = "RuntimeFull";
        private const string RuntimeVariantStripped = "RuntimeStripped";
        private const string PublicDistFolder = "Tiny/Dist/artifacts/Stevedore/tiny-dist";
        private const string PublicSamplesPackage = "Tiny/Dist/artifacts/Stevedore/tiny-samples/tiny-samples.unitypackage";

        static TinyRuntimeInstaller()
        {
            LazyInstall();
        }

        private static void LazyInstall()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
#if UNITY_TINY_INTERNAL
                // need to delay this one until the MenuItem is created by the application
                EditorApplication.delayCall += () =>
                {
                    var enabled = AutoBuildEnabled;
                    Menu.SetChecked(k_ToggleAutoBuildMenuItem, enabled);
                    if (enabled)
                    {
                        BuildDevRuntimeHtml5DevOnly();
                    }
                };
#else
                Install(force: false, silent: true);
#endif
            }
        }

        private static string PrependMono(string program)
        {
#if UNITY_EDITOR_WIN
            return program.Replace('/', '\\');
#else
            return "mono " + program;
#endif
        }

        /// <summary>
        /// Utility class to use whenever tools binaries need to be overwritten.
        /// Upon new scope, running tools instances will be killed.
        /// Upon scope dispose, necessary tools will be restarted.
        /// </summary>
        private class OverwriteToolsScope : IDisposable
        {
            private bool m_WSServerWasListening;
            private bool m_HTTPServerWasListening;

            public OverwriteToolsScope()
            {
                // Gracefully close WebSocketServer
                if (WebSocketServer.Instance != null && WebSocketServer.Instance.Listening)
                {
                    m_WSServerWasListening = true;
                    WebSocketServer.Instance.Close();
                }

                // Gracefully close HTTPServer
                if (HTTPServer.Instance != null && HTTPServer.Instance.Listening)
                {
                    m_HTTPServerWasListening = true;
                    HTTPServer.Instance.Close();
                }

                // Kill remaining tools
                var processes = Process.GetProcessesByName(TinyShell.ToolsManagerNativeName());
                foreach (var process in processes)
                {
                    process.Kill();
                }
            }

            public void Dispose()
            {
                // Restore WebSocketServer if it was listening
                if (WebSocketServer.Instance != null && m_WSServerWasListening)
                {
                    var project = TinyEditorApplication.Project;
                    if (project != null)
                    {
                        WebSocketServer.Instance.Listen(project.Settings.LocalWSServerPort);
                    }
                    else
                    {
                        WebSocketServer.Instance.Listen(TinyProjectSettings.DefaultLocalWSServerPort);
                    }
                }

                // Restore HTTPServer if it was listening
                if (HTTPServer.Instance != null && m_HTTPServerWasListening)
                {
                    var project = TinyEditorApplication.Project;
                    if (project != null)
                    {
                        HTTPServer.Instance.Listen(project.Settings.LocalHTTPServerPort);
                    }
                    else
                    {
                        HTTPServer.Instance.Listen(TinyProjectSettings.DefaultLocalHTTPServerPort);
                    }
                }
            }
        }

#if UNITY_TINY_INTERNAL

        private const string k_AutoBuildPrefKey = "TINY_INTERNAL_AUTO_BUILD_RUNTIME";

        private static bool AutoBuildEnabled
        {
            get => EditorPrefs.GetBool(k_AutoBuildPrefKey, true);
            set => EditorPrefs.SetBool(k_AutoBuildPrefKey, value);
        }

        [Flags]
        private enum BuildRuntimeFlags
        {
            None = 0,

            Generic = 1,

            Docs = 2, // DEPRECATED - Runtime API docs are now derived from the distributed package

            Html5Debug = 4,
            Html5Development = 8,
            Html5Release = 16,
            Html5 = Html5Debug | Html5Development | Html5Release,

            Clean = 32,

            Distribution = Clean | Html5
        }

        private const string k_ToggleAutoBuildMenuItem = "Tiny/INTERNAL/Build Runtime/Build On Domain Reload";

        [MenuItem(k_ToggleAutoBuildMenuItem)]
        internal static void ToggleBuildOnDomainReload()
        {
            var enabled = !AutoBuildEnabled;
            AutoBuildEnabled = enabled;
            Menu.SetChecked(k_ToggleAutoBuildMenuItem, enabled);
        }

        [MenuItem("Tiny/INTERNAL/Build Runtime/HTML5 Incremental")]
        internal static void BuildDevRuntimeHtml5()
        {
            // used by CI - do not rename this method
            BuildRuntime(BuildRuntimeFlags.Html5);
        }

        [MenuItem("Tiny/INTERNAL/Build Runtime/HTML5 Incremental - Development Only")]
        internal static void BuildDevRuntimeHtml5DevOnly()
        {
            BuildRuntime(BuildRuntimeFlags.Html5Development);
        }

        [MenuItem("Tiny/INTERNAL/Build Runtime/For Distribution")]
        internal static void BuildRuntimeDistribution()
        {
            BuildRuntime(BuildRuntimeFlags.Distribution);
        }

        [MenuItem("Tiny/INTERNAL/Build Runtime/Package Only")]
        private static void BuildDevRuntimePackage()
        {
            BuildRuntime(BuildRuntimeFlags.Generic);
        }

        private static void BuildRuntime(BuildRuntimeFlags buildFlags)
        {
            // gather build targets
            var beeTargets = new List<string>();

            var isHtml5 = (buildFlags & BuildRuntimeFlags.Html5) != 0;
            var isClean = (buildFlags & BuildRuntimeFlags.Clean) != 0;

            if (isHtml5 || buildFlags.HasFlag(BuildRuntimeFlags.Generic))
            {
                beeTargets.Add("runtimepackage-generic");
            }
            if (buildFlags.HasFlag(BuildRuntimeFlags.Docs))
            {
                beeTargets.Add("GenerateAPIDocs");
            }
            if (buildFlags.HasFlag(BuildRuntimeFlags.Html5Debug))
            {
                beeTargets.Add("build/asmjs-debug/runtime/RuntimeFull.js");
                beeTargets.Add("build/asmjs-debug/runtime/RuntimeStripped.js");
            }
            if (buildFlags.HasFlag(BuildRuntimeFlags.Html5Development))
            {
                beeTargets.Add("build/asmjs-devel/runtime/RuntimeFull.js");
                beeTargets.Add("build/asmjs-devel/runtime/RuntimeStripped.js");
            }
            if (buildFlags.HasFlag(BuildRuntimeFlags.Html5Release))
            {
                beeTargets.Add("build/asmjs-release/runtime/RuntimeFull.js");
                beeTargets.Add("build/asmjs-release/runtime/RuntimeStripped.js");
                beeTargets.Add("webgl-runtime-modularized");
            }

            if (beeTargets.Count == 0)
            {
                UnityEngine.Debug.Log("No build targets selected.");
                return;
            }

            // build targets
            var runtimeFolder = "../Runtime/";
            var extraPaths = new string[]
            {
                TinyPreferences.MonoDirectory
#if UNITY_EDITOR_OSX
                // TODO: test this on Windows - required when building PDF docs
                , Path.GetFullPath(runtimeFolder + "artifacts/Stevedore/wkhtmltox/bin")
#endif
            };
            using (var progress = new TinyEditorUtility.ProgressBarScope("Building Runtime...", "..."))
            {
                var beeProgram = PrependMono("bee.exe");
                for (var i = 0; i < beeTargets.Count; ++i)
                {
                    var target = beeTargets[i];
                    progress.Update(target, (float)i / beeTargets.Count);

                    var output = TinyShell.RunInShell($"{beeProgram} --no-colors {target}", new ShellProcessArgs()
                    {
                        WorkingDirectory = new DirectoryInfo(runtimeFolder),
                        ExtraPaths = extraPaths,
                        ThrowOnError = false,
                        MaxIdleTimeInMilliseconds = 60 * 60 * 1000 // 1h
                    });
                    if (false == output.Succeeded)
                    {
                        throw new Exception(
                            $"Failed to build runtime target: {target}. See Editor.log for details. Some steps may require a VPN connection.\n{output.FullOutput}");
                    }
                }

                var buildFolder = runtimeFolder + "build/";
                var distFolder = GetRuntimeDistDirectory() + "/";

                if (isClean)
                {
                    TinyBuildUtilities.PurgeDirectory(new DirectoryInfo(distFolder));
                }

                // copy build outputs
                if (buildFlags.HasFlag(BuildRuntimeFlags.Generic))
                {
                    TinyBuildUtilities.CopyDirectory(buildFolder + "RuntimePackage", distFolder + "runtime", purge: true);
                }

                if (buildFlags.HasFlag(BuildRuntimeFlags.Docs))
                {
                    TinyBuildUtilities.CopyDirectory(runtimeFolder + "docfx/_site_pdf", distFolder + "apidocs", purge: true);
                }

                if (buildFlags.HasFlag(BuildRuntimeFlags.Html5Debug))
                {
                    TinyBuildUtilities.CopyDirectory(buildFolder + "asmjs-debug/runtime", distFolder + "html5/debug", purge: true);
                }

                if (buildFlags.HasFlag(BuildRuntimeFlags.Html5Development))
                {
                    TinyBuildUtilities.CopyDirectory(buildFolder + "asmjs-devel/runtime", distFolder + "html5/development", purge: true);
                }

                if (buildFlags.HasFlag(BuildRuntimeFlags.Html5Release))
                {
                    TinyBuildUtilities.CopyDirectory(buildFolder + "asmjs-release/runtime", distFolder + "html5/release", purge: true);
                }

                if (isHtml5)
                {
                    TinyBuildUtilities.CopyDirectory(buildFolder + "RuntimePackage/Tools", distFolder + "bindgem", purge: true);
                    TinyBuildUtilities.CopyDirectory(buildFolder + "runtimedll", distFolder + "runtimedll", purge: true);

                    foreach (var file in new DirectoryInfo(distFolder + "runtimedll").GetFiles("*.pdb", SearchOption.TopDirectoryOnly))
                    {
                        file.Delete();
                    }

                    var dataDefs = new DirectoryInfo(distFolder + "datadefinitions");
                    TinyBuildUtilities.CopyDirectory(buildFolder + "RuntimePackage/DataDefinitions", distFolder + "datadefinitions", purge: true);

                    dataDefs.Refresh();
                    foreach (var file in dataDefs.GetFiles("*.pdb", SearchOption.TopDirectoryOnly))
                    {
                        file.Delete();
                    }
                    foreach (var file in dataDefs.GetFiles("mscorlib.*", SearchOption.TopDirectoryOnly))
                    {
                        file.Delete();
                    }
                }

                var runtimeRev = distFolder + "runtime-rev.txt";
                var projectRoot = new DirectoryInfo(".");
#if UNITY_EDITOR_WIN
                TinyShell.RunInShell($"git show --format=\"%%H\" --no-patch > {runtimeRev}", new ShellProcessArgs()
#else
                TinyShell.RunInShell($"git show --format=\"%H\" --no-patch > {runtimeRev}", new ShellProcessArgs()
#endif
                {
                    WorkingDirectory = projectRoot,
                    ThrowOnError = true
                });
            }

            BuildTools(clean: isClean);
        }

        [MenuItem("Tiny/INTERNAL/Build Tools/Distribution")]
        internal static void BuildToolsDistribution()
        {
            BuildTools(clean: true);
        }

        [MenuItem("Tiny/INTERNAL/Build Tools/Development")]
        internal static void BuildToolsIncremental()
        {
            BuildTools(clean: false);
        }

        private static void BuildTools(bool clean)
        {
            using (new OverwriteToolsScope())
            {
                var rootDir = new DirectoryInfo(".");
                var toolsDir = new DirectoryInfo("./Tools/");
                var toolsInstallDir = new DirectoryInfo("./Tiny/Tools/");

                if (clean)
                {
                    foreach (var dir in Directory.EnumerateDirectories(toolsDir.FullName, "node_modules", SearchOption.AllDirectories))
                    {
                        Directory.Delete(dir, true);
                    }
                    foreach (var file in Directory.EnumerateFiles(toolsDir.FullName, "package-lock.json", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                    TinyBuildUtilities.PurgeDirectory(toolsInstallDir);
                }

                var extraPaths = new string[]
                {
                    TinyPreferences.MonoDirectory
                };
                using (new TinyEditorUtility.ProgressBarScope("Building Tools...", "Packaging node tools into native executables, please wait!"))
                {
                    var program = PrependMono("Packages/com.unity.tiny/Dist~/bee.exe");
                    var output = TinyShell.RunInShell(program, new ShellProcessArgs()
                    {
                        WorkingDirectory = rootDir,
                        ExtraPaths = extraPaths,
                        ThrowOnError = false,
                        MaxIdleTimeInMilliseconds = 10 * 1000 // 10min
                    });
                    if (!output.Succeeded)
                    {
                        throw new Exception($"Failed to build tools:\n{output.FullOutput}");
                    }
                }
            }
        }

#endif // UNITY_TINY_INTERNAL

        private static void UpdateToolchain(bool force)
        {
            var depDir = new DirectoryInfo("Tiny/Dist");
            var dstBeeScript = new FileInfo("Tiny/Dist/Bees/BuildProgram.bee.cs");
            
            // we can't run bee from the Packages folder, as it may be read-only
            var srcBeeScript = new FileInfo(Path.GetFullPath("Packages/com.unity.tiny/Dist~/Bees/BuildProgram.bee.cs"));
            Assert.IsTrue(srcBeeScript.Exists, "Could not find Tiny toolchain update script");

            if (false == dstBeeScript.Exists)
            {
                // main bee script not found (package install)? synchronize the whole tree
                TinyBuildUtilities.CopyDirectory(
                    from: new DirectoryInfo(Path.GetFullPath("Packages/com.unity.tiny/Dist~")),
                    to: depDir,
                    purge: true);
            }
            else if (force || dstBeeScript.LastWriteTimeUtc < srcBeeScript.LastWriteTimeUtc)
            {
                // new bee script (package update)? synchronize only the Bees folder
                TinyBuildUtilities.CopyDirectory(
                    from: new DirectoryInfo(Path.GetFullPath("Packages/com.unity.tiny/Dist~/Bees")),
                    to: new DirectoryInfo(Path.Combine(depDir.FullName, "Bees")), 
                    purge: true);
            }
            else
            {
                // let bee handle the rest: incremental builds are cheap
            }

            var extraPaths = new string[]
            {
                TinyPreferences.MonoDirectory
            };
            var program = PrependMono("bee.exe");
            var output = TinyShell.RunInShell(program, new ShellProcessArgs()
            {
                WorkingDirectory = depDir,
                ExtraPaths = extraPaths,
                ThrowOnError = false,
                MaxIdleTimeInMilliseconds = 10 * 1000 // 10min
            });
            if (!output.Succeeded || !Directory.Exists(PublicDistFolder))
            {
                throw new Exception($"Failed to update dependencies:\n{output.FullOutput}");
            }
        }

        private static void Install(bool force, bool silent)
        {
            using (new OverwriteToolsScope())
            using (new TinyEditorUtility.ProgressBarScope("Tiny Mode", "Updating toolchain..."))
            {
                UpdateToolchain(force);
                
#if UNITY_EDITOR_OSX
                // TODO: figure out why UnzipFile does not preserve executable bits in some cases
                // chmod +x any native executables here
                TinyBuildUtilities.RunInShell("chmod +x cwebp moz-cjpeg pngcrush",
                    new ShellProcessArgs()
                    {
                        WorkingDirectory = new DirectoryInfo(GetToolDirectory("images/osx")),
                        ExtraPaths = "/bin".AsEnumerable(), // adding this folder just in case, but should be already in $PATH
                        ThrowOnError = false
                    });
                TinyBuildUtilities.RunInShell("chmod +x TinyToolsManager-macos",
                    new ShellProcessArgs()
                    {
                        WorkingDirectory = new DirectoryInfo(GetToolDirectory("manager")),
                        ExtraPaths = "/bin".AsEnumerable(), // adding this folder just in case, but should be already in $PATH
                        ThrowOnError = false
                    });
#endif

                if (!silent)
                {
                    UnityEngine.Debug.Log($"Installed {TinyConstants.ApplicationName} toolchain successfully");
                }
            }
        }

#if !UNITY_TINY_INTERNAL
        [MenuItem(TinyConstants.ApplicationName + "/Import Samples...")]
        internal static void InstallSamples()
        {
            InstallSamples(interactive: true);
        }

        [MenuItem(TinyConstants.ApplicationName + "/Update Runtime")]
        private static void InstallRuntimeMenuItem()
        {
            Install(force: true, silent: false);
        }
#endif

        [MenuItem(TinyConstants.ApplicationName + "/Help/Forums...", false, 10000)]
        private static void OpenUserForums()
        {
            Application.OpenURL("https://forum.unity.com/forums/project-tiny.151/");
        }

        internal static void InstallSamples(bool interactive)
        {
            AssetDatabase.ImportPackage(PublicSamplesPackage, interactive);
            CreateSampleLayers();
        }

        private static void CreateSampleLayers()
        {
            // the MatchThree sample requires pre-defined user layers
            // the names don't matter (not referenced in code), as long as they're defined, so let's not change existing names if any
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            const string layerFormat = "layers.Array.data[{0}]";
            var layerProp = tagManager.FindProperty(string.Format(layerFormat, 8));
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = "Grid";
            }
            layerProp = tagManager.FindProperty(string.Format(layerFormat, 9));
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = "Cutscene";
            }
            tagManager.ApplyModifiedProperties();
        }

        internal static string GetRuntimeDistDirectory()
        {
#if UNITY_TINY_INTERNAL
            return Path.Combine("Tiny", "Runtime");
#else
            return Path.Combine(PublicDistFolder, "Tiny", "Runtime");
#endif
        }

        internal static readonly DirectoryInfo RuntimeDataDefinitionDirectory =
            new DirectoryInfo(GetRuntimeDataDefinitionDirectory(TinyPlatform.Html5));

        internal static FileInfo GetRuntimeDefsAssemblyPath(TinyBuildOptions options)
        {
            var runtimeVariant = GetJsRuntimeVariant(options);
            return new FileInfo(Path.Combine(GetRuntimeDistDirectory(), "runtimedll", runtimeVariant + "-Defs.dll"));
        }

        internal static string GetRuntimeDirectory(TinyPlatform platform, TinyBuildConfiguration configuration)
        {
            if (platform == TinyPlatform.PlayableAd)
            {
                platform = TinyPlatform.Html5;
            }

            return Path.Combine(GetRuntimeDistDirectory(), $"{platform.ToString().ToLower()}", $"{configuration.ToString().ToLower()}");
        }

        private static string GetRuntimeDataDefinitionDirectory(TinyPlatform platform)
        {
            if (platform == TinyPlatform.PlayableAd)
            {
                platform = TinyPlatform.Html5;
            }

            return Path.Combine(GetRuntimeDistDirectory(), "datadefinitions");
        }

        internal static string GetBindgemDirectory()
        {
            return Path.Combine(GetRuntimeDistDirectory(), "bindgem");
        }

        internal static string GetToolDirectory(string toolName)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentException("tool");
            }
#if UNITY_TINY_INTERNAL
            return Path.Combine("Tiny", "Tools", toolName);
#else
            return Path.Combine(PublicDistFolder, "Tiny", "Tools", toolName);
#endif
        }

        private static bool IncludesModule(TinyProject project, string moduleName)
        {
            return project.Module.Dereference(project.Registry).EnumerateDependencies().WithName(moduleName).Any();
        }

        private const string k_BuiltInPhysicsModule = "UTiny.Physics2D";

        internal static string GetJsRuntimeVariant(TinyBuildOptions options)
        {
            return options.Configuration == TinyBuildConfiguration.Release || IncludesModule(options.Project, k_BuiltInPhysicsModule) ? RuntimeVariantFull : RuntimeVariantStripped;
        }
    }
}
