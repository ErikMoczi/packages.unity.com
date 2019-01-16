using System.IO;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class AssemblyInfo
    {
        public enum AssemblyKind
        {
            Asmdef,
            PrecompiledAssembly
        }

        public readonly AssemblyKind assemblyKind;
        public readonly Assembly assembly;
        public readonly string asmdefPath;
        public readonly string precompiledDllPath;

        public AssemblyDefinition assemblyDefinition
        {
            get
            {
                if (asmdefPath == null)
                    return null;

                cachedAssemblyDefinition = cachedAssemblyDefinition ?? JsonUtility.FromJson<AssemblyDefinition>(File.ReadAllText(asmdefPath));

                return cachedAssemblyDefinition;
            }
        }
        private AssemblyDefinition cachedAssemblyDefinition;

        public AssemblyInfo(Assembly assembly, string asmdefPath)
        {
            assemblyKind = AssemblyKind.Asmdef;
            this.assembly = assembly;
            this.asmdefPath = asmdefPath;
        }

        public AssemblyInfo(string precompiledDllPath)
        {
            assemblyKind = AssemblyKind.PrecompiledAssembly;
            this.precompiledDllPath = precompiledDllPath;
        }
    }
}
