using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class EditorUnityPlatformAttributeTests
    {
        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void WhenUnityPlatformIsSetToWindowsEditor_ThenShouldIncludedOnlyOnWindowsEditor()
        {
            Assert.AreEqual(Application.platform, RuntimePlatform.WindowsEditor);
        }

        [Test]
        [UnityPlatform(RuntimePlatform.OSXEditor)]
        public void WhenUnityPlatformIsSetToOSXEditorThenShouldIncludedOnlyOnOSXEditor()
        {
            Assert.AreEqual(Application.platform, RuntimePlatform.OSXEditor);
        }
    }
}
