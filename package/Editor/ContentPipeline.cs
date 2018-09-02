using System.Diagnostics;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.AssetBundle
{
    public static class ContentPipeline
    {
        public const string kTempBuildPath = "Temp/ContentBuildData";

        public static BuildCallbacks BuildCallbacks = new BuildCallbacks();

        public static ReturnCodes BuildContentPacks(IBuildParameters buildParameters, IBuildContent buildContent, out IBuildResults result)
        {
            var buildTimer = new Stopwatch();
            buildTimer.Start();

            ReturnCodes exitCode;
            result = new BuildBuildResults();

            using (var progressTracker = new ProgressTracker())
            {
                using (var buildCleanup = new BuildStateCleanup(buildParameters.TempOutputFolder))
                {
                    var buildContext = new BuildContext(buildParameters, buildContent, result, progressTracker);
                    buildContext.SetContextObject(new PrefabPackedIdentifiers());
                    buildContext.SetContextObject(new BuildDependencyData());
                    buildContext.SetContextObject(new BuildWriteData());
                    buildContext.SetContextObject(BuildCallbacks);

                    var pipeline = DefaultBuildTasks.Create(DefaultBuildTasks.Presets.AutopackReleaseContent, buildParameters.ScriptInfo == null);
                    exitCode = BuildTasksRunner.Validate(pipeline, buildContext);
                    if (exitCode >= ReturnCodes.Success)
                        exitCode = BuildTasksRunner.Run(pipeline, buildContext);
                }
            }

            buildTimer.Stop();
            if (exitCode >= ReturnCodes.Success)
                BuildLogger.Log("Build Content successful in: {0:c}", buildTimer.Elapsed);
            else if (exitCode == ReturnCodes.Canceled)
                BuildLogger.LogWarning("Build Content canceled in: {0:c}", buildTimer.Elapsed);
            else
                BuildLogger.LogError("Build Content failed in: {0:c}. Error: {1}.", buildTimer.Elapsed, exitCode);

            return exitCode;
        }

        public static ReturnCodes BuildAssetBundles(IBuildParameters buildParameters, IBundleContent bundleContent, out IBundleBuildResults result)
        {
            var buildTimer = new Stopwatch();
            buildTimer.Start();

            ReturnCodes exitCode;
            result = new BundleBuildResults();

            using (var progressTracker = new ProgressTracker())
            {
                using (var buildCleanup = new BuildStateCleanup(buildParameters.TempOutputFolder))
                {
                    var buildContext = new BuildContext(buildParameters, bundleContent, result, progressTracker);
                    buildContext.SetContextObject(new Unity5PackedIdentifiers());
                    buildContext.SetContextObject(new BuildDependencyData());
                    buildContext.SetContextObject(new BundleWriteData());
                    buildContext.SetContextObject(BuildCallbacks);

                    var pipeline = DefaultBuildTasks.Create(DefaultBuildTasks.Presets.AssetBundleCompatible, buildParameters.ScriptInfo == null);
                    exitCode = BuildTasksRunner.Validate(pipeline, buildContext);
                    if (exitCode >= ReturnCodes.Success)
                        exitCode = BuildTasksRunner.Run(pipeline, buildContext);
                }
            }

            buildTimer.Stop();
            if (exitCode >= ReturnCodes.Success)
                BuildLogger.Log("Build Asset Bundles successful in: {0:c}", buildTimer.Elapsed);
            else if (exitCode == ReturnCodes.Canceled)
                BuildLogger.LogWarning("Build Asset Bundles canceled in: {0:c}", buildTimer.Elapsed);
            else
                BuildLogger.LogError("Build Asset Bundles failed in: {0:c}. Error: {1}.", buildTimer.Elapsed, exitCode);

            return exitCode;
        }
    }
}