using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Static class implementing API wrappers to match BuildPipeline APIs but use the Scriptable Build Pipeline.
    /// <seealso cref="BuildPipeline.BuildAssetBundles"/>
    /// </summary>
    public static class LegacyBuildPipeline
    {
        /// <summary>
        /// Wrapper API to match BuildPipeline API but use the Scriptable Build Pipeline to build Asset Bundles.
        /// <seealso cref="BuildPipeline.BuildAssetBundles(string, BuildAssetBundleOptions, BuildTarget)"/>
        /// </summary>
        /// <remarks>
        /// Not all BuildAssetBundleOptions are supported in the Scriptable Build Pipeline initial release. 
        /// Supported options are: ChunkBasedCompression, UncompressedAssetBundle, DisableWriteTypeTree, and DisableWriteTypeTree.
        /// In addition, existing BuildPipeline callbacks are not yet supported. Est: 2018.3 release for support.
        /// </remarks>
        /// <param name="outputPath">Output path for the AssetBundles.</param>
        /// <param name="assetBundleOptions">AssetBundle building options.</param>
        /// <param name="targetPlatform">Chosen target build platform.</param>
        /// <returns>null - Generating and returning an AssetBundleManifest is not yet supported by the Scriptable Build Pipeline. Est: 2018.3 support.</returns>
        public static AssetBundleManifest BuildAssetBundles(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            var buildInput = ContentBuildInterface.GenerateAssetBundleBuilds();
            return BuildAssetBundles_Internal(outputPath, new BundleBuildContent(buildInput), assetBundleOptions, targetPlatform);
        }

        /// <summary>
        /// Wrapper API to match BuildPipeline API but use the Scriptable Build Pipeline to build Asset Bundles.
        /// <seealso cref="BuildPipeline.BuildAssetBundles(string, AssetBundleBuild[], BuildAssetBundleOptions, BuildTarget)"/>
        /// </summary>
        /// <remarks>
        /// Not all BuildAssetBundleOptions are supported in the Scriptable Build Pipeline initial release. 
        /// Supported options are: ChunkBasedCompression, UncompressedAssetBundle, DisableWriteTypeTree, and DisableWriteTypeTree.
        /// In addition, existing BuildPipeline callbacks are not yet supported. Est: 2018.3 release for support.
        /// </remarks>
        /// <param name="outputPath">Output path for the AssetBundles.</param>
        /// <param name="builds">AssetBundle building map.</param>
        /// <param name="assetBundleOptions">AssetBundle building options.</param>
        /// <param name="targetPlatform">Chosen target build platform.</param>
        /// <returns>null - Generating and returning an AssetBundleManifest is not yet supported by the Scriptable Build Pipeline. Est: 2018.3 release for support.</returns>
        public static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            return BuildAssetBundles_Internal(outputPath, new BundleBuildContent(builds), assetBundleOptions, targetPlatform);
        }

        internal static AssetBundleManifest BuildAssetBundles_Internal(string outputPath, IBundleBuildContent buildContent, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            var group = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            var buildParams = new BuildParameters(targetPlatform, group, outputPath);

            if ((assetBundleOptions & BuildAssetBundleOptions.ChunkBasedCompression) != 0)
                buildParams.BundleCompression = BuildCompression.DefaultLZ4;
            else if ((assetBundleOptions & BuildAssetBundleOptions.UncompressedAssetBundle) != 0)
                buildParams.BundleCompression = BuildCompression.DefaultUncompressed;
            else
                buildParams.BundleCompression = BuildCompression.DefaultLZMA;

            if ((assetBundleOptions & BuildAssetBundleOptions.DisableWriteTypeTree) != 0)
                buildParams.ContentBuildFlags |= ContentBuildFlags.DisableWriteTypeTree;

            IBundleBuildResults results;
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out results);
            if (exitCode < ReturnCode.Success)
                return null;

            // TODO: Return Unity 5 AssetBundleManifest
            return null;
        }
    }
}