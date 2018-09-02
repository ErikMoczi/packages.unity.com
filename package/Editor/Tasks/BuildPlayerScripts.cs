using System;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Player;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class BuildPlayerScripts : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBuildResults) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBuildResults>());
        }

        static ReturnCode Run(IBuildParameters parameters, IBuildResults results)
        {
            // TODO: Replace with call to GetTempOrCachePath
            // TODO: Create tasks to copy scripts to correct output folder?
            results.ScriptResults = PlayerBuildInterface.CompilePlayerScripts(parameters.GetScriptCompilationSettings(), parameters.TempOutputFolder);
            if (results.ScriptResults.assemblies.IsNullOrEmpty() && results.ScriptResults.typeDB == null)
                return ReturnCode.Error;
            return ReturnCode.Success;
        }
    }
}
