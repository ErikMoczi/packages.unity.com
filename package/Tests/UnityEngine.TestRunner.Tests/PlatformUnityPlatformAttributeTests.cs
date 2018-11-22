using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class PlatformUnityPlatformAttributeTests
    {
        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void WhenUnityPlatformIsSetToWindowsEditor_ThenShouldIncludedOnlyOnWindowsEditor()
        {
            Assert.AreEqual(Application.platform, RuntimePlatform.WindowsEditor);
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsPlayer)]
        public void WhenUnityPlatformIsSetToWindowsPlayer_ThenShouldIncludedOnlyOnWindowsPlayer()
        {
            Assert.AreEqual(Application.platform, RuntimePlatform.WindowsPlayer);
        }

        [Test]
        [UnityPlatform(exclude = new[] {RuntimePlatform.WindowsEditor })]
        public void WhenUnityPlatformExcludesWindowsEditor_ThenShouldNotBeWindowsEditor()
        {
            Assert.AreNotEqual(Application.platform, RuntimePlatform.WindowsEditor);
        }

        [Test]
        [UnityPlatform(RuntimePlatform.OSXEditor)]
        public void WhenUnityPlatformIsSetToOSXEditor_ThenShouldIncludedOnlyOnOSXEditor()
        {
            Assert.AreEqual(Application.platform, RuntimePlatform.OSXEditor);
        }

        [Test]
        [UnityPlatform(RuntimePlatform.OSXPlayer)]
        public void WhenUnityPlatformIsSetToOSXPlayer_ThenShouldIncludedOnlyOnOSXPlayer()
        {
            Assert.AreEqual(Application.platform, RuntimePlatform.OSXPlayer);
        }

        [Test]
        [UnityPlatform(exclude = new[] { RuntimePlatform.OSXEditor })]
        public void WhenUnityPlatformExcludesOSXEditor_ThenShouldNotBeOSXEditor()
        {
            Assert.AreNotEqual(Application.platform, RuntimePlatform.OSXEditor);
        }
    }
}
