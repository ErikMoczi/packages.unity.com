using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageItemTests : UITests<PackageManagerWindow>
    {
        [SetUp]
        public void Setup()
        {
            Window.Collection.SetFilter(PackageFilter.Local);
            Window.Collection.UpdatePackageCollection(true);
            SetSearchPackages(Enumerable.Empty<PackageInfo>());
            SetListPackages(Enumerable.Empty<PackageInfo>());
            Window.SelectionManager.ClearAll();
            Factory.ResetOperations();
        }

        private void SetSearchPackages(IEnumerable<PackageInfo> packages)
        {
            Factory.SearchOperation = new MockSearchOperation(Factory, packages);
            Window.Collection.FetchSearchCache(true);
        }

        private void SetListPackages(IEnumerable<PackageInfo> packages)
        {
            Factory.Packages = packages;
            Window.Collection.FetchListCache(true);
        }


		[Test]
        public void Version_Visible_When_Package_Core()
        {
            var packageCore = PackageSets.Instance.Single(PackageSource.Registry, "core-package", "0.0.0-builtin", true);

            Window.Collection.SetFilter(PackageFilter.All);
            SetListPackages(new List<PackageInfo> { packageCore });

            var detailVersion = Container.Q<Label>("packageVersion");
            Assert.IsTrue(detailVersion.visible);
            Assert.AreEqual("0.0.0", detailVersion.text);
            var expander = Container.Q<ArrowToggle>("expander");
            Assert.IsTrue(expander.visible);
            var expanderHidden = Container.Q<Label>("expanderHidden");
            Assert.IsFalse(expanderHidden.visible);
        }

		[Test]
        public void Version_Not_Visible_When_Package_Module()
        {
            var packageModule = PackageSets.Instance.Single(PackageSource.BuiltIn, "module-package", "1.1.0", true, false, PackageType.module);

            Window.Collection.SetFilter(PackageFilter.Modules);
            SetListPackages(new List<PackageInfo> { packageModule });

            var detailVersion = Container.Q<Label>("packageVersion");
            Assert.IsFalse(detailVersion.visible);
            var expander = Container.Q<ArrowToggle>("expander");
            Assert.IsFalse(expander.visible);
            var expanderHidden = Container.Q<Label>("expanderHidden");
            Assert.IsTrue(expanderHidden.visible);
        }

		[Test]
        public void Version_Visible_When_Normal_Package()
        {
            var package = PackageSets.Instance.Single(PackageSource.Registry, "package", "1.1.0", true);

            Window.Collection.SetFilter(PackageFilter.All);
            SetListPackages(new List<PackageInfo> { package });

            var detailVersion = Container.Q<Label>("packageVersion");
            Assert.IsTrue(detailVersion.visible);
            var expander = Container.Q<ArrowToggle>("expander");
            Assert.IsTrue(expander.visible);
            var expanderHidden = Container.Q<Label>("expanderHidden");
            Assert.IsFalse(expanderHidden.visible);
        }
    }
}
