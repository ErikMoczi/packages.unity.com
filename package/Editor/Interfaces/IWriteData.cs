using System.Collections.Generic;
using UnityEditor.Build.WriteTypes;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.Interfaces
{
    public interface IWriteData : IContextObject
    {
        Dictionary<GUID, List<string>> AssetToFiles { get; }
        Dictionary<string, List<ObjectIdentifier>> FileToObjects { get; }

        List<IWriteOperation> WriteOperations { get; }
    }

    public interface IBundleWriteData : IWriteData
    {
        Dictionary<string, string> FileToBundle { get; }
        Dictionary<string, BuildUsageTagSet> FileToUsageSet { get; }
        Dictionary<string, BuildReferenceMap> FileToReferenceMap { get; }
    }
}