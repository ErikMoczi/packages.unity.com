using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageCollectionTests : PackageBaseTests
    {
        private Action<PackageFilter> OnFilterChangeEvent;
        private Action<PackageFilter, IEnumerable<Package>, string> OnPackagesChangeEvent;
        private PackageCollection Collection;

        [SetUp]
        public void Setup()
        {
            Collection = new PackageCollection();
            Collection.SetFilter(PackageFilter.Local);
        }

        [TearDown]
        public void TearDown()
        {
            Collection.OnFilterChanged -= OnFilterChangeEvent;
            Collection.OnPackagesChanged -= OnPackagesChangeEvent;
            Collection = null;
        }

        [Test]
        public void Constructor_Instance_FilterIsLocal()
        {
            Assert.AreEqual(PackageFilter.Local, Collection.Filter);
        }

        [Test]
        public void SetFilter_WhenFilterChange_FilterChangeEventIsPropagated()
        {
            var wasCalled = false;
            OnFilterChangeEvent = filter =>
            {
                wasCalled = true;
            };

            Collection.OnFilterChanged += OnFilterChangeEvent;
            Collection.SetFilter(PackageFilter.All, false);
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

            Collection.OnFilterChanged += OnFilterChangeEvent;
            Collection.SetFilter(PackageFilter.Local, false);
            Assert.IsFalse(wasCalled);
        }

        [Test]
        public void SetFilter_WhenFilterChange_FilterIsChanged()
        {
            Collection.SetFilter(PackageFilter.All, false);
            Assert.AreEqual(PackageFilter.All, Collection.Filter);
        }

        [Test]
        public void SetFilter_WhenNoFilterChangeRefresh_PackagesChangeEventIsNotPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = (filter, packages, selected) =>
            {
                wasCalled = true;
            };

            Collection.OnPackagesChanged += OnPackagesChangeEvent;
            Collection.SetFilter(PackageFilter.Local);
            Assert.IsFalse(wasCalled);
        }

        [Test]
        public void SetFilter_WhenFilterChangeNoRefresh_PackagesChangeEventIsNotPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = (filter, packages, selected) =>
            {
                wasCalled = true;
            };

            Collection.OnPackagesChanged += OnPackagesChangeEvent;
            Collection.SetFilter(PackageFilter.All, false);
            Assert.IsFalse(wasCalled);
        }

        [Test]
        public void SetFilter_WhenNoFilterChangeNoRefresh_PackagesChangeEventIsNotPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = (filter, packages, selected) =>
            {
                wasCalled = true;
            };

            Collection.OnPackagesChanged += OnPackagesChangeEvent;
            Collection.SetFilter(PackageFilter.Local, false);
            Assert.IsFalse(wasCalled);
        }

        [Test]
        public void FetchListCache_PackagesChangeEventIsPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = (filter, packages, selected) =>
            {
                wasCalled = true;
            };

            Collection.OnPackagesChanged += OnPackagesChangeEvent;
            Factory.Packages = PackageSets.Instance.Many(5);
            Collection.FetchListCache(true);

            Assert.IsTrue(wasCalled);
        }


        [Test]
        public void FetchListOfflineCache_PackagesChangeEventIsPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = (filter, packages, selected) =>
            {
                wasCalled = true;
            };
            Collection.OnPackagesChanged += OnPackagesChangeEvent;

            Factory.Packages = PackageSets.Instance.Many(5);
            Collection.FetchListOfflineCache(true);

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void FetchSearchCache_PackagesChangeEventIsPropagated()
        {
            var wasCalled = false;
            OnPackagesChangeEvent = (filter, packages, selected) =>
            {
                wasCalled = true;
            };
            Collection.OnPackagesChanged += OnPackagesChangeEvent;

            Factory.SearchOperation = new MockSearchOperation(Factory, PackageSets.Instance.Many(5));
            Collection.FetchSearchCache(true);

            Assert.IsTrue(wasCalled);
        }
    }
}
