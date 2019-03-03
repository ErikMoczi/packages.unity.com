using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine.TestTools;

namespace FrameworkTests
{
    public class AssetPipelineIgnoreAttributeTests
    {
        [Test]
        [AssetPipelineIgnore.IgnoreInV1("Ignored intentionally for test purposes.")]
        public void TestIgnoredInAssetPipelineV1_OnlyRunsAgainstV2Environment()
        {
            Assert.False(AssetDatabase.IsV1Enabled(), "The ignored mode (V1) was enabled.");
            Assert.True(AssetDatabase.IsV2Enabled(), "Inconsistend mode.");
        }

        [Test]
        [AssetPipelineIgnore.IgnoreInV2("Ignored intentionally for test purposes.")]
        public void TestIgnoredInAssetPipelineV2_OnlyRunsAgainstV1Environment()
        {
            Assert.False(AssetDatabase.IsV2Enabled(), "The ignored mode (V2) was enabled.");
            Assert.True(AssetDatabase.IsV1Enabled(), "Inconsistend mode.");
        }
    }
}
