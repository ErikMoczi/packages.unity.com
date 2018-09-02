using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public struct SwitchToBuildPlatform : IBuildTask
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
            if (EditorUserBuildSettings.SwitchActiveBuildTarget(parameters.Group, parameters.Target))
                return ReturnCodes.Success;
            return ReturnCodes.Error;
        }
    }
}
