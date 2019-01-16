using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials.Tests
{
    public class BuildPlayerTests : TestBase
    {
        static string buildPath
        {
            get
            {
                const string buildName = "BuildPlayerTests_Build";
                if (Application.platform == RuntimePlatform.OSXEditor)
                    return buildName + ".app";
                return buildName;
            }
        }

        [SetUp]
        public void SetUp()
        {
            Assert.That(File.Exists(buildPath), Is.False, "Existing file at path " + buildPath);
            Assert.That(Directory.Exists(buildPath), Is.False, "Existing directory at path " + buildPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(buildPath))
                File.Delete(buildPath);

            if (Directory.Exists(buildPath))
                Directory.Delete(buildPath, true);
        }

        static BuildTarget buildTarget
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXEditor:
                        return BuildTarget.StandaloneOSX;

                    case RuntimePlatform.WindowsEditor:
                        return BuildTarget.StandaloneWindows;

                    case RuntimePlatform.LinuxEditor:
                        return BuildTarget.StandaloneLinux;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [Test]
        public void BuildPlayer_Succeeds()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { GetTestAssetPath("EmptyTestScene.unity") },
                locationPathName = buildPath,
                targetGroup = BuildTargetGroup.Standalone,
                target = buildTarget,
                options = BuildOptions.StrictMode,
            };

            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
    }
}
