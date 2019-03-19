using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestRunner.TestLaunchers;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestRunner.Utils;
using UnityEngine.TestTools.TestRunner;
using UnityEngine.TestTools.TestRunner.Callbacks;

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
        private ITestRunSettings m_OverloadTestRunSettings;
        private string m_TestPlayerPath;
        private bool m_DoNotLaunchTestPlayer;
        private string m_SceneName;

        public PlayerLauncher(PlaymodeTestsControllerSettings settings, BuildTarget? targetPlatform, ITestRunSettings overloadTestRunSettings, string testPlayerPath = null, bool doNotLaunchTestPlayer = false)
        {
            m_Settings = settings;
            m_TargetPlatform = targetPlatform ?? EditorUserBuildSettings.activeBuildTarget;
            m_OverloadTestRunSettings = overloadTestRunSettings;
            m_TestPlayerPath = testPlayerPath;
            m_DoNotLaunchTestPlayer = doNotLaunchTestPlayer;
        }

        protected override RuntimePlatform? TestTargetPlatform
        {
            get { return BuildTargetConverter.TryConvertToRuntimePlatform(m_TargetPlatform); }
        }

        public override void Run()
        {
            var editorConnectionTestCollector = RemoteTestRunController.instance;
            editorConnectionTestCollector.hideFlags = HideFlags.HideAndDontSave;
            editorConnectionTestCollector.Init(m_TargetPlatform);

            var remotePlayerLogController = RemotePlayerLogController.instance;
            remotePlayerLogController.hideFlags = HideFlags.HideAndDontSave;

            using (var settings = new PlayerLauncherContextSettings(m_OverloadTestRunSettings))
            {
                m_SceneName = CreateSceneName();
                var scene = PrepareScene(m_SceneName);

                var filter = m_Settings.filter.BuildNUnitFilter();
                var runner = LoadTests(filter);
                var exceptionThrown = ExecutePreBuildSetupMethods(runner.LoadedTest, filter);
                if (exceptionThrown)
                {
                    ReopenOriginalScene(m_Settings.originalScene);
                    AssetDatabase.DeleteAsset(m_SceneName);
                    CallbacksDelegator.instance.RunFailed("Run Failed: One or more errors in a prebuild setup. See the editor log for details.");
                    return;
                }

                var playerBuildOptions = GetBuildOptions(scene);

                var success = BuildAndRunPlayer(playerBuildOptions);
                editorConnectionTestCollector.PostBuildAction();
                ExecutePostBuildCleanupMethods(runner.LoadedTest, filter);

                ReopenOriginalScene(m_Settings.originalScene);
                AssetDatabase.DeleteAsset(m_SceneName);

                if (!success)
                {
                    ScriptableObject.DestroyImmediate(editorConnectionTestCollector);
                    Debug.LogError("Player build failed");
                    throw new TestLaunchFailedException("Player build failed");
                }

                editorConnectionTestCollector.PostSuccessfulBuildAction();

                if (m_DoNotLaunchTestPlayer)
                {
                    EditorApplication.Exit(0);
                }
            }
        }

        public Scene PrepareScene(string sceneName)
        {
            var scene = CreateBootstrapScene(sceneName, runner =>
            {
                runner.AddEventHandlerMonoBehaviour<PlayModeRunnerCallback>();
                runner.settings = m_Settings;
                runner.AddEventHandlerMonoBehaviour<RemoteTestResultSender>();
                runner.AddEventHandlerScriptableObject<TestRunCallbackListener>();
            });
            return scene;
        }

        private static bool BuildAndRunPlayer(PlayerLauncherBuildOptions buildOptions)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Building player with following options:\n{0}", buildOptions);


            // Android has to be in listen mode to establish player connection
            if (buildOptions.BuildPlayerOptions.target == BuildTarget.Android)
            {
                buildOptions.BuildPlayerOptions.options &= ~BuildOptions.ConnectToHost;
            }

            // For now, so does Lumin
            if (buildOptions.BuildPlayerOptions.target == BuildTarget.Lumin)
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
            var reduceBuildLocationPathLength = false;

            //Some platforms hit MAX_PATH limits during the build process, in these cases minimize the path length
            if ((m_TargetPlatform == BuildTarget.WSAPlayer) || (m_TargetPlatform == BuildTarget.XboxOne))
            {
                reduceBuildLocationPathLength = true;
            }

            var scenes = new List<string>() { scene.path };
            scenes.AddRange(EditorBuildSettings.scenes.Select(x => x.path));
            buildOptions.scenes = scenes.ToArray();

            buildOptions.options |= BuildOptions.Development | BuildOptions.ConnectToHost | BuildOptions.IncludeTestAssemblies | BuildOptions.StrictMode;
            if (!m_DoNotLaunchTestPlayer)
            {
                buildOptions.options |= BuildOptions.AutoRunPlayer;
            }

            buildOptions.target = m_TargetPlatform;

            if (EditorUserBuildSettings.waitForPlayerConnection)
                buildOptions.options |= BuildOptions.WaitForPlayerConnection;

            var buildTargetGroup = EditorUserBuildSettings.activeBuildTargetGroup;

            //Check if Lz4 is supported for the current buildtargetgroup and enable it if need be
            if (PostprocessBuildPlayer.SupportsLz4Compression(buildTargetGroup, m_TargetPlatform))
            {
                if (EditorUserBuildSettings.GetCompressionType(buildTargetGroup) == Compression.Lz4)
                    buildOptions.options |= BuildOptions.CompressWithLz4;
                else if (EditorUserBuildSettings.GetCompressionType(buildTargetGroup) == Compression.Lz4HC)
                    buildOptions.options |= BuildOptions.CompressWithLz4HC;
            }

            string buildLocation;
            if (!string.IsNullOrEmpty(m_TestPlayerPath))
            {
                buildLocation = m_TestPlayerPath;
            }
            else
            {
                var uniqueTempPathInProject = FileUtil.GetUniqueTempPathInProject();
                var playerDirectoryName = reduceBuildLocationPathLength ? "PwT" : "PlayerWithTests";

                if (reduceBuildLocationPathLength)
                {
                    uniqueTempPathInProject = Path.GetTempFileName();
                    File.Delete(uniqueTempPathInProject);
                    Directory.CreateDirectory(uniqueTempPathInProject);
                }
                var tempPath = Path.GetFullPath(uniqueTempPathInProject);
                buildLocation = Path.Combine(tempPath, playerDirectoryName);
            }

            string extensionForBuildTarget = PostprocessBuildPlayer.GetExtensionForBuildTarget(buildTargetGroup, buildOptions.target, buildOptions.options);

            var playerExecutableName = "PlayerWithTests";
            playerExecutableName += string.Format(".{0}", extensionForBuildTarget);

            buildOptions.locationPathName = Path.Combine(buildLocation, playerExecutableName);

            return new PlayerLauncherBuildOptions
            {
                BuildPlayerOptions = buildOptions,
                PlayerDirectory = buildLocation,
            };
        }
    }
}
