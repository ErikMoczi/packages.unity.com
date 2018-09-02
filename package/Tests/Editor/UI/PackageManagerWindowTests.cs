using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.UIElements;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageManagerWindowTests : UITests<PackageManagerWindow>
    {
        // Filter change shows correct result
        private Action<IEnumerable<Package>> onPackageChangedEvent;    // TODO: We need to have a discussion on event de-registration

        [SetUp]
        public void Setup()
        {
            PackageCollection.Instance.SetFilter(PackageFilter.Local);
            PackageCollection.Instance.UpdatePackageCollection(true);
            SetPackages(null);
            Factory.ResetOperations();
        }

        [TearDown]
        public void Dispose()
        {
            PackageCollection.Instance.OnPackagesChanged -= onPackageChangedEvent;
        }

        [Test]
        public void When_Default_FirstPackageUIElement_HasSelectedClass()
        {
            onPackageChangedEvent = packages =>
            {
                var package = Container.Query(null, "package").First();

                Assert.NotNull(package);
                Assert.IsTrue(package.ClassListContains(PackageItem.SelectedClassName));
            };
            
            PackageCollection.Instance.OnPackagesChanged += onPackageChangedEvent;
            SetPackages(PackageSets.Instance.Many(5, true));
        }

        [Test]
        public void When_Default_PackageGroupsCollapsedState()
        {
            SetPackages(PackageSets.Instance.Many(5));
            
            var packageGroups = Container.Query<PackageGroup>("groupContainerOuter").Build().ToList();
            foreach (var packageGroup in packageGroups)
            {
                var groupHeader = packageGroup.Origin;
                var children = packageGroup.Query(null, "package").Build().ToList().Count;

                if (groupHeader == PackageGroupOrigins.Packages)
                    Assert.IsTrue(packageGroup.Collapsed);           // Make sure it is not collapsed
                else if (groupHeader == PackageGroupOrigins.BuiltInPackages)
                    Assert.IsFalse(packageGroup.Collapsed);        // Make sure it is collapsed
            }
        }
        

        [Test]
        public void When_Default_PackageGroupsCollapsedState_Has_NoChildren()
        {
            SetPackages(PackageSets.Instance.Many(5));
            
            var packageGroups = Container.Query<PackageGroup>("groupContainerOuter").Build().ToList();
            foreach (var packageGroup in packageGroups)
            {
                var groupHeader = packageGroup.Origin;
                var children = packageGroup.Query(null, "package").Build().ToList().Count;

                if (groupHeader == PackageGroupOrigins.Packages)
                {
                    Assert.IsTrue(packageGroup.Collapsed);           // Make sure it is not collapsed
                    Assert.IsTrue(children > 0);           // Make sure it is not collapsed
                }
                else if (groupHeader == PackageGroupOrigins.BuiltInPackages)
                {
                    Assert.IsFalse(packageGroup.Collapsed);        // Make sure it is collapsed
                    Assert.IsTrue(children == 0);        // Make sure it is collapsed
                }
            }
        }

        [Test]
        public void When_PackageCollection_Updates_PackageList_Updates()
        {
            var packages = PackageSets.Instance.Outdated();
            var current = packages.ToList().First();
            var latest = packages.ToList().Last();

            SetPackages(packages);
            Factory.AddOperation = new MockAddOperation(Factory, latest);

            PackageCollection.Instance.SetListPackageInfos(packages);
            var package = PackageCollection.Instance.GetPackageByName(current.Name);

            onPackageChangedEvent = newpackages =>
            {
                package = PackageCollection.Instance.GetPackageByName(current.Name);

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
                    PackageCollection.Instance.OnPackagesChanged += onPackageChangedEvent;
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

            SetPackages(packages);

            var error = MakeError(ErrorCode.Unknown, "Fake error");
            Factory.AddOperation = new MockAddOperation(Factory, latest);
            Factory.AddOperation.ForceError = error;

            PackageCollection.Instance.SetListPackageInfos(packages);
            var package = PackageCollection.Instance.GetPackageByName(current.Name);

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
                    var hasOutdatedClass = state.ClassListContains(PackageItem.GetIconStateId(PackageState.Outdated));
                    Assert.IsTrue(current.Name == string.Format("com.unity.{0}", label.text));
                    Assert.IsTrue(current.Version == version.text);
                    Assert.IsTrue(hasOutdatedClass);
                };
            };

            package.Update();
        }

        [Test]
        public void When_PackageCollection_Remove_PackageLists_Updated()
        {
            var packages = PackageSets.Instance.Many(5);
            var current = packages.ToList().First();

            SetPackages(packages);
            PackageCollection.Instance.SetListPackageInfos(packages);
            var package = PackageCollection.Instance.GetPackageByName(current.Name);
            Assert.IsNotNull(package);

            PackageCollection.Instance.OnPackagesChanged += allPackages =>
            {
                package = PackageCollection.Instance.GetPackageByName(current.Name);
                Assert.IsNull(package);
            };

            package.Remove();
        }

        [Test]
        public void When_PackageCollection_Remove_Fails_PackageLists_NotUpdated()
        {
            var packages = PackageSets.Instance.Many(5);
            var current = packages.ToList().First();

            var error = MakeError(ErrorCode.Unknown, "Fake error");
            Factory.RemoveOperation = new MockRemoveOperation(Factory) {ForceError = error};
            SetPackages(packages);
            PackageCollection.Instance.SetListPackageInfos(packages);
            var package = PackageCollection.Instance.GetPackageByName(current.Name);
            Assert.IsNotNull(package);

            package.RemoveSignal.OnOperation += operation =>
            {
                operation.OnOperationError += operationError => { Assert.AreEqual(error, operationError); };
                operation.OnOperationFinalized += () =>
                {
                    package = PackageCollection.Instance.GetPackageByName(current.Name);
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
            
            Factory.SearchOperation = new MockSearchOperation(Factory, packagesAll);
            SetPackages(packagesLocal);

            onPackageChangedEvent = packages =>
            {
                foreach (var package in packagesAll)
                {
                    Assert.IsTrue(packages.Any(updatePackage => updatePackage.Current == package));
                }
            };

            PackageCollection.Instance.OnPackagesChanged += onPackageChangedEvent;
            
            PackageCollection.Instance.SetFilter(PackageFilter.All);
        }

        [Test]
        public void ListPackages_UsesCache()
        {
            var packages = PackageSets.Instance.Many(2);
            PackageCollection.Instance.SetFilter(PackageFilter.Local);                            // Set filter to use list
            Factory.SearchOperation = new MockSearchOperation(Factory, packages);
            SetPackages(packages);
            
            Assert.IsTrue(PackageCollection.Instance.HasFetchedPackageList());            // Make sure packages are cached
        }

        [Test]
        public void SearchPackages_UsesCache()
        {
            var packages = PackageSets.Instance.Many(2);
            PackageCollection.Instance.SetFilter(PackageFilter.All);                                // Set filter to use search
            Factory.SearchOperation = new MockSearchOperation(Factory, packages);
            SetPackages(packages);
            
            Assert.IsTrue(PackageCollection.Instance.HasFetchedSearchPackages());     // Make sure packages are cached
        }
    }
}
