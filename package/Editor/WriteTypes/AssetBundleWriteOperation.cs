using System;
using UnityEditor.Experimental.Build;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.WriteTypes
{
    [Serializable]
    public struct AssetBundleWriteOperation : IWriteOperation
    {
        public WriteCommand command { get; set; }
        public BuildUsageTagSet usageSet { get; set; }
        public BuildReferenceMap referenceMap { get; set; }

        public AssetBundleInfo info { get; set; }

        public WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            return BundleBuildInterface.WriteSerializedFile(outputFolder, command, settings, globalUsage, usageSet, referenceMap, info);
        }
    }
}
