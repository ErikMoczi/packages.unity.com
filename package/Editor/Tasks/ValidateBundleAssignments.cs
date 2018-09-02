using System;
using System.Collections.Generic;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.Tasks
{
    public struct ValidateBundleAssignments : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBundleContent) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBundleContent>());
        }

        public static ReturnCodes Run(IBundleContent content)
        {
            if (content.BundleLayout.IsNullOrEmpty())
                return ReturnCodes.Success;

            foreach (KeyValuePair<string, List<GUID>> bundle in content.BundleLayout)
            {
                if (ValidationMethods.ValidAssetBundle(bundle.Value))
                    continue;

                if (ValidationMethods.ValidSceneBundle(bundle.Value))
                    continue;

                BuildLogger.LogError("Bundle '{0}' contains mixed assets and scenes.", bundle.Key);
                return ReturnCodes.Error;
            }

            return ReturnCodes.Success;
        }
    }
}
