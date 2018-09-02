using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Semver;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageTests : PackageBaseTests
    {
        private Action<IAddOperation> OnAddOperation;
        private Action<IRemoveOperation> OnRemoveOperation;

        // Package version to display
        public PackageInfo Display(Package package)
        {
            return PackageCollection.Instance.Filter == PackageFilter.All || package.Current == null ? package.Latest : package.Current;
        }
        
        [TearDown]
        public void TearDown()
        {
            Factory.ResetOperations();
            Factory.Packages = Enumerable.Empty<PackageInfo>();
        }

        [Test]
        public void Constructor_WithNullPackageName_ThrowsException()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            Assert.Throws<ArgumentException>(() => 
            {
                new Package(null, packages);
            });
        }

        [Test]
        public void Constructor_WithEmptyPackageName_ThrowsException()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            Assert.Throws<ArgumentException>(() => 
            {
                new Package("", packages);
            });
        }

        [Test]
        public void Constructor_WithNullPackageInfos_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => 
            {
                new Package(kPackageTestName, null);
            });
        }

        [Test]
        public void Constructor_WithEmptyPackageInfos_ThrowsException()
        {
            var packages = Enumerable.Empty<PackageInfo>();
            Assert.Throws<ArgumentException>(() => 
            {
                new Package(kPackageTestName, packages);
            });
        }
        
        [Test]
        public void Constructor_WithOnePackageInfo_CurrentIsFirstVersion()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(package.Current, package.Versions.First());
            Assert.IsTrue(package.Current.IsCurrent);
        }
        
        [Test]
        public void Constructor_WithOnePackageInfo_LatestIsLastVersion()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(package.Latest, package.Versions.Last());
            Assert.IsTrue(package.Latest.IsCurrent);
        }
        
        [Test]
        public void Constructor_WithOnePackageInfo_LatestAndCurrentAreEqual()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(package.Current, package.Latest);
        }
        
        [Test]
        public void Constructor_WithTwoPackageInfos_CurrentIsFirstVersion()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 2, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(package.Current, package.Versions.First());
            Assert.IsTrue(package.Current.IsCurrent);
        }
        
        [Test]
        public void Constructor_WithTwoPackageInfos_LatestIsLastVersion()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 2, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(package.Latest, package.Versions.Last());
            Assert.IsFalse(package.Latest.IsCurrent);
        }
        
        [Test]
        public void Constructor_WithTwoPackagesInfo_LatestAndCurrentAreNotEqual()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 2, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreNotEqual(package.Current, package.Latest);
        }

        [Test]
        public void Constructor_WithMultiplePackagesInfo_VersionsCorrespond()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 5, true);
            var package = new Package(kPackageTestName, packages);

            Assert.AreEqual(packages, package.Versions);
        }
        
        [Test]
        public void Add_WhenPackageInfoIsCurrent_AddOperationIsNotCalled()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 3, true);
            Factory.Packages = packages;
            var package = new Package(kPackageTestName, packages);
            var addOperationCalled = false;

            OnAddOperation = operation =>
            {
                addOperationCalled = true;
            };
            
            package.AddSignal.OnOperation += OnAddOperation;
            package.Add(packages.First());
            package.AddSignal.OnOperation -= OnAddOperation;
            
            Assert.IsFalse(addOperationCalled);
        }

        [Test]
        public void Add_WhenPackageInfoIsNotCurrent_AddOperationIsCalled()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 3, true);
            Factory.Packages = packages;
            Factory.AddOperation = new MockAddOperation(Factory, packages[1]);
            var package = new Package(kPackageTestName, packages);
            var addOperationCalled = false;

            OnAddOperation = operation =>
            {
                addOperationCalled = true;
            };
            
            package.AddSignal.OnOperation += OnAddOperation;
            package.Add(packages[1]);
            package.AddSignal.OnOperation -= OnAddOperation;
            
            Assert.IsTrue(addOperationCalled);
        }
        [Test]
        public void Update_WhenCurrentIsLatest_AddOperationIsNotCalled()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            Factory.Packages = packages;
            var package = new Package(kPackageTestName, packages);
            var addOperationCalled = false;

            OnAddOperation = operation =>
            {
                addOperationCalled = true;
            };
            
            package.AddSignal.OnOperation += OnAddOperation;
            package.Update();
            package.AddSignal.OnOperation -= OnAddOperation;
            
            Assert.IsFalse(addOperationCalled);
        }
        
        [Test]
        public void Update_WhenCurrentIsNotLatest_AddOperationIsCalled()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 2, true);
            Factory.Packages = packages;
            Factory.AddOperation = new MockAddOperation(Factory, packages.Last());
            var package = new Package(kPackageTestName, packages);
            var addOperationCalled = false;

            OnAddOperation = operation =>
            {
                addOperationCalled = true;
            };
            
            package.AddSignal.OnOperation += OnAddOperation;
            package.Update();
            package.AddSignal.OnOperation -= OnAddOperation;
            
            Assert.IsTrue(addOperationCalled);
        }
        
        [Test]
        public void Remove_RemoveOperationIsCalled()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            Factory.Packages = packages;
            var package = new Package(kPackageTestName, packages);
            var removeOperationCalled = false;

            OnRemoveOperation = operation =>
            {
                removeOperationCalled = true;
            };
            
            package.RemoveSignal.OnOperation += OnRemoveOperation;
            package.Remove();
            package.RemoveSignal.OnOperation -= OnRemoveOperation;
            
            Assert.IsTrue(removeOperationCalled);
        }

        [Test]
        public void IsPackageManagerUI_WhenPackageManagerUIPackage_ReturnsTrue()
        {
            var packages = new List<PackageInfo>
            {
                PackageSets.Instance.Single(PackageOrigin.Unknown, Package.packageManagerUIName, "1.0.0")
            };
            var package = new Package(Package.packageManagerUIName, packages);
            
            Assert.IsTrue(package.IsPackageManagerUI);
        }
        
        [Test]
        public void IsPackageManagerUI_WhenNotPackageManagerUIPackage_ReturnsFalse()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.IsFalse(package.IsPackageManagerUI);
        }
        
        [Test]
        public void Name_ReturnsExpectedValue()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(kPackageTestName, package.Name);
        }
        
        [Test]
        public void DocumentationLink_ReturnsNotEmptyString()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 1, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.IsNotEmpty(package.DocumentationLink);
        }
        
        [Test]
        public void Display_WhenCurrentIsNotNull_ReturnsCurrent()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 2, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(package.Current, Display(package));
        }
        
        [Test]
        public void Display_WhenCurrentIsNull_ReturnsLatest()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 2, true);
            packages[0].IsCurrent = false;
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(package.Latest, Display(package));
        }
        
        [Test]
        public void Display_WhenCurrentAndLatest_ReturnsLatest()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 2, true);
            packages[0].IsCurrent = false;
            var package = new Package(kPackageTestName, packages);
            var answer = packages.Max(x => x.Version);

            Assert.AreEqual(Display(package).Version, answer);
        }
        
        [Test]
        public void Versions_WhenOrderedPackageInfo_ReturnsOrderedValues()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 5, true);
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(packages, package.Versions);
        }
        
        [Test]
        public void Versions_WhenUnorderedPackageInfo_ReturnsOrderedValues()
        {
            var packages = PackageSets.Instance.Many(kPackageTestName, 5, true);
            packages[0].Version = new SemVersion(1);
            packages[1].Version = new SemVersion(4);
            packages[2].Version = new SemVersion(2);
            packages[3].Version = new SemVersion(5);
            packages[4].Version = new SemVersion(3);

            var orderPackages = packages.OrderBy(p => p.Version);
            
            var package = new Package(kPackageTestName, packages);
            
            Assert.AreEqual(orderPackages, package.Versions);
        }
    }
}