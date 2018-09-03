#if BURST_AOT
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

    internal class BurstAotCompiler : IPostprocessScripts
    {
        private const string BurstAotCompilerExecutable = "bcl.exe";
        private const string TempStagingManaged = @"Temp/StagingArea/Data/Managed/";

        int IOrderedCallback.callbackOrder => 0;
        public void OnPostprocessScripts(BuildReport report)
        {
            // Early exit if not activated/supported
            if (!JobsUtility.JobCompilerEnabled || !IsSupportedPlatform(report.summary.platform))
            {
                return;
            }

            // Collect all method signatures
            var methodsToCompile = BurstReflection.FindExecuteMethods(AssembliesType.Player);

            // Prepare options
            var options = new List<string>();

            for (var i = 0; i < methodsToCompile.Count; i++)
            {
                var burstCompileTarget = methodsToCompile[i];
                if (!burstCompileTarget.SupportsBurst)
                {
                    continue;
                }

                var methodStr = BurstCompilerService.GetMethodSignature(burstCompileTarget.Method);
                var methodFullSignature = methodStr + "--" + Hash128.Compute(methodStr);
                options.Add(GetOption(OptionAotMethod, methodFullSignature) );
            }
            options.Add(GetOption(OptionAotPlatform, report.summary.platform));

            if (!BurstEditorOptions.EnableBurstSafetyChecks)
                options.Add(GetOption(OptionDisableSafetyChecks));

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

            options.AddRange(assemblyFolders.Select(folder => GetOption(OptionAotAssemblyFolder, folder)));

            var outputFilePrefix = Path.Combine(stagingFolder, DefaultLibraryName);
            options.Add(GetOption(OptionAotOutputPath, outputFilePrefix));

            var responseFile = Path.GetTempFileName();
            File.WriteAllLines(responseFile, options);

            //var readback = File.ReadAllText(responseFile);
            //Debug.Log(readback);
            //Console.WriteLine(readback);

            Runner.RunManagedProgram(Path.Combine(BurstLoader.RuntimePath, BurstAotCompilerExecutable), "@" + responseFile);
        }

        public static bool IsSupportedPlatform(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    return true;
            }

            return false;
        }
    }
}
#endif