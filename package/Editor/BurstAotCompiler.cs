#if ENABLE_BURST_AOT
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Burst.Compiler.IL;
using Unity.Burst.LowLevel;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Burst.Editor
{
    using static BurstCompilerOptions;

    struct BurstOutputCombination
    {
        public readonly TargetCpu TargetCpu;
        public readonly string OutputPath;

        public BurstOutputCombination(string outputPath, TargetCpu targetCpu = TargetCpu.Auto)
        {
            TargetCpu = targetCpu;
            OutputPath = outputPath;
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
            // Early exit if not activated/supported
            if (!JobsUtility.JobCompilerEnabled || !IsSupportedPlatform(report.summary.platform))
            {
                return;
            }

            // Collect all method signatures
            var methodsToCompile = BurstReflection.FindExecuteMethods(AssembliesType.Player);

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

            var targetPlatform = GetTargetPlatform(report.summary.platform);
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
                        if (!assemblyFolders.Contains(fullPath))
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

            BurstOutputCombination[] combinations;

            if (targetPlatform == TargetPlatform.macOS)
            {
                // NOTE: OSX has a special folder for the plugin
                // Declared in GetStagingAreaPluginsFolder
                // PlatformDependent\OSXPlayer\Extensions\Managed\OSXDesktopStandalonePostProcessor.cs
                combinations = new[] { new BurstOutputCombination("UnityPlayer.app/Contents/Plugins") };
            }
            else if (targetPlatform == TargetPlatform.iOS)
            {
                // PlatformDependent\iPhonePlayer\Extensions\Common\BuildPostProcessor.cs
                // TODO: Add support for CPU (v8, arm64...)
                combinations = new[] { new BurstOutputCombination("Frameworks") };
            }
            else if (targetPlatform == TargetPlatform.Android)
            {
                // TODO: Add support for CPU (v8, arm64...)
                combinations = new[] { new BurstOutputCombination("libs/armeabi-v7a") };
            }
            else if (targetPlatform == TargetPlatform.UWP)
            {
                combinations = new[]
                {
                    new BurstOutputCombination("Plugins/x64", TargetCpu.Auto),
                    new BurstOutputCombination("Plugins/x86", TargetCpu.X86_SSE2),
                    new BurstOutputCombination("Plugins/ARM", TargetCpu.THUMB2_NEON32),
                };
            }
            else
            {
                combinations = new[] { new BurstOutputCombination("Data/Plugins/") };
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

                Runner.RunManagedProgram(Path.Combine(BurstLoader.RuntimePath, BurstAotCompilerExecutable), "@" + responseFile);
            }
        }

        public static TargetPlatform GetTargetPlatform(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    return TargetPlatform.Windows;
                case BuildTarget.StandaloneOSX:
                    return TargetPlatform.macOS;
                case BuildTarget.StandaloneLinux64:
                    return TargetPlatform.Linux;
                case BuildTarget.WSAPlayer:
                    return TargetPlatform.UWP;
                case BuildTarget.XboxOne:
                    return TargetPlatform.XboxOne;
                case BuildTarget.PS4:
                    return TargetPlatform.PS4;
                case BuildTarget.Android:
                    return TargetPlatform.Android;
                case BuildTarget.iOS:
                    return TargetPlatform.iOS;
            }

            throw new NotSupportedException("The target platform " + target + " is not supported by the burst compiler");
        }

        public static bool IsSupportedPlatform(BuildTarget target)
        {
            // NOTE: Update GetTatgetPlatform accordingly
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.WSAPlayer:
                case BuildTarget.XboxOne:
                case BuildTarget.PS4:
                case BuildTarget.Android:
                case BuildTarget.iOS:
                    return true;
            }

            return false;
        }
    }
}
#endif