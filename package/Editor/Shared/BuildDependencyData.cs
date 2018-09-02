using System;
using System.Collections.Generic;
using UnityEditor.Build.Interfaces;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build
{
    [Serializable]
    public class BuildDependencyData : IDependencyData
    {
        public Dictionary<GUID, AssetLoadInfo> AssetInfo { get; private set; }
        public Dictionary<GUID, BuildUsageTagSet> AssetUsage { get; private set; }

        public Dictionary<GUID, SceneDependencyInfo> SceneInfo { get; private set; }
        public Dictionary<GUID, BuildUsageTagSet> SceneUsage { get; private set; }

        public BuildDependencyData()
        {
            AssetInfo = new Dictionary<GUID, AssetLoadInfo>();
            AssetUsage = new Dictionary<GUID, BuildUsageTagSet>();
            SceneInfo = new Dictionary<GUID, SceneDependencyInfo>();
            SceneUsage = new Dictionary<GUID, BuildUsageTagSet>();
        }
    }
}
