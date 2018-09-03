using System;
using System.Collections;
using UnityEditor;

namespace UnityEngine.TestTools
{
    internal class RecompileScripts : IEditModeTestYieldInstruction
    {
        public RecompileScripts() : this(true)
        {
        }

        public RecompileScripts(bool expectScriptCompilation)
        {
            ExpectScriptCompilation = expectScriptCompilation;
            ExpectDomainReload = true;
        }

        public bool ExpectDomainReload { get; private set; }
        public bool ExpectedPlaymodeState { get; }
        public bool ExpectScriptCompilation { get; private set; }

        public IEnumerator Perform()
        {
            AssetDatabase.Refresh();

            if (ExpectScriptCompilation && !EditorApplication.isCompiling)
            {
                throw new Exception("Editor does not need to recompile scripts");
            }

            EditorApplication.UnlockReloadAssemblies();

            while (EditorApplication.isCompiling)
            {
                yield return null;
            }

            if (EditorUtility.scriptCompilationFailed)
            {
                EditorApplication.LockReloadAssemblies();
                throw new Exception("Script compilation failed");
            }
        }
    }
}
