using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class ValidateBundleAssignments : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBundleBuildContent) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return Run(context.GetContextObject<IBundleBuildContent>());
        }

        static ReturnCode Run(IBundleBuildContent buildContent)
        {
            if (buildContent.BundleLayout.IsNullOrEmpty())
                return ReturnCode.Success;

            foreach (KeyValuePair<string, List<GUID>> bundle in buildContent.BundleLayout)
            {
                if (ValidationMethods.ValidAssetBundle(bundle.Value))
                    continue;

                if (ValidationMethods.ValidSceneBundle(bundle.Value))
                    continue;

                BuildLogger.LogError("Bundle '{0}' contains mixed assets and scenes.", bundle.Key);
                return ReturnCode.Error;
            }

            return ReturnCode.Success;
        }
    }
}
