using NUnit.Framework;

namespace UnityEditor.AddressableAssets.Tests
{
    public class AssetGroupTests : AddressableAssetTestBase
    {
        [Test]
        public void AddRemoveEntry()
        {
            var group = m_settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var entry = new AddressableAssetEntry(assetGUID, "test", group, false);
            group.AddAssetEntry(entry);
            Assert.IsNotNull(group.GetAssetEntry(assetGUID));
            group.RemoveAssetEntry(entry);
            Assert.IsNull(group.GetAssetEntry(assetGUID));
        }
    }
}