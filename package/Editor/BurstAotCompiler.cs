using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Burst.LowLevel;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.Burst.Editor
{
    internal class BurstAotCompiler : IPostBuildPlayerScriptDLLs
    {
        private const string TempStagingManaged = @"Temp/StagingArea/Data/Managed/";

        int IOrderedCallback.callbackOrder => 0;

        void IPostBuildPlayerScriptDLLs.OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            // Early exit if not activated/supported
            if (!JobsUtility.JobCompilerEnabled || !IsSupportedPlatform(report.summary.platform))
            {
                return;
            }

            // Collect all method signatures
            var methodsToCompile = BurstReflection.FindExecuteMethods();


            var methodsignatures = new List<string>();
            for (var i = 0; i < methodsToCompile.Count; i++)
            {
                var burstCompileTarget = methodsToCompile[i];
                if (!burstCompileTarget.SupportsBurst)
                {
                    continue;
                }

                var methodStr = BurstCompilerService.GetMethodSignature(burstCompileTarget.Method);
                if (methodStr.Contains("Culling"))
                {
                    var methodAndHash = methodStr + "--" + Hash128.Compute(methodStr);
                    methodsignatures.Add(methodAndHash);
                }
            }

            var methodsignaturesAsText = string.Join(",", methodsignatures);

            // Prepare options
            var options = new StringBuilder();
            options.Append("--platform=" + report.summary.platform);

            if (!BurstEditorOptions.EnableBurstSafetyChecks)
                options.Append(" -disable-safety-checks");

            // TODO: Add support for configuring the optimizations/CPU
            // TODO: Add support for per method options

            var folder = Path.GetFullPath(TempStagingManaged);
            Debug.Log($"Burst CompileAot - To Folder {folder}");

            // Prepare assembly folder list
            var assemblyFolders = new List<string>();
            assemblyFolders.Add(folder);
            foreach (var assembly in CompilationPipeline.GetAssemblies())
            {
                foreach (var assemblyRef in assembly.compiledAssemblyReferences)
                {
                    // Exclude folders with assemblies already compiled in the `folder`
                    var assemblyName = Path.GetFileName(assemblyRef);
                    if (assemblyName != null && File.Exists(Path.Combine(folder, assemblyName)))
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

            var assemblyFoldersTxt = string.Join(";", assemblyFolders);

            BurstCompilerService.CompileAot(methodsignaturesAsText, options.ToString(), assemblyFoldersTxt, folder);
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
