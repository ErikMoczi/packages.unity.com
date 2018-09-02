using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class PostScriptsCallback : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBuildResults), typeof(IScriptsCallback) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBuildResults>(), context.GetContextObject<IScriptsCallback>());
        }

        static ReturnCode Run(IBuildParameters parameters, IBuildResults results, IScriptsCallback callback)
        {
            return callback.PostScripts(parameters, results);
        }
    }
}
