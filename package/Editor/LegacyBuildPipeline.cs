using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.Build.Pipeline
{
    public static class LegacyBuildPipeline
    {
        public static AssetBundleManifest BuildAssetBundles(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            var buildInput = ContentBuildInterface.GenerateAssetBundleBuilds();
            return BuildAssetBundles_Internal(outputPath, new BundleBuildContent(buildInput), assetBundleOptions, targetPlatform);
        }

        public static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            return BuildAssetBundles_Internal(outputPath, new BundleBuildContent(builds), assetBundleOptions, targetPlatform);
        }

        internal static AssetBundleManifest BuildAssetBundles_Internal(string outputPath, IBundleBuildContent buildContent, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            var group = UnityEditor.BuildPipeline.GetBuildTargetGroup(targetPlatform);
            var buildParams = new BuildParameters(targetPlatform, group, outputPath);

            if ((assetBundleOptions & BuildAssetBundleOptions.ChunkBasedCompression) != 0)
                buildParams.BundleCompression = BuildCompression.DefaultLZ4;
            else if ((assetBundleOptions & BuildAssetBundleOptions.UncompressedAssetBundle) != 0)
                buildParams.BundleCompression = BuildCompression.DefaultUncompressed;
            else
                buildParams.BundleCompression = BuildCompression.DefaultLZMA;

            IBundleBuildResults results;
            ReturnCodes exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out results);
            if (exitCode < ReturnCodes.Success)
                return null;

            // TODO: Return Unity 5 Manifest
            return null;
        }
    }
}