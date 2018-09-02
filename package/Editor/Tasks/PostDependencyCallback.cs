using System;
using UnityEditor.Build.Interfaces;

namespace UnityEditor.Build.Tasks
{
    public struct PostDependencyCallback : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IDependencyData), typeof(IDependencyCallback) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IDependencyData>(),
                context.GetContextObject<IDependencyCallback>());
        }

        public static ReturnCodes Run(IBuildParameters parameters, IDependencyData dependencyData, IDependencyCallback callback)
        {
            return callback.PostDependency(parameters, dependencyData);
        }
    }
}
