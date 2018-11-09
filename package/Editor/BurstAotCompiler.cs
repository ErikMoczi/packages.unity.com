#if ENABLE_BURST_AOT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Burst.Compiler.IL;
using Unity.Burst.LowLevel;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.Scripting.Compilers;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Burst.Editor
{
    using static BurstCompilerOptions;

    internal class BclParser : CompilerOutputParserBase
    {
        private static Regex sNeverMatch = new Regex(@"^\b$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        protected override string GetErrorIdentifier()
        {
            return "";
        }

        protected override Regex GetOutputRegex()
        {
            return sNeverMatch;
        }
    }


    internal class BurstAotCompiler : IPostBuildPlayerScriptDLLs
    {
        private const string BurstAotCompilerExecutable = "bcl.exe";
        private const string TempStaging = @"Temp/StagingArea/";
        private const string TempStagingManaged = TempStaging + @"Data/Managed/";
        private const string TempStagingPlugins = @"Data/Plugins/";

        int IOrderedCallback.callbackOrder => 0;
        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            // Early exit if burst is not activated or the platform is not supported
            if (!BurstEditorOptions.EnableBurstCompilation || !IsSupportedPlatform(report.summary.platform))
            {
                return;
            }

            // Collect all method signatures
            var methodsToCompile = BurstReflection.FindExecuteMethods(AssembliesType.Player);

            if (methodsToCompile.Count == 0)
            {
                return; // Nothing to do
            }

            // Prepare options
            var commonOptions = new List<string>();

            for (var i = 0; i < methodsToCompile.Count; i++)
            {
                var burstCompileTarget = methodsToCompile[i];
                if (!burstCompileTarget.SupportsBurst)
                {
                    continue;
                }

                var methodStr = BurstCompilerService.GetMethodSignature(burstCompileTarget.Method);
                var methodFullSignature = methodStr + "--" + Hash128.Compute(methodStr);
                commonOptions.Add(GetOption(OptionAotMethod, methodFullSignature));
            }

            var targetCpu = TargetCpu.Auto;
            var targetPlatform = GetTargetPlatformAndDefaultCpu(report.summary.platform, out targetCpu);
            commonOptions.Add(GetOption(OptionPlatform, targetPlatform));

            if (!BurstEditorOptions.EnableBurstSafetyChecks)
                commonOptions.Add(GetOption(OptionDisableSafetyChecks));

            // TODO: Add support for configuring the optimizations/CPU
            // TODO: Add support for per method options

            var stagingFolder = Path.GetFullPath(TempStagingManaged);
            //Debug.Log($"Burst CompileAot - To Folder {stagingFolder}");

            // Prepare assembly folder list
            var assemblyFolders = new List<string>();
            assemblyFolders.Add(stagingFolder);

            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            foreach (var assembly in playerAssemblies)
            {
                foreach (var assemblyRef in assembly.compiledAssemblyReferences)
                {
                    // Exclude folders with assemblies already compiled in the `folder`
                    var assemblyName = Path.GetFileName(assemblyRef);
                    if (assemblyName != null && File.Exists(Path.Combine(stagingFolder, assemblyName)))
                    {
                        continue;
                    }

                    var directory = Path.GetDirectoryName(assemblyRef);
                    if (directory != null)
                    {
                        var fullPath = Path.GetFullPath(directory);
                        if (IsMonoReferenceAssemblyDirectory(fullPath) || IsDotNetStandardAssemblyDirectory(fullPath))
                        {
                            // Don't pass reference assemblies to burst because they contain methods without implementation
                            // If burst accidentally resolves them, it will emit calls to burst_abort.
                            fullPath = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/lib/mono/unityaot");
                            fullPath = Path.GetFullPath(fullPath); // GetFullPath will normalize path separators to OS native format
                            if (!assemblyFolders.Contains(fullPath))
                                assemblyFolders.Add(fullPath);

                            fullPath = Path.Combine(fullPath, "Facades");
                            if (!assemblyFolders.Contains(fullPath))
                                assemblyFolders.Add(fullPath);
                        }
                        else if (!assemblyFolders.Contains(fullPath))
                        {
                            assemblyFolders.Add(fullPath);
                        }
                    }
                }
            }

            commonOptions.AddRange(assemblyFolders.Select(folder => GetOption(OptionAotAssemblyFolder, folder)));

            // Gets platform specific IL2CPP plugin folder
            // Only the following platforms are providing a dedicated Tools directory
            switch (report.summary.platform)
            {
                case BuildTarget.XboxOne:
                case BuildTarget.PS4:
                case BuildTarget.Android:
                case BuildTarget.iOS:
                    var pluginFolder = BuildPipeline.GetBuildToolsDirectory(report.summary.platform);
                    commonOptions.Add(GetOption(OptionAotIL2CPPPluginFolder, pluginFolder));
                    break;
            }

            var combinations = new List<BurstOutputCombination>();

            if (targetPlatform == TargetPlatform.macOS)
            {
                // NOTE: OSX has a special folder for the plugin
                // Declared in GetStagingAreaPluginsFolder
                // PlatformDependent\OSXPlayer\Extensions\Managed\OSXDesktopStandalonePostProcessor.cs
                combinations.Add(new BurstOutputCombination("UnityPlayer.app/Contents/Plugins", targetCpu));
            }
            else if (targetPlatform == TargetPlatform.iOS)
            {
                // Check if we are under il2cpp as we may have to force a CPU backend for it
                // TODO: Should use report.summary.platformGroup instead?
                bool isUsingIL2CPP = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone) == ScriptingImplementation.IL2CPP;

                if (!isUsingIL2CPP)
                {
                    var targetArchitecture = (IOSArchitecture)UnityEditor.PlayerSettings.GetArchitecture(report.summary.platformGroup);
                    switch (targetArchitecture)
                    {
                        case IOSArchitecture.ARMv7:
                            targetCpu = TargetCpu.ARMV7A_NEON32;
                            break;
                        case IOSArchitecture.ARM64:
                            targetCpu = TargetCpu.ARMV8A_AARCH64;
                            break;
                        case IOSArchitecture.Universal:
                            // TODO: How do we proceed here?
                            targetCpu = TargetCpu.ARMV7A_NEON32;
                            break;
                    }
                }

                // PlatformDependent\iPhonePlayer\Extensions\Common\BuildPostProcessor.cs
                combinations.Add(new BurstOutputCombination("Frameworks", targetCpu));
            }
            else if (targetPlatform == TargetPlatform.Android)
            {
                // Set the ANDROID_NDK_ROOT so IL2CPP knows where to find the Android toolchain

                var ndkRoot = EditorPrefs.GetString("AndroidNdkRoot");
                if (!string.IsNullOrEmpty(ndkRoot))
                {
                    Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", ndkRoot);
                }

                var androidTargetArch = UnityEditor.PlayerSettings.Android.targetArchitectures;
                if ((androidTargetArch & AndroidArchitecture.ARMv7) != 0)
                {
                    combinations.Add(new BurstOutputCombination("libs/armeabi-v7a", TargetCpu.ARMV7A_NEON32));
                }
                if ((androidTargetArch & AndroidArchitecture.ARM64) != 0)
                {
                    combinations.Add(new BurstOutputCombination("libs/arm64-v8a", TargetCpu.ARMV8A_AARCH64));
                }
                if ((androidTargetArch & AndroidArchitecture.X86) != 0)
                {
                    combinations.Add(new BurstOutputCombination("libs/x86", TargetCpu.X86_SSE2));
                }
            }
            else if (targetPlatform == TargetPlatform.UWP)
            {
                // TODO: Make it configurable for x86 (sse2, sse4)
                combinations.Add(new BurstOutputCombination("Plugins/x64", TargetCpu.X64_SSE4));
                combinations.Add(new BurstOutputCombination("Plugins/x86", TargetCpu.X86_SSE2));
                combinations.Add(new BurstOutputCombination("Plugins/ARM", TargetCpu.THUMB2_NEON32));
                combinations.Add(new BurstOutputCombination("Plugins/ARM64", TargetCpu.ARMV8A_AARCH64));
            }
            else
            {
                combinations.Add(new BurstOutputCombination("Data/Plugins/", targetCpu));
            }

            foreach (var combination in combinations)
            {
                // Gets the output folder
                var stagingOutputFolder = Path.GetFullPath(Path.Combine(TempStaging, combination.OutputPath));
                var outputFilePrefix = Path.Combine(stagingOutputFolder, DefaultLibraryName);

                var options = new List<string>(commonOptions);
                options.Add(GetOption(OptionAotOutputPath, outputFilePrefix));
                options.Add(GetOption(OptionTarget, combination.TargetCpu));

                var responseFile = Path.GetTempFileName();
                File.WriteAllLines(responseFile, options);

                //Debug.Log("Burst compile with response file: " + responseFile);

                try
                {
	                Runner.RunManagedProgram(Path.Combine(BurstLoader.RuntimePath, BurstAotCompilerExecutable),  "@" + responseFile, Application.dataPath + "/..", new BclParser(), null);
                }
                catch (Exception e)
                {
	                Debug.LogError(e.ToString());
                }
            }
        }

        private static bool IsMonoReferenceAssemblyDirectory(string path)
        {
            var editorDir = Path.GetFullPath(EditorApplication.applicationContentsPath);
            return path.IndexOf(editorDir, StringComparison.OrdinalIgnoreCase) != -1 && path.IndexOf("MonoBleedingEdge", StringComparison.OrdinalIgnoreCase) != -1 && path.IndexOf("-api", StringComparison.OrdinalIgnoreCase) != -1;
        }

        private static bool IsDotNetStandardAssemblyDirectory(string path)
        {
            var editorDir = Path.GetFullPath(EditorApplication.applicationContentsPath);
            return path.IndexOf(editorDir, StringComparison.OrdinalIgnoreCase) != -1 && path.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) != -1 && path.IndexOf("shims", StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static TargetPlatform GetTargetPlatformAndDefaultCpu(BuildTarget target, out TargetCpu targetCpu)
        {
            var platform = TryGetTargetPlatform(target, out targetCpu);
            if (!platform.HasValue)
            {
                throw new NotSupportedException("The target platform " + target + " is not supported by the burst compiler");
            }
            return platform.Value;
        }

        public static bool IsSupportedPlatform(BuildTarget target)
        {
            TargetCpu cpu;
            return TryGetTargetPlatform(target, out cpu).HasValue;
        }

        public static TargetPlatform? TryGetTargetPlatform(BuildTarget target, out TargetCpu targetCpu)
        {
            // TODO: Add support for multi-CPU switch
            targetCpu = TargetCpu.Auto;
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                    targetCpu = TargetCpu.X86_SSE4;
                    return TargetPlatform.Windows;
                case BuildTarget.StandaloneWindows64:
                    targetCpu = TargetCpu.X64_SSE4;
                    return TargetPlatform.Windows;
                case BuildTarget.StandaloneOSX:
                    targetCpu = TargetCpu.X64_SSE4;
                    return TargetPlatform.macOS;
                case BuildTarget.StandaloneLinux:
                    targetCpu = TargetCpu.X86_SSE4;
                    return TargetPlatform.Linux;
                case BuildTarget.StandaloneLinux64:
                    targetCpu = TargetCpu.X64_SSE4;
                    return TargetPlatform.Linux;
                case BuildTarget.WSAPlayer:
                    targetCpu = TargetCpu.X64_SSE4;
                    return TargetPlatform.UWP;
                case BuildTarget.XboxOne:
                    targetCpu = TargetCpu.X64_SSE4;
                    return TargetPlatform.XboxOne;
                case BuildTarget.PS4:
                    targetCpu = TargetCpu.X64_SSE4;
                    return TargetPlatform.PS4;
                case BuildTarget.Android:
                    targetCpu = TargetCpu.ARMV7A_NEON32;
                    return TargetPlatform.Android;
                case BuildTarget.iOS:
                    targetCpu = TargetCpu.ARMV7A_NEON32;
                    return TargetPlatform.iOS;
            }
            return null;
        }

        /// <summary>
        /// Not exposed by Unity Editor today.
        /// This is a copy of the Architecture enum from `PlatformDependent\iPhonePlayer\Extensions\Common\BuildPostProcessor.cs` 
        /// </summary>
        private enum IOSArchitecture
        {
            ARMv7,
            ARM64,
            Universal
        }

        /// <summary>
        /// Defines an output path (for the generated code) and the target CPU 
        /// </summary>
        private struct BurstOutputCombination
        {
            public readonly TargetCpu TargetCpu;
            public readonly string OutputPath;

            public BurstOutputCombination(string outputPath, TargetCpu targetCpu)
            {
                TargetCpu = targetCpu;
                OutputPath = outputPath;
            }
        }

    }
}
#endif