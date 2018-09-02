using System.Collections.Generic;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.Interfaces
{
    public interface IDependencyData : IContextObject
    {
        Dictionary<GUID, AssetLoadInfo> AssetInfo { get; }
        Dictionary<GUID, BuildUsageTagSet> AssetUsage { get; }

        Dictionary<GUID, SceneDependencyInfo> SceneInfo { get; }
        Dictionary<GUID, BuildUsageTagSet> SceneUsage { get; }
    }
}