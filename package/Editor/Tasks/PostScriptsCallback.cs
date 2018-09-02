using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public struct PostScriptsCallback : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBuildResults), typeof(IScriptsCallback) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBuildResults>(), context.GetContextObject<IScriptsCallback>());
        }

        public static ReturnCodes Run(IBuildParameters parameters, IBuildResults results, IScriptsCallback callback)
        {
            return callback.PostScripts(parameters, results);
        }
    }
}
