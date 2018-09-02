using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Semver;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageCollectionTests : PackageBaseTests
    {
        private Action<PackageFilter> OnFilterChangeEvent;
        private Action<IEnumerable<Package>> OnPackagesChangeEvent;

        [SetUp]
        public void Setup()
        {
            PackageCollection.Instance.SetFilter(PackageFilter.Local);
        }

        [TearDown]
        public void TearDown()
        {
            PackageCollection.Instance.OnFilterChanged -= OnFilterChangeEvent;
            PackageCollection.Instance.OnPackagesChanged -= OnPackagesChangeEvent;
        }

        [Test]
        public void Constructor_Instance_FilterIsLocal()
        {
            Assert.AreEqual(PackageFilter.Local, PackageCollection.Instance.Filter);
        }

        [Test]
        public void Constructor_Instance_PackageInfosIsEmpty()
        {
            Assert.IsEmpty(PackageCollection.Instance.PackageInfos);
        }

        [Test]
        public void SetFilter_WhenFilterChange_FilterChangeEventIsPropagated()
        {
            var wasCalled = false;
            OnFilterChangeEvent = filter =>
            {
                wasCalled = true;
            };

            PackageCollection.Instance.OnFilterChanged += OnFilterChangeEvent;
            PackageCollection.Instance.SetFilter(PackageFilter.All, false);
            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void SetFilter_WhenNoFilterChange_FilterChangeEventIsNotPropagated()
        {
            var wasCalled = false;
            OnFilterChangeEvent = filter =>
            {
                wasCalled = true;
            };

            PackageCollection.Instance.OnFilterChanged += OnFilterChangeEvent;
            PackageCollection.Instance.SetFilter(PackageFilter.Local, false);
            Assert.IsFalse(wasCalled);
        }

        [Test]
        public void SetFilter_WhenFilterChange_FilterIsChanged()
        {
            PackageCollection.Instance.SetFilter(PackageFilter.All, false);
            Assert.AreEqual(PackageFilter.All, PackageCollection.Instance.Filter);
        }

        [Test]
        public void SetFilter_WhenNoFilterChangeRefresh_PackagesChangeEventIsNotPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = packages =>
            {
                wasCalled = true;
            };

            PackageCollection.Instance.OnPackagesChanged += OnPackagesChangeEvent;
            PackageCollection.Instance.SetFilter(PackageFilter.Local);
            Assert.IsFalse(wasCalled);
        }

        [Test]
        public void SetFilter_WhenFilterChangeNoRefresh_PackagesChangeEventIsNotPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = packages =>
            {
                wasCalled = true;
            };

            PackageCollection.Instance.OnPackagesChanged += OnPackagesChangeEvent;
            PackageCollection.Instance.SetFilter(PackageFilter.All, false);
            Assert.IsFalse(wasCalled);
        }

        [Test]
        public void SetFilter_WhenNoFilterChangeNoRefresh_PackagesChangeEventIsNotPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = packages =>
            {
                wasCalled = true;
            };

            PackageCollection.Instance.OnPackagesChanged += OnPackagesChangeEvent;
            PackageCollection.Instance.SetFilter(PackageFilter.Local, false);
            Assert.IsFalse(wasCalled);
        }

        [Test]
        public void SetPackageInfos_PackagesChangeEventIsPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = packages =>
            {
                wasCalled = true;
            };

            PackageCollection.Instance.OnPackagesChanged += OnPackagesChangeEvent;
            PackageCollection.Instance.SetListPackageInfos(Enumerable.Empty<PackageInfo>());
            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void AddPackageInfos_PackagesChangeEventIsPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = packages =>
            {
                wasCalled = true;
            };

            PackageCollection.Instance.OnPackagesChanged += OnPackagesChangeEvent;
            PackageCollection.Instance.SetListPackageInfos(Enumerable.Empty<PackageInfo>());
            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void AddPackageInfo_PackagesChangeEventIsPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = packages =>
            {
                wasCalled = true;
            };

            PackageCollection.Instance.OnPackagesChanged += OnPackagesChangeEvent;
            var info = new PackageInfo() {
              Name = kPackageTestName,
              Version = new SemVersion(1,0,0),
              IsCurrent = true,
              Group = "Test",
              Errors = new List<Error>()
            };
            PackageCollection.Instance.AddPackageInfo(info);
            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void ClearPackages_PackagesChangeEventIsPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = packages =>
            {
                wasCalled = true;
            };

            PackageCollection.Instance.OnPackagesChanged += OnPackagesChangeEvent;
            PackageCollection.Instance.ClearPackages();
            Assert.IsTrue(wasCalled);
        }
    }
}
