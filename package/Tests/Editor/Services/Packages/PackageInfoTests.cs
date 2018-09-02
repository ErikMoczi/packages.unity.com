using NUnit.Framework;
using Semver;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageInfoTests : PackageBaseTests
    {
        [Test]
        public void IsInPreview_WhenPreviewPackageVersionTagIsPreviewLowerCase_ReturnsTrue()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, "preview")
            };
            
            Assert.IsTrue(info.IsInPreview);
        }
        
        [Test]
        public void IsInPreview_WhenPreviewPackageVersionTagIsPreviewUpperCase_ReturnsTrue()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, "PREVIEW")
            };
            
            Assert.IsTrue(info.IsInPreview);
        }

        [Test]
        public void IsInPreview_WhenPreviewPackageVersionTagIsNotPreview_ReturnsFalse()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, "release")
            };
            
            Assert.IsFalse(info.IsInPreview);
        }

        [Test]
        public void IsInPreview_WhenPackageVersionMajorIsZero_ReturnsTrue()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(0)
            };
            
            Assert.IsTrue(info.IsInPreview);
        }
        
        [Test]
        public void IsInPreview_WhenPackageVersionMajorIsGreaterThanZero_ReturnsFalse()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1)
            };
            
            Assert.IsFalse(info.IsInPreview);
        }
        
        [Test]
        public void VersionWithoutTag_WhenVersionContainsPreviewTag_ReturnsVersionOnly()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, "preview")
            };
            
            Assert.AreEqual("1.0.0", info.VersionWithoutTag);
        }
        
        [Test]
        public void VersionWithoutTag_WhenVersionContainsOtherTag_ReturnsVersionOnly()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, "release")
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
    }
}