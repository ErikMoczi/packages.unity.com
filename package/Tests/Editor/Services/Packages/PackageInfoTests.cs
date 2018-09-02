using NUnit.Framework;
using Semver;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageInfoTests : PackageBaseTests
    {
        [Test]
        public void HasTag_WhenPreReleasePackageVersionTagWithPreReleaseName_ReturnsTrue()
        {
            var tag = PackageTag.alpha.ToString();
            
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, tag)
            };
            
            Assert.IsTrue(info.HasTag(tag));
        }
        
        [Test]
        public void HasTag_WhenPackageVersionTagIsAnyCase_ReturnsTrue()
        {
            var tag = "bEtA";
            
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, tag)
            };
            
            Assert.IsTrue(info.HasTag(tag));
        }
        
        [Test]
        public void VersionWithoutTag_WhenVersionContainsTag_ReturnsVersionOnly()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, PackageTag.alpha.ToString())
            };
            
            Assert.AreEqual("1.0.0", info.VersionWithoutTag);
        }
        
        [Test]
        public void VersionWithoutTag_WhenVersionDoesNotContainTag_ReturnsVersionOnly()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1)
            };
            
            Assert.AreEqual("1.0.0", info.VersionWithoutTag);
        }

        [Test]
        public void PackageFromUpm_GivesCorrectRecommendedTag()
        {
            Assert.IsTrue(UpmBaseOperation.GetIsRecommended("0.0.1", "0.0.1", true));
            Assert.IsTrue(UpmBaseOperation.GetIsRecommended("0.0.2", "0.0.2", false));
            Assert.IsTrue(UpmBaseOperation.GetIsRecommended("0.0.2", "0.0.1", true));    // Higher patch version are recommended
            Assert.IsFalse(UpmBaseOperation.GetIsRecommended("1.0.2", "0.0.1", true));    // But not higher major/minor
            Assert.IsFalse(UpmBaseOperation.GetIsRecommended("0.1.2", "0.0.1", true));    //
            Assert.IsFalse(UpmBaseOperation.GetIsRecommended("0.1.0", "0.0.1", true));    //
            Assert.IsFalse(UpmBaseOperation.GetIsRecommended("1.0.0", "0.0.1", true));    //
            
            Assert.IsFalse(UpmBaseOperation.GetIsRecommended("0.0.2-alpha", "0.0.1", true));
            Assert.IsFalse(UpmBaseOperation.GetIsRecommended("0.0.2-beta", "0.0.1", true));
            Assert.IsFalse(UpmBaseOperation.GetIsRecommended("0.0.2-experimental", "0.0.1", true));
        }
    }
}