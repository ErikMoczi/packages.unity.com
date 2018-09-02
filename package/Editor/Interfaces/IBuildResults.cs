using System.Collections.Generic;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;

namespace UnityEditor.Build.Interfaces
{
    public interface IBuildResults : IContextObject
    {
        ScriptCompilationResult ScriptResults { get; set; }
        Dictionary<string, WriteResult> WriteResults { get; }
    }

    public interface IBundleBuildResults : IBuildResults
    {
        Dictionary<string, BundleDetails> BundleInfos { get; }
    }
}