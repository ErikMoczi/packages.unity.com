using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.TestRunner.TestLaunchers;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.TestRunner;
using UnityEngine.TestTools.TestRunner.Callbacks;
using Object = UnityEngine.Object;

namespace UnityEditor.TestTools.TestRunner
{
    internal class TestLaunchFailedException : Exception
    {
        public TestLaunchFailedException() {}
        public TestLaunchFailedException(string message) : base(message) {}
    }

    [Serializable]
    internal class PlayerLauncher : RuntimeTestLauncherBase
    {
        private readonly PlaymodeTestsControllerSettings m_Settings;
        private readonly BuildTarget m_TargetPlatform;
        private string m_TempBuildLocation;
        private ITestRunSettings m_OverloadTestRunSettings;

        public PlayerLauncher(PlaymodeTestsControllerSettings settings, BuildTarget? targetPlatform, ITestRunSettings overloadTestRunSettings)
        {
            m_Settings = settings;
            m_TargetPlatform = targetPlatform ?? EditorUserBuildSettings.activeBuildTarget;
            m_OverloadTestRunSettings = overloadTestRunSettings;
        }

        public override void Run()
        {
            var editorConnectionTestCollector = RemoteTestRunController.instance;
            editorConnectionTestCollector.hideFlags = HideFlags.HideAndDontSave;
            editorConnectionTestCollector.Init(m_TargetPlatform);

            using (var settings = new PlayerLauncherContextSettings(m_OverloadTestRunSettings))
            {
                var sceneName = CreateSceneName();
                var scene = PrepareScene(sceneName);

                var filter = m_Settings.filter.BuildNUnitFilter();
                var runner = LoadTests(filter);
                var exceptionThrown = ExecutePreBuildSetupMethods(runner.LoadedTest, filter);
                if (exceptionThrown)
                {
                    ReopenOriginalScene(m_Settings.originalScene);
                    AssetDatabase.DeleteAsset(sceneName);
                    CallbacksDelegator.instance.RunFailed("Run Failed: One or more errors in a prebuild setup. See the editor log for details.");
                    return;
                }

                var playerBuildOptions = GetBuildOptions(scene);

                var success = BuildAndRunPlayer(playerBuildOptions);
                editorConnectionTestCollector.PostBuildAction();
                ExecutePostBuildCleanupMethods(runner.LoadedTest, filter);

                ReopenOriginalScene(m_Settings.originalScene);
                AssetDatabase.DeleteAsset(sceneName);

                if (!success)
                {
                    ScriptableObject.DestroyImmediate(editorConnectionTestCollector);
                    Debug.LogError("Player build failed");
                    throw new TestLaunchFailedException("Player build failed");
                }

                editorConnectionTestCollector.PostSuccessfulBuildAction();
            }
        }

        public Scene PrepareScene(string sceneName)
        {
            var scene = CreateBootstrapScene(sceneName, runner =>
            {
                runner.AddEventHandlerMonoBehaviour<TestResultRendererCallback>();
                runner.AddEventHandlerMonoBehaviour<PlayModeRunnerCallback>();
                runner.settings = m_Settings;
                runner.AddEventHandlerMonoBehaviour<RemoteTestResultSender>();
            });
            return scene;
        }

        private static bool BuildAndRunPlayer(PlayerLauncherBuildOptions buildOptions)
        {
            Debug.Log("Building player with following options:\n" + buildOptions);


            // Android has to be in listen mode to establish player connection
            if (buildOptions.BuildPlayerOptions.target == BuildTarget.Android)
            {
                buildOptions.BuildPlayerOptions.options &= ~BuildOptions.ConnectToHost;
            }

            var result = BuildPipeline.BuildPlayer(buildOptions.BuildPlayerOptions);
            if (result.summary.result != Build.Reporting.BuildResult.Succeeded)
                Debug.LogError(result.SummarizeErrors());

            return result.summary.result == Build.Reporting.BuildResult.Succeeded;
        }

        private PlayerLauncherBuildOptions GetBuildOptions(Scene scene)
        {
            var buildOptions = new BuildPlayerOptions();
            var scenes = new List<string>() { scene.path };
            scenes.AddRange(EditorBuildSettings.scenes.Select(x => x.path));
            buildOptions.scenes = scenes.ToArray();

            buildOptions.options |= BuildOptions.AutoRunPlayer | BuildOptions.Development | BuildOptions.ConnectToHost | BuildOptions.IncludeTestAssemblies | BuildOptions.StrictMode;
            buildOptions.target = m_TargetPlatform;

            var buildTargetGroup = EditorUserBuildSettings.activeBuildTargetGroup;
            var uniqueTempPathInProject = FileUtil.GetUniqueTempPathInProject();

            //WSA and XboxOne have issues with MAX_PATH, try to minimize the path length
            if ((m_TargetPlatform == BuildTarget.WSAPlayer) || (m_TargetPlatform == BuildTarget.XboxOne))
            {
                uniqueTempPathInProject = uniqueTempPathInProject.Substring(0, 25);
            }

            //Check if Lz4 is supported for the current buildtargetgroup and enable it if need be
            if (PostprocessBuildPlayer.SupportsLz4Compression(buildTargetGroup, m_TargetPlatform))
            {
                if (EditorUserBuildSettings.GetCompressionType(buildTargetGroup) == Compression.Lz4)
                    buildOptions.options |= BuildOptions.CompressWithLz4;
                else if (EditorUserBuildSettings.GetCompressionType(buildTargetGroup) == Compression.Lz4HC)
                    buildOptions.options |= BuildOptions.CompressWithLz4HC;
            }

            m_TempBuildLocation = Path.GetFullPath(uniqueTempPathInProject);

            string extensionForBuildTarget = PostprocessBuildPlayer.GetExtensionForBuildTarget(buildTargetGroup, buildOptions.target, buildOptions.options);

            var playerExecutableName = "PlayerWithTests";
            var locationPath = Path.Combine(m_TempBuildLocation, "PlayerWithTests");

            if (!string.IsNullOrEmpty(extensionForBuildTarget))
            {
                playerExecutableName += string.Format(".{0}", extensionForBuildTarget);
                locationPath = Path.Combine(locationPath, playerExecutableName);
            }

            buildOptions.locationPathName = locationPath;

            return new PlayerLauncherBuildOptions
            {
                BuildPlayerOptions = buildOptions,
                PlayerDirectory = Path.Combine(m_TempBuildLocation, "PlayerWithTests"),
            };
        }
    }
}
