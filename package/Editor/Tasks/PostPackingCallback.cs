using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public struct PostPackingCallback : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IDependencyData), typeof(IWriteData), typeof(IPackingCallback) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IDependencyData>(),
                context.GetContextObject<IWriteData>(), context.GetContextObject<IPackingCallback>());
        }

        public static ReturnCodes Run(IBuildParameters parameters, IDependencyData dependencyData, IWriteData writeData, IPackingCallback callback)
        {
            return callback.PostPacking(parameters, dependencyData, writeData);
        }
    }
}
