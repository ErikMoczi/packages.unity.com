#if UNITY_2019_1_OR_NEWER
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class UpdateConfigurationValidation : BaseAssemblyValidation
    {
        public UpdateConfigurationValidation()
        {
            this.TestName = "API Updater Configuration Validation";
        }

        protected override bool IncludePrecompiledAssemblies => true;
        protected override void Run(AssemblyInfo[] info)
        {
            if (Context.ProjectPackageInfo?.name == "com.unity.package-validation-suite")
            {
                Information("PackageValidationSuite update configurations tested by editor tests.");
                return;
            }

            this.TestState = TestState.Running;
            if (info.Length == 0)
            {
                TestState = TestState.Succeeded;
                return;
            }

            var validatorPath = Path.Combine(EditorApplication.applicationContentsPath, "Tools/ScriptUpdater/APIUpdater.ConfigurationValidator.exe");
            if (!File.Exists(validatorPath))
            {
                Information("APIUpdater.ConfigurationValidator.exe is not present in this version of Unity. Not validating update configurations.");
                return;
            }

            var asmdefAssemblies = info.Where(i=>i.assemblyKind == AssemblyInfo.AssemblyKind.Asmdef).ToArray();
            if (asmdefAssemblies.Length > 0)
            {
                var asmdefAssemblyPaths = asmdefAssemblies.Select(i => Path.GetFullPath(i.assembly.outputPath));
                var references = new HashSet<string>(asmdefAssemblies.SelectMany(i => i.assembly.allReferences).Select(Path.GetFullPath));
                RunValidator(references, validatorPath, asmdefAssemblyPaths);
            }

            var precompiledAssemlbyInfo = info.Where(i => i.assemblyKind == AssemblyInfo.AssemblyKind.PrecompiledAssembly).ToArray();
            if (precompiledAssemlbyInfo.Length > 0)
            {
                var precompiledDllPaths = precompiledAssemlbyInfo.Select(i => Path.GetFullPath(i.precompiledDllPath));
                var precompiledAssemblyPaths = CompilationPipeline.GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.All);
                 
                RunValidator(precompiledAssemblyPaths, validatorPath, precompiledDllPaths);
            }
        }

        private void RunValidator(IEnumerable<string> references, string validatorPath, IEnumerable<string> assemblyPaths)
        {
            var responseFilePath = Path.GetTempFileName();
            File.WriteAllLines(responseFilePath, references);

            var monoPath = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin", Application.platform == RuntimePlatform.WindowsEditor ? "mono.exe" : "mono");

            var processStartInfo =
                new ProcessStartInfo(monoPath, $@"{validatorPath} {responseFilePath} -a {string.Join(",", assemblyPaths.Select(p => $"\"{Path.GetFullPath(p)}\""))}")
                {
                    UseShellExecute = false,
                    RedirectStandardError = true
                };
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            var standardError = process.StandardError.ReadToEnd();
            if (process.ExitCode != 0)
            {
                Error(standardError);
            }
            else
            {
                this.TestState = TestState.Succeeded;
            }
        }
    }
}
#endif