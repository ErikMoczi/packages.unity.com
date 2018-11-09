using NUnit.Framework;
using Semver;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageInfoTests : PackageBaseTests
    {
        [Test]
        public void HasTag_WhenPreReleasePackageVersionTagWithPreReleaseName_ReturnsTrue()
        {
            var tag = PackageTag.preview.ToString();

            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, tag)
            };

            Assert.IsTrue(info.HasVersionTag(tag));
        }

        [Test]
        public void HasTag_WhenPackageVersionTagIsAnyCase_ReturnsTrue()
        {
            var tag = "pREview";

            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, tag)
            };

            Assert.IsTrue(info.HasVersionTag(tag));
        }
        
        [Test]
        public void HasTag_WhenPackageVersionTagIsNotWhatIsAsked_ReturnsFalse()
        {
            var tag = "builtin";

            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, tag)
            };

            Assert.IsFalse(info.HasVersionTag(PackageTag.preview));
        }
        
        [Test]
        public void HasTag_WhenPackageVersionTagHasNoTag_ReturnsFalse()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0)
            };

            Assert.IsFalse(info.HasVersionTag(PackageTag.builtin));
        }

        [Test]
        public void VersionWithoutTag_WhenVersionContainsTag_ReturnsVersionOnly()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1, 0, 0, PackageTag.preview.ToString())
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
        public void IsBuiltIn_WhenPackageIsModule_ReturnsTrue()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1),
                Origin = PackageSource.BuiltIn,
                Type = PackageType.module.ToString()
            };

            Assert.IsTrue(info.IsBuiltIn);
        }
        
        [Test]
        public void IsBuiltIn_WhenPackageIsCore_ReturnsFalse()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(0,0,0,PackageTag.builtin.ToString()),
                Origin = PackageSource.BuiltIn
            };

            Assert.IsFalse(info.IsBuiltIn);
        }
        
        [Test]
        public void IsCore_WhenPackageIsCore_ReturnsTrue()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(0,0,0,PackageTag.builtin.ToString()),
                Origin = PackageSource.BuiltIn
            };

            Assert.IsTrue(info.IsCore);
        }
        
        [Test]
        public void IsCore_WhenPackageIsModule_ReturnsFalse()
        {
            var info = new PackageInfo()
            {
                PackageId = kPackageTestName,
                Version = new SemVersion(1),
                Origin = PackageSource.BuiltIn,
                Type = PackageType.module.ToString()
            };

            Assert.IsFalse(info.IsCore);
        }
    }
}
