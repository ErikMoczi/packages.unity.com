using System;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.WriteTypes
{
    [Serializable]
    public struct RawWriteOperation : IWriteOperation
    {
        public WriteCommand Command { get; set; }
        public BuildUsageTagSet UsageSet { get; set; }
        public BuildReferenceMap ReferenceMap { get; set; }

        public WriteResult Write(string outputFolder, BuildSettings settings, BuildUsageTagGlobal globalUsage)
        {
            return ContentBuildInterface.WriteSerializedFile(outputFolder, Command, settings, globalUsage, UsageSet, ReferenceMap);
        }

        public Hash128 GetHash128()
        {
            return HashingMethods.CalculateMD5Hash(Command, UsageSet.GetHash128(), ReferenceMap.GetHash128());
        }
    }
}
