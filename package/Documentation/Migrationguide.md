# Migration guide

To build your AssetBundles with the SBP package, use the `LegacyBuildPipeline.BuildAssetBundles` method wherever you used the `BuildPipeline.BuildAssetBundle` method.

__Note__: Not all of the features that were supported previously are supported in SBP.

The following tables list the features of the `LegacyBuildPipeline.BuildAssetBundles` method in comparison to the `BuildPipeline.BuildAssetBundle` method.

| Feature| Support | Notes |
|:---|:---|:---|
| AssetBundles| Supported | SBP builds AssetBundles built nearly identically to the previous build pipeline. You load them in a similar manner to how you currently load AssetBundles.  |
| Incremental Building | Supported | SBP implements this feature using the BuildCache. |
| Asset loading path| Behavior changed | To avoid loading collision and associated runtime issues, SBP bundles only load an Asset if the path you pass in exactly matches the source Asset path at build time, or the custom identifier you passed in. For example, if your source Asset is located at *Assets/ExampleFolder/Asset.prefab* then that is the exact path you must use to load the Asset, including the correct upper and lower case, and all punctuation. To change this behavior, use the overload that takes an `AssetBundleBuild[]` and use the `addressableNames` field to pass in a custom identifier to use when loading. |
| AssetBundle Manifest | Behavior changed | SBP implements replacement functionality using the new class name `LegacyAssetBundleManifest`. This has an identical API to the existing class. |
| AssetBundle Variants| Not supported | There is currently no replacement functionality for AssetBundle Variants. |

BuildAssetBundleOptions Enum:

| Value| Support | Notes |
|:---|:---|:---|
| UncompressedAssetBundle| Supported | Identical to using `BuildCompression.DefaultUncompressed` with the new API. |
| ChunkBasedCompression | Supported | Identical to using `BuildCompression.DefaultLZ4` with the new API. __Note__: This has always been LZ4HC in the Editor, and LZ4 if it was recompressed at Runtime. |
| DisableWriteTypeTree | Supported | Identical to using `ContentBuildFlags.DisableWriteTyp with the new API. |
| DeterministicAssetBundle | Supported | This is enabled by default, and it can’t be disabled. SBP builds deterministically. |
| ForceRebuildAssetBundle | Supported | Identical to using `IBuildParameters.UseCache = false;` with the new API. |
| AppendHashToAssetBundleName | Supported | Identical to using IBundleBuildParameters.AppendHash = true; with the new API.  |
| DisableLoadAssetByFileName | Always enabled | This is enabled by default, and can’t be disabled. SBP is strict about the rule: "what you pass in is exactly what you get out". If you pass in *My/Example1/Example2/Asset.asset* as the file name to use to load the Asset, you must use that identifier exactly, including the correct upper and lower case, and all punctuation. |
| DisableLoadAssetByFileNameWithExtension | Always enabled | See above details on DisableLoadAssetByFileName. |
| IgnoreTypeTreeChanges | Not supported | The incremental build system used this value to prevent rebuilding AssetBundles when an Asset's serialization layout changed, but the data for the Asset itself did not change. SBP currently rebuilds if there are any changes. |
| StrictMode | Not supported | The SBP is stricter about properly building AssetBundles and knowing when builds fail.  |
| DryRunBuild | Not supported | SBP works fundamentally differently. It is faster to do a full build to determine if anything has changed. |