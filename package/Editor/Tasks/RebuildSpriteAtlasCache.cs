using System;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Sprites;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class RebuildSpriteAtlasCache : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return Run(context.GetContextObject<IBuildParameters>());
        }

        static ReturnCode Run(IBuildParameters parameters)
        {
            // TODO: Need a return value if this ever can fail
            Packer.RebuildAtlasCacheIfNeeded(parameters.Target, true, Packer.Execution.Normal);
            return ReturnCode.Success;
        }
    }
}
