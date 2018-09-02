using System;
using UnityEditor.Build.Interfaces;
using UnityEditor.Sprites;

namespace UnityEditor.Build.Tasks
{
    public struct RebuildAtlasCache : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBuildParameters>());
        }

        public static ReturnCodes Run(IBuildParameters parameters)
        {
            // TODO: Need a return value if this ever can fail
            Packer.RebuildAtlasCacheIfNeeded(parameters.Target, true, Packer.Execution.Normal);
            return ReturnCodes.Success;
        }
    }
}
