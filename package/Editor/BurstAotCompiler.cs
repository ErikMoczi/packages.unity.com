#if ENABLE_BURST_AOT
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Burst.LowLevel;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Callbacks;
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

    internal class StaticPostProcessor
    {
        private const string TempSourceLibrary = @"Temp/StagingArea/StaticLibraries";
        [PostProcessBuildAttribute(1)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target == BuildTarget.iOS)
            {
                PostAddStaticLibraries(path);
            }
        }

        private static void PostAddStaticLibraries(string path)
        {
            var assm = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly =>
                assembly.GetName().Name == "UnityEditor.iOS.Extensions.Xcode");
            Type PBXType = assm?.GetType("UnityEditor.iOS.Xcode.PBXProject");
            Type PBXSourceTree = assm?.GetType("UnityEditor.iOS.Xcode.PBXSourceTree");
            if (PBXType != null && PBXSourceTree!=null)
            {
                var project = Activator.CreateInstance(PBXType, null);

                var _sGetPBXProjectPath = PBXType.GetMethod("GetPBXProjectPath");
                var _ReadFromFile = PBXType.GetMethod("ReadFromFile");
                var _sGetUnityTargetName = PBXType.GetMethod("GetUnityTargetName");
                var _TargetGuidByName = PBXType.GetMethod("TargetGuidByName");
                var _AddFileToBuild = PBXType.GetMethod("AddFileToBuild");
                var _AddFile = PBXType.GetMethod("AddFile");
                var _WriteToString = PBXType.GetMethod("WriteToString");

                var sourcetree = new EnumConverter(PBXSourceTree).ConvertFromString("Source");

                string sPath = (string)_sGetPBXProjectPath?.Invoke(null, new object[] {path});
                _ReadFromFile?.Invoke(project, new object[] {sPath});

                string tn = (string) _sGetUnityTargetName?.Invoke(null, null);
                string g = (string) _TargetGuidByName?.Invoke(project, new object[] {tn});

                string srcPath = TempSourceLibrary;
                string dstPath = "Libraries";
                string dstCopyPath = Path.Combine(path, dstPath);

                string burstCppLinkFile = "lib_burst_generated.cpp";
                string libName = DefaultLibraryName + "32.a";
                if (File.Exists(Path.Combine(srcPath, libName)))
                {
                    File.Copy(Path.Combine(srcPath, libName), Path.Combine(dstCopyPath, libName));
                    string fg = (string) _AddFile?.Invoke(project,
                        new object[] {Path.Combine(dstPath, libName), Path.Combine(dstPath, libName), sourcetree});
                    _AddFileToBuild?.Invoke(project, new object[] {g, fg});
                }

                libName = DefaultLibraryName + "64.a";
                if (File.Exists(Path.Combine(srcPath, libName)))
                {
                    File.Copy(Path.Combine(srcPath, libName), Path.Combine(dstCopyPath, libName));
                    string fg = (string) _AddFile?.Invoke(project,
                        new object[] {Path.Combine(dstPath, libName), Path.Combine(dstPath, libName), sourcetree});
                    _AddFileToBuild?.Invoke(project, new object[] {g, fg});
                }

                // Additionally we need a small cpp file (weak symbols won't unfortunately override directly from the libs
                //presumably due to link order?
                string cppPath = Path.Combine(dstCopyPath, burstCppLinkFile);
                File.WriteAllText(cppPath, @"
extern ""C""
{
    void Staticburst_initialize(void* );
    void* StaticBurstStaticMethodLookup(void* );

    int burst_enable_static_linkage = 1;
    void burst_initialize(void* i) { Staticburst_initialize(i); }
    void* BurstStaticMethodLookup(void* i) { return StaticBurstStaticMethodLookup(i); }
}
");
                cppPath = Path.Combine(dstPath, burstCppLinkFile);
                string fileg = (string) _AddFile?.Invoke(project, new object[] {cppPath,cppPath,sourcetree});
                _AddFileToBuild?.Invoke(project, new object[] {g, fileg});

                string pstring = (string) _WriteToString?.Invoke(project, null);
                File.WriteAllText(sPath, pstring);
            }
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
            BurstPlatformAotSettings aotSettingsForTarget = BurstPlatformAotSettings.GetOrCreateSettings(report.summary.platform);

            // Early exit if burst is not activated or the platform is not supported
            if (aotSettingsForTarget.DisableBurstCompilation || !IsSupportedPlatform(report.summary.platform))
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

            // We are grouping methods per their compiler options (float precision...etc)
            var methodGroups = new Dictionary<string, List<string>>();
            for (var i = 0; i < methodsToCompile.Count; i++)
            {
                var burstCompileTarget = methodsToCompile[i];
                if (!burstCompileTarget.IsSupported)
                {
                    continue;
                }

                var methodStr = BurstCompilerService.GetMethodSignature(burstCompileTarget.Method);
                var methodFullSignature = methodStr + "--" + Hash128.Compute(methodStr);

                if (aotSettingsForTarget.DisableOptimisations)
                    burstCompileTarget.Options.DisableOptimizations = true;

                burstCompileTarget.Options.EnableBurstSafetyChecks = !aotSettingsForTarget.DisableSafetyChecks;

                string optionsAsStr;
                if (burstCompileTarget.TryGetOptionsAsString(false, out optionsAsStr))
                {
                    List<string> methodOptions;
                    if (!methodGroups.TryGetValue(optionsAsStr, out methodOptions))
                    {
                        methodOptions = new List<string>();
                        methodGroups.Add(optionsAsStr, methodOptions);
                    }
                    methodOptions.Add(GetOption(OptionAotMethod, methodFullSignature));
                }
            }

            var methodGroupOptions = new List<string>();

            // We should have something like this in the end:
            //
            // --group                1st group of method with the following shared options
            // --float-mode=xxx
            // --method=...
            // --method=...
            //
            // --group                2nd group of methods with the different shared options
            // --float-mode=yyy
            // --method=...
            // --method=...
            if (methodGroups.Count == 1)
            {
                var methodGroup = methodGroups.FirstOrDefault();
                // No need to create a group if we don't have multiple
                methodGroupOptions.Add(methodGroup.Key);
                foreach (var methodOption in methodGroup.Value)
                {
                    methodGroupOptions.Add(methodOption);
                }
            }
            else
            {
                foreach (var methodGroup in methodGroups)
                {
                    methodGroupOptions.Add(GetOption(OptionGroup));
                    methodGroupOptions.Add(methodGroup.Key);
                    foreach (var methodOption in methodGroup.Value)
                    {
                        methodGroupOptions.Add(methodOption);
                    }
                }
            }

            var commonOptions = new List<string>();
            var targetCpu = TargetCpu.Auto;
            var targetPlatform = GetTargetPlatformAndDefaultCpu(report.summary.platform, out targetCpu);
            commonOptions.Add(GetOption(OptionPlatform, targetPlatform));

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
                var targetArchitecture = (IOSArchitecture)UnityEditor.PlayerSettings.GetArchitecture(report.summary.platformGroup);
                if (targetArchitecture == IOSArchitecture.ARMv7 || targetArchitecture == IOSArchitecture.Universal)
                {
                    // PlatformDependent\iPhonePlayer\Extensions\Common\BuildPostProcessor.cs
                    combinations.Add(new BurstOutputCombination("StaticLibraries", TargetCpu.ARMV7A_NEON32, DefaultLibraryName+"32"));
                }
                if (targetArchitecture == IOSArchitecture.ARM64 || targetArchitecture == IOSArchitecture.Universal)
                {
                    // PlatformDependent\iPhonePlayer\Extensions\Common\BuildPostProcessor.cs
                    combinations.Add(new BurstOutputCombination("StaticLibraries", TargetCpu.ARMV8A_AARCH64, DefaultLibraryName+"64"));
                }
            }
            else if (targetPlatform == TargetPlatform.Android)
            {
                // Set the ANDROID_NDK_ROOT so BCL knows where to find the Android toolchain
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT")))
                {
                    var ndkRoot = EditorPrefs.GetString("AndroidNdkRoot");
                    if (!string.IsNullOrEmpty(ndkRoot))
                    {
                        Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", ndkRoot);
                    }
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
                var outputFilePrefix = Path.Combine(stagingOutputFolder, combination.LibraryName);

                var options = new List<string>(commonOptions);
                options.Add(GetOption(OptionAotOutputPath, outputFilePrefix));
                options.Add(GetOption(OptionTarget, combination.TargetCpu));

                if (targetPlatform == TargetPlatform.iOS)
                {
                    options.Add(GetOption(OptionStaticLinkage));
                }

                // finally add method group options
                options.AddRange(methodGroupOptions);

                var responseFile = Path.GetTempFileName();
                File.WriteAllLines(responseFile, options);

                //Debug.Log("Burst compile with response file: " + responseFile);

                try
                {
	                Runner.RunManagedProgram(Path.Combine(BurstLoader.RuntimePath, BurstAotCompilerExecutable), "--debug=true " + "@" + responseFile, Application.dataPath + "/..", new BclParser(), null);
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
            public readonly string LibraryName;

            public BurstOutputCombination(string outputPath, TargetCpu targetCpu, string libraryName = DefaultLibraryName)
            {
                TargetCpu = targetCpu;
                OutputPath = outputPath;
                LibraryName = libraryName;
            }
        }

    }
}
#endif
