using System;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.Player;

namespace UnityEditor.Build.Tasks
{
    public struct BuildPlayerScripts : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBuildResults) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBuildResults>());
        }

        public static ReturnCodes Run(IBuildParameters parameters, IBuildResults results)
        {
            // TODO: Replace with call to GetTempOrCachePath
            // TODO: Create tasks to copy scripts to correct output folder?
            results.ScriptResults = PlayerBuildInterface.CompilePlayerScripts(parameters.GetScriptCompilationSettings(), parameters.TempOutputFolder);
            if (results.ScriptResults.assemblies.IsNullOrEmpty() && results.ScriptResults.typeDB == null)
                return ReturnCodes.Error;
            return ReturnCodes.Success;
        }
    }
}
