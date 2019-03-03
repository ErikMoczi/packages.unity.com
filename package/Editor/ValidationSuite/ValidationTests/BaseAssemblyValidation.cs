using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    /// <summary>
    /// Base validation class for validations that operate on the compiled asmdefs in the package
    /// </summary>
    internal abstract class BaseAssemblyValidation : BaseValidation
    {
        protected virtual bool IncludePrecompiledAssemblies => false;

        protected sealed override void Run()
        {
            //does it compile?
            if (EditorUtility.scriptCompilationFailed)
            {
                Error("Compilation failed. Please fix any compilation errors.");
                return;
            }

            if (EditorApplication.isCompiling)
            {
                Error("Compilation in progress. Please wait for compilation to finish.");
                return;
            }

            var relevantAssemblyInfo = GetRelevantAssemblyInfo();
            Run(relevantAssemblyInfo);
        }

        protected abstract void Run(AssemblyInfo[] info);

        protected AssemblyInfo[] GetRelevantAssemblyInfo()
        {
            var packagePath = Path.GetFullPath(Context.ProjectPackageInfo.path);
            var files = new HashSet<string>(Directory.GetFiles(packagePath, "*", SearchOption.AllDirectories));

            var allAssemblyInfo = CompilationPipeline.GetAssemblies().Select(AssemblyInfoFromAssembly).Where(a => a != null)
                .ToArray();

            var assemblyInfoOutsidePackage =
                allAssemblyInfo.Where(a => !a.asmdefPath.StartsWith(packagePath)).ToArray();
            foreach (var badFilePath in assemblyInfoOutsidePackage.SelectMany(a => a.assembly.sourceFiles).Where(files.Contains))
                Error("Script \"{0}\" is not included by any asmdefs in the package.", badFilePath);

            var relevantAssemblyInfo =
                allAssemblyInfo.Where(a => a.asmdefPath.StartsWith(packagePath));

            if (IncludePrecompiledAssemblies)
            {
                relevantAssemblyInfo = relevantAssemblyInfo.Concat(
                        files.Where(f => string.Equals(Path.GetExtension(f), ".dll", StringComparison.OrdinalIgnoreCase) && IsManagedDll(f))
                            .Select(f => new AssemblyInfo(f)))
                    .ToArray();
            }

            return relevantAssemblyInfo.ToArray();
        }

        private static bool IsManagedDll(string f)
        {
            var dllType = InternalEditorUtility.DetectDotNetDll(f);
            switch (dllType)
            {
                case DllType.ManagedNET35:
                case DllType.ManagedNET40:
                case DllType.UnknownManaged:
                    return true;
                default:
                    return false;
            }
        }

        private AssemblyInfo AssemblyInfoFromAssembly(Assembly assembly)
        {
            var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly.name);
            if (string.IsNullOrEmpty(path))
                return null;

            var asmdefPath = Path.GetFullPath(path);
            return new AssemblyInfo(assembly, asmdefPath);
        }
    }
}