using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace UnityEditor.TestTools.TestRunner
{
    internal class EditorLoadedTestAssemblyProvider
    {
        private const string k_NunitAssemblyName = "nunit.framework";
        private const string k_TestRunnerAssemblyName = "UnityEngine.TestRunner";
        internal const string k_PerformanceTestingAssemblyName = "Unity.PerformanceTesting";


        private readonly IEditorCompilationInterfaceProxy m_CompilationInterfaceProxy;
        private readonly IEditorAssembliesProxy m_EditorAssembliesProxy;

        public EditorLoadedTestAssemblyProvider(IEditorCompilationInterfaceProxy compilationInterfaceProxy, IEditorAssembliesProxy editorAssembliesProxy)
        {
            m_CompilationInterfaceProxy = compilationInterfaceProxy;
            m_EditorAssembliesProxy = editorAssembliesProxy;
        }

        public List<IAssemblyWrapper> GetAssembliesGroupedByType(TestPlatform mode)
        {
            IAssemblyWrapper[] loadedAssemblies = m_EditorAssembliesProxy.loadedAssemblies;
            var allEditorScriptAssemblies = m_CompilationInterfaceProxy.GetAllEditorScriptAssemblies();
            var allPrecompiledAssemblies = m_CompilationInterfaceProxy.GetAllPrecompiledAssemblies();

            IDictionary<TestPlatform, List<IAssemblyWrapper>> result = new Dictionary<TestPlatform, List<IAssemblyWrapper>>()
            {
                {TestPlatform.EditMode, new List<IAssemblyWrapper>() },
                {TestPlatform.PlayMode, new List<IAssemblyWrapper>() }
            };

            foreach (var loadedAssembly in loadedAssemblies)
            {
                if (loadedAssembly.GetReferencedAssemblies().Any(x => x.Name == k_NunitAssemblyName || x.Name == k_TestRunnerAssemblyName || x.Name == k_PerformanceTestingAssemblyName))
                {
                    var assemblyName = new FileInfo(loadedAssembly.Location).Name;
                    var scriptAssemblies = allEditorScriptAssemblies.Where(x => x.Filename == assemblyName).ToList();
                    var precompiledAssemblies = allPrecompiledAssemblies.Where(x => new FileInfo(x.Path).Name == assemblyName).ToList();
                    if (scriptAssemblies.Count < 1 && precompiledAssemblies.Count < 1)
                    {
                        continue;
                    }

                    var assemblyFlags = scriptAssemblies.Any() ? scriptAssemblies.Single().Flags : precompiledAssemblies.Single().Flags;
                    var assemblyType = (assemblyFlags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly ? TestPlatform.EditMode : TestPlatform.PlayMode;
                    result[assemblyType].Add(loadedAssembly);
                }
            }

            return result[mode];
        }
    }
}
