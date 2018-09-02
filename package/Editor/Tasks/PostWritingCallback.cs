using System;
using UnityEditor.Build.Interfaces;

namespace UnityEditor.Build.Tasks
{
    public struct PostWritingCallback : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IDependencyData), typeof(IWriteData), typeof(IBuildResults), typeof(IWritingCallback) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IDependencyData>(),
                context.GetContextObject<IWriteData>(), context.GetContextObject<IBuildResults>(), context.GetContextObject<IWritingCallback>());
        }

        public static ReturnCodes Run(IBuildParameters parameters, IDependencyData dependencyData, IWriteData writeData, IBuildResults results, IWritingCallback callback)
        {
            return callback.PostWriting(parameters, dependencyData, writeData, results);
        }
    }
}
