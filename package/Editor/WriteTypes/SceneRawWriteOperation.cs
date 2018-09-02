using System;
using UnityEditor.Experimental.Build;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.WriteTypes
{
    [Serializable]
    public struct SceneRawWriteOperation : IWriteOperation
    {
        public WriteCommand command { get; set; }
        public BuildUsageTagSet usageSet { get; set; }
        public BuildReferenceMap referenceMap { get; set; }

        public string scene { get; set; }
        public string processedScene { get; set; }

        public WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            return BundleBuildInterface.WriteSceneSerializedFile(outputFolder, scene, processedScene, command, settings, globalUsage, usageSet, referenceMap);
        }
    }
}
