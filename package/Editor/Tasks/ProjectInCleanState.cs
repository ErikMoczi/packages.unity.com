using System;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class ProjectInCleanState : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            return Run();
        }

        static ReturnCode Run()
        {
            if (ValidationMethods.HasDirtyScenes())
                return ReturnCode.UnsavedChanges;
            return ReturnCode.Success;
        }
    }
}
