using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.WriteTypes
{
    [Serializable]
    public struct SceneDataWriteOperation : IWriteOperation
    {
        public WriteCommand Command { get; set; }
        public BuildUsageTagSet UsageSet { get; set; }
        public BuildReferenceMap ReferenceMap { get; set; }

        public string Scene { get; set; }
        public string ProcessedScene { get; set; }
        public PreloadInfo PreloadInfo { get; set; }

        public WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            return ContentBuildInterface.WriteSceneSerializedFile(outputFolder, Scene, ProcessedScene, Command, settings, globalUsage, UsageSet, ReferenceMap, PreloadInfo);
        }

        public Hash128 GetHash128()
        {
            return HashingMethods.CalculateMD5Hash(Command, UsageSet.GetHash128(), ReferenceMap.GetHash128(), Scene, ProcessedScene, PreloadInfo);
        }
    }
}
