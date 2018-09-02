using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Static class containing the main content building entry points into the Scriptable Build Pipeline.
    /// </summary>
    public static class ContentPipeline
    {
        /// <summary>
        /// Default temporary path used for building content data.
        /// </summary>
        public const string kTempBuildPath = "Temp/ContentBuildData";

        /// <summary>
        /// Default callback implementation.
        /// </summary>
        public static BuildCallbacks BuildCallbacks = new BuildCallbacks();

        /// <summary>
        /// Default implementation of generating Asset Bundles using the Scriptable Build Pipeline.
        /// </summary>
        /// <param name="buildParameters">Set of parameters used for building asset bundles.</param>
        /// <param name="buildContent">Set of content and explicit asset bundle layout to build.</param>
        /// <param name="result">Results from building the content and explicit asset bundle layout.</param>
        /// <returns>Return code with status information about success or failure causes.</returns>
        public static ReturnCode BuildAssetBundles(IBuildParameters buildParameters, IBundleBuildContent buildContent, out IBundleBuildResults result)
        {
            var taskList = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleCompatible, buildParameters.ScriptInfo == null);
            return BuildAssetBundles(buildParameters, buildContent, out result, taskList);
        }
        
        /// <summary>
        /// Default implementation of generating Asset Bundles using the Scriptable Build Pipeline.
        /// </summary>
        /// <param name="buildParameters">Set of parameters used for building asset bundles.</param>
        /// <param name="buildContent">Set of content and explicit asset bundle layout to build.</param>
        /// <param name="result">Results from building the content and explicit asset bundle layout.</param>
        /// <param name="taskList">Custom task list for building asset bundles.</param>
        /// <param name="contextObjects">Additional context objects to make available to the build.</param>
        /// <returns>Return code with status information about success or failure causes.</returns>
        public static ReturnCode BuildAssetBundles(IBuildParameters buildParameters, IBundleBuildContent buildContent, out IBundleBuildResults result, IList<IBuildTask> taskList, params IContextObject[] contextObjects)
        {
            // Avoid throwing exceptions in here as we don't want them bubbling up to calling user code
            if (buildParameters == null)
            {
                result = null;
                BuildLogger.LogException(new ArgumentNullException("buildParameters"));
                return ReturnCode.Exception;
            }

            // Avoid throwing exceptions in here as we don't want them bubbling up to calling user code
            if (taskList.IsNullOrEmpty())
            {
                result = null;
                BuildLogger.LogException(new ArgumentException("Argument cannot be null or empty.", "taskList"));
                return ReturnCode.Exception;
            }

            ReturnCode exitCode;
            result = new BundleBuildResults();

            using (var progressTracker = new ProgressTracker())
            {
                using (var wrapper = new BuildInterfacesWrapper())
                {
                    Directory.CreateDirectory(buildParameters.TempOutputFolder);

                    BuildContext buildContext;
                    try
                    {
                        buildContext = new BuildContext(contextObjects);
                        buildContext.SetContextObject(buildParameters);
                        buildContext.SetContextObject(buildContent);
                        buildContext.SetContextObject(result);
                        buildContext.SetContextObject(progressTracker);
                        buildContext.SetContextObject(new BuildCache());
                        buildContext.SetContextObject(new Unity5PackedIdentifiers());
                        buildContext.SetContextObject(new BuildDependencyData());
                        buildContext.SetContextObject(new BundleWriteData());
                        buildContext.SetContextObject(BuildCallbacks);
                    }
                    catch (Exception e)
                    {
                        // Avoid throwing exceptions in here as we don't want them bubbling up to calling user code
                        result = null;
                        BuildLogger.LogException(e);
                        return ReturnCode.Exception;
                    }

                    exitCode = BuildTasksRunner.Validate(taskList, buildContext);
                    if (exitCode >= ReturnCode.Success)
                        exitCode = BuildTasksRunner.Run(taskList, buildContext);

                    if (Directory.Exists(buildParameters.TempOutputFolder))
                        Directory.Delete(buildParameters.TempOutputFolder, true);
                }
            }
            return exitCode;
        }
    }
}
