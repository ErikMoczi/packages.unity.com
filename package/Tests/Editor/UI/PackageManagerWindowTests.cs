using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.UIElements;
using NUnit.Framework;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageManagerWindowTests : UITests<PackageManagerWindow>
    {
        // Filter change shows correct result
        private Action<PackageFilter, IEnumerable<Package>, string> onPackageChangedEvent;    // TODO: We need to have a discussion on event de-registration

        [SetUp]
        public void Setup()
        {
            Window.Collection.SetFilter(PackageFilter.Local);
            SetListPackages(Enumerable.Empty<PackageInfo>());
            SetSearchPackages(Enumerable.Empty<PackageInfo>());
            Factory.ResetOperations();
        }

        [TearDown]
        public void Dispose()
        {
            Window.Collection.OnPackagesChanged -= onPackageChangedEvent;
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
        public void When_Default_FirstPackageUIElement_HasSelectedClass()
        {
            onPackageChangedEvent = (filter, packages, selected) =>
            {
                var package = Container.Query(null, "package").First();

                Assert.NotNull(package);
                Assert.IsTrue(package.ClassListContains(PackageItem.SelectedClassName));
            };
            
            Window.Collection.OnPackagesChanged += onPackageChangedEvent;
            SetListPackages(PackageSets.Instance.Many(5, true));
        }

        [Test]
        public void When_PackageCollection_Updates_PackageList_Updates()
        {
            var packages = PackageSets.Instance.Outdated();
            var current = packages.ToList().First();
            var latest = packages.ToList().Last();

            SetListPackages(packages);
            Factory.AddOperation = new MockAddOperation(Factory, latest);

            var package = Window.Collection.GetPackageByName(current.Name);

            onPackageChangedEvent = (filter, newpackages, selected) =>
            {
                package = Window.Collection.GetPackageByName(current.Name);

                Assert.IsTrue(package.Current.PackageId == latest.PackageId);

                var packageItem = Container.Query(null, "package").Build().First();
                var label = packageItem.Q<Label>("packageName");
                var version = packageItem.Q<Label>("packageVersion");
                var state = packageItem.Q<Label>("packageState");
                var hasOutdatedClass = state.ClassListContains(PackageItem.GetIconStateId(PackageState.Outdated));
                Assert.IsTrue(latest.Name == string.Format("com.unity.{0}", label.text));
                Assert.IsTrue(latest.Version == version.text);
                Assert.IsFalse(hasOutdatedClass);
            };
            
            package.AddSignal.OnOperation += operation =>
            {
                operation.OnOperationSuccess += packageInfo =>
                {
                    Window.Collection.OnPackagesChanged += onPackageChangedEvent;
                };
            };

            package.Update();
        }

        [Test]
        public void When_PackageCollection_Update_Fails_Package_Stay_Current()
        {
            var packages = PackageSets.Instance.Outdated();
            var current = packages.ToList().First();
            var latest = packages.ToList().Last();

            SetListPackages(packages);

            var error = MakeError(ErrorCode.Unknown, "Fake error");
            Factory.AddOperation = new MockAddOperation(Factory, latest);
            Factory.AddOperation.ForceError = error;

            var package = Window.Collection.GetPackageByName(current.Name);

            package.AddSignal.OnOperation += operation =>
            {
                operation.OnOperationError += operationError => { Assert.IsTrue(error == operationError); };
                operation.OnOperationFinalized += () =>
                {
                    Assert.IsTrue(package.Current.PackageId ==
                                  current.PackageId); // Make sure package hasn't been upgraded

                    var packageItem = Container.Query(null, "package").Build().First();
                    var label = packageItem.Q<Label>("packageName");
                    var version = packageItem.Q<Label>("packageVersion");
                    var state = packageItem.Q<Label>("packageState");
                    var hasErrorClass = state.ClassListContains(PackageItem.GetIconStateId(PackageState.Error));
                    Assert.IsTrue(current.Name == string.Format("com.unity.{0}", label.text));
                    Assert.IsTrue(current.Version == version.text);
                    Assert.IsTrue(hasErrorClass);
                };
            };

            package.Update();
        }

        [Test]
        public void When_PackageCollection_Remove_PackageLists_Updated()
        {
            var packages = PackageSets.Instance.Many(5);
            var current = packages.ToList().First();

            SetListPackages(packages);
            var package = Window.Collection.GetPackageByName(current.Name);
            Assert.IsNotNull(package);

            onPackageChangedEvent = (filter, allPackages, selected) =>
            {
                package = Window.Collection.GetPackageByName(current.Name);
                Assert.IsNull(package);
            };

            Window.Collection.OnPackagesChanged += onPackageChangedEvent;

            package.Remove();
            Window.Collection.FetchListOfflineCache(true);
        }

        [Test]
        public void When_PackageCollection_Remove_Fails_PackageLists_NotUpdated()
        {
            var packages = PackageSets.Instance.Many(5);
            var current = packages.ToList().First();

            var error = MakeError(ErrorCode.Unknown, "Fake error");
            Factory.RemoveOperation = new MockRemoveOperation(Factory) {ForceError = error};
            SetListPackages(packages);
            var package = Window.Collection.GetPackageByName(current.Name);
            Assert.IsNotNull(package);

            package.RemoveSignal.OnOperation += operation =>
            {
                operation.OnOperationError += operationError => { Assert.AreEqual(error, operationError); };
                operation.OnOperationFinalized += () =>
                {
                    package = Window.Collection.GetPackageByName(current.Name);
                    Assert.IsNotNull(package);
                };
            };

            package.Remove();
        }
        
        [Test] 
        public void When_Filter_Changes_Shows_Correct_List()
        {
            var packagesLocal = PackageSets.Instance.Many(2);
            var packagesAll = PackageSets.Instance.Many(5);

            SetListPackages(packagesLocal);
            SetSearchPackages(packagesAll);

            onPackageChangedEvent = (filter, packages, selected) =>
            {
                foreach (var package in packagesAll)
                {
                    Assert.IsTrue(packages.Any(updatePackage => updatePackage.Current == package));
                }
            };

            Window.Collection.OnPackagesChanged += onPackageChangedEvent;
            
            Window.Collection.SetFilter(PackageFilter.All);
        }

        [Test]
        public void ListPackages_UsesCache()
        {
            Window.Collection.SetFilter(PackageFilter.Local);                            // Set filter to use list
            SetListPackages(PackageSets.Instance.Many(2));
            
            Assert.IsTrue(Window.Collection.LatestListPackages.Any());            // Make sure packages are cached
        }

        [Test]
        public void SearchPackages_UsesCache()
        {
            Window.Collection.SetFilter(PackageFilter.All);                                // Set filter to use search
            SetSearchPackages(PackageSets.Instance.Many(2));
            
            Assert.IsTrue(Window.Collection.LatestSearchPackages.Any());     // Make sure packages are cached
        }
    }
}
