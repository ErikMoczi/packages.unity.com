using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class SwitchToBuildPlatform : IBuildTask
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
            if (EditorUserBuildSettings.SwitchActiveBuildTarget(parameters.Group, parameters.Target))
                return ReturnCode.Success;
            return ReturnCode.Error;
        }
    }
}
