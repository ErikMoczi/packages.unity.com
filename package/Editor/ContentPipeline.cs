using System.Diagnostics;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Content
{
    public static class ContentPipeline
    {
        public const string kTempBuildPath = "Temp/ContentBuildData";

        public static BuildCallbacks BuildCallbacks = new BuildCallbacks();

        public static ReturnCodes BuildAssetBundles(IBuildParameters buildParameters, IBundleBuildContent buildContent, out IBundleBuildResults result)
        {
            ReturnCodes exitCode;
            result = new BundleBuildResults();

            using (var progressTracker = new ProgressTracker())
            {
                using (var buildCleanup = new BuildStateCleanup(buildParameters.TempOutputFolder))
                {
                    var buildContext = new BuildContext(buildParameters, buildContent, result, progressTracker);
                    buildContext.SetContextObject(new BuildCache());
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
            return exitCode;
        }
    }
}
