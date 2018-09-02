using System;
using System.Collections.Generic;
using UnityEditor.Build.Interfaces;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;

namespace UnityEditor.Build
{
    [Serializable]
    public class BuildBuildResults : IBuildResults
    {
        public ScriptCompilationResult ScriptResults { get; set; }
        public Dictionary<string, WriteResult> WriteResults { get; private set; }

        public BuildBuildResults()
        {
            WriteResults = new Dictionary<string, WriteResult>();
        }
    }

    [Serializable]
    public class BundleBuildResults : IBundleBuildResults
    {
        public ScriptCompilationResult ScriptResults { get; set; }
        public Dictionary<string, BundleDetails> BundleInfos { get; private set; }
        public Dictionary<string, WriteResult> WriteResults { get; private set; }

        public BundleBuildResults()
        {
            BundleInfos = new Dictionary<string, BundleDetails>();
            WriteResults = new Dictionary<string, WriteResult>();
        }
    }
}
