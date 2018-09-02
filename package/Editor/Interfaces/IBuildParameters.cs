using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;
using UnityEngine;

namespace UnityEditor.Build.Interfaces
{
    //public interface IScriptParameters : IContextObject
    //{
    //    BuildTarget Target { get; set; }
    //    BuildTargetGroup Group { get; set; }
    //    ScriptCompilationOptions ScriptOptions { get; set; }

    //    ScriptCompilationSettings GetScriptCompilationSettings();
    //}

    //public interface IContentParameters : IContextObject
    //{
    //    BuildTarget Target { get; set; }
    //    BuildTargetGroup Group { get; set; }
    //    TypeDB ScriptInfo { get; set; }

    //    BuildSettings GetContentBuildSettings();

    //    BuildCompression GetCompressionForIdentifier(string identifier);
    //}


    public interface IBuildParameters : IContextObject//IScriptParameters, IContentParameters
    {
        BuildTarget Target { get; set; }
        BuildTargetGroup Group { get; set; }

        TypeDB ScriptInfo { get; set; }

        ScriptCompilationOptions ScriptOptions { get; set; }

        string OutputFolder { get; set; }
        string TempOutputFolder { get; }
        bool UseCache { get; set; }

        string GetTempOrCacheBuildPath(Hash128 hash);

        BuildSettings GetContentBuildSettings();

        BuildCompression GetCompressionForIdentifier(string identifier);

        ScriptCompilationSettings GetScriptCompilationSettings();
    }
}
