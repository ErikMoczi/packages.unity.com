using System;
using System.Collections.Generic;
using UnityEditor.Build.WriteTypes;
using UnityEditor.Build.Interfaces;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build
{
    [Serializable]
    public class BuildWriteData : IWriteData
    {
        public Dictionary<GUID, List<string>> AssetToFiles { get; private set; }
        public Dictionary<string, List<ObjectIdentifier>> FileToObjects { get; private set; }
        public List<IWriteOperation> WriteOperations { get; private set; }

        public BuildWriteData()
        {
            AssetToFiles = new Dictionary<GUID, List<string>>();
            FileToObjects = new Dictionary<string, List<ObjectIdentifier>>();
            WriteOperations = new List<IWriteOperation>();
        }
    }

    [Serializable]
    public class BundleWriteData : IBundleWriteData
    {
        public Dictionary<GUID, List<string>> AssetToFiles { get; private set; }
        public Dictionary<string, List<ObjectIdentifier>> FileToObjects { get; private set; }
        public Dictionary<string, string> FileToBundle { get; private set; }
        public Dictionary<string, BuildUsageTagSet> FileToUsageSet { get; private set; }
        public Dictionary<string, BuildReferenceMap> FileToReferenceMap { get; private set; }
        public List<IWriteOperation> WriteOperations { get; private set; }

        public BundleWriteData()
        {
            AssetToFiles = new Dictionary<GUID, List<string>>();
            FileToObjects = new Dictionary<string, List<ObjectIdentifier>>();
            FileToBundle = new Dictionary<string, string>();
            FileToUsageSet = new Dictionary<string, BuildUsageTagSet>();
            FileToReferenceMap = new Dictionary<string, BuildReferenceMap>();
            WriteOperations = new List<IWriteOperation>();
        }

    }
}
