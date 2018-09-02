using System;
using System.IO;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;
using UnityEngine;

namespace UnityEditor.Build
{
    [Serializable]
    public class BuildParameters : IBuildParameters
    {
        public BuildTarget Target { get; set; }
        public BuildTargetGroup Group { get; set; }
        
        public TypeDB ScriptInfo { get; set; }
        public ScriptCompilationOptions ScriptOptions { get; set; }

        public BuildCompression BundleCompression { get; set; }

        public string OutputFolder { get; set; }
        public string TempOutputFolder { get; protected set; }
        public bool UseCache { get; set; }

        public BuildParameters(BuildTarget target, BuildTargetGroup group, string outputFolder, string tempOutputFolder)
        {
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("Argument cannot be null or empty.", "outputFolder");
            if (string.IsNullOrEmpty(tempOutputFolder))
                throw new ArgumentException("Argument cannot be null or empty.", "tempOutputFolder");

            Target = target;
            Group = group;
            // TODO: Validate target & group
            
            ScriptInfo = null;
            ScriptOptions = ScriptCompilationOptions.None;

            BundleCompression = BuildCompression.DefaultLZMA;

            OutputFolder = outputFolder;
            TempOutputFolder = tempOutputFolder;
            UseCache = true;
        }

        public string GetTempOrCacheBuildPath(Hash128 hash)
        {
            var path = TempOutputFolder;
            if (UseCache)
                path = BuildCache.GetPathForCachedArtifacts(hash);
            Directory.CreateDirectory(path);
            return path;
        }

        public BuildSettings GetContentBuildSettings()
        {
            return new BuildSettings
            {
                group = Group,
                target = Target,
                typeDB = ScriptInfo
            };
        }

        public ScriptCompilationSettings GetScriptCompilationSettings()
        {
            return new ScriptCompilationSettings
            {
                group = Group,
                target = Target,
                options = ScriptOptions
            };
        }

        public BuildCompression GetCompressionForIdentifier(string identifier)
        {
            return BundleCompression;
        }
    }
}