using System;
using UnityEditor.Build.Interfaces;

namespace UnityEditor.Build.Tasks
{
    public struct SetBundleSettingsTypeDB : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildResults), typeof(IBuildParameters) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBuildResults>(), context.GetContextObject<IBuildParameters>());
        }

        public static ReturnCodes Run(IBuildResults results, IBuildParameters parameters)
        {
            parameters.ScriptInfo = results.ScriptResults.typeDB;
            return ReturnCodes.Success;
        }
    }
}
