using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class PostDependencyCallback : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IDependencyData), typeof(IDependencyCallback) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IDependencyData>(),
                context.GetContextObject<IDependencyCallback>());
        }

        static ReturnCode Run(IBuildParameters parameters, IDependencyData dependencyData, IDependencyCallback callback)
        {
            return callback.PostDependency(parameters, dependencyData);
        }
    }
}
