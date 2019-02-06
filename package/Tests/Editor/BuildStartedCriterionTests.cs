using System.Collections;
using NUnit.Framework;
using Unity.InteractiveTutorials;
using Unity.InteractiveTutorials.Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Windows;

namespace Unity.InteractiveTutorials.Tests
{
    public class BuildStartedCriterionTests : CriterionTestBase<BuildStartedCriterion>
    {
        [UnityTest]
        public IEnumerator CustomHandlerIsInvoked_IsCompleted()
        {
            m_Criterion.BuildPlayerCustomHandler(new BuildPlayerOptions { scenes = null, target = BuildTarget.StandaloneWindows, locationPathName = "Test/Test.exe", targetGroup = BuildTargetGroup.Unknown });
            yield return null;

            Assert.IsTrue(m_Criterion.completed);

            // Cleanup
            if (Directory.Exists("Test"))
            {
                Directory.Delete("Test");
            }
        }

        [UnityTest]
        public IEnumerator AutoComplete_IsCompleted()
        {
            yield return null;
            Assert.IsTrue(m_Criterion.AutoComplete());
        }
    }
}
