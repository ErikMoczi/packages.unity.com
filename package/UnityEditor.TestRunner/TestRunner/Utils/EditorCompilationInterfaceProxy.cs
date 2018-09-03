using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.TestTools.TestRunner
{
    internal class EditorCompilationInterfaceProxy : IEditorCompilationInterfaceProxy
    {
        public ScriptAssembly[] GetAllEditorScriptAssemblies()
        {
            var options = EditorCompilationInterface.GetAdditionalEditorScriptCompilationOptions();
            return EditorCompilationInterface.Instance.GetAllEditorScriptAssemblies(options);
        }

        public PrecompiledAssembly[] GetAllPrecompiledAssemblies()
        {
            return EditorCompilationInterface.Instance.GetAllPrecompiledAssemblies();
        }
    }
}
