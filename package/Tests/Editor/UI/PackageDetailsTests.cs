using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageDetailsTests : UITests<PackageManagerWindow>
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
        public void Show_CorrectTag_Package_Version()
        {
            var packageInfo = PackageSets.Instance.Single();
            foreach (var tag in new List<string>
                 {
                     PackageTag.preview.ToString(),
                     PackageTag.verified.ToString(),
                     "usertag"   // Any other unsupported tag a user might use
                 })
            {
                packageInfo.IsVerified = PackageTag.verified.ToString() == tag;
                packageInfo.Version = packageInfo.Version.Change(null, null, null, tag);
                var package = new Package(packageInfo.Name, new List<PackageInfo> {packageInfo});
                var details = Container.Q<PackageDetails>("detailsGroup");
                details.SetPackage(package);

                // Check for every UI-supported tags that visibility is correct
                Assert.IsTrue(UIUtils.IsElementVisible(details.GetTag(PackageTag.preview)) == packageInfo.IsPreview);
                Assert.IsTrue(UIUtils.IsElementVisible(details.GetTag(PackageTag.verified)) == packageInfo.IsVerified);
                Assert.IsTrue(UIUtils.IsElementVisible(details.GetTag(PackageTag.git)) == packageInfo.IsGit);
                Assert.IsTrue(UIUtils.IsElementVisible(details.GetTag(PackageTag.local)) == packageInfo.IsLocal);
                Assert.IsTrue(UIUtils.IsElementVisible(details.GetTag(PackageTag.inDevelopment)) == packageInfo.IsInDevelopment);
            }
        }

        [Test]
        public void Show_CorrectTag_Package_Source()
        {
            var packageInfo = PackageSets.Instance.Single();
            foreach (var origin in new List<PackageSource>
                 {
                     PackageSource.Git,
                     PackageSource.Local,
                     PackageSource.Embedded
                 })
            {
                packageInfo.Origin = origin;
                var package = new Package(packageInfo.Name, new List<PackageInfo> {packageInfo});
                var details = Container.Q<PackageDetails>("detailsGroup");
                details.SetPackage(package);

                Assert.IsTrue(UIUtils.IsElementVisible(details.GetTag(PackageTag.preview)) == packageInfo.IsPreview);
                Assert.IsTrue(UIUtils.IsElementVisible(details.GetTag(PackageTag.git)) == packageInfo.IsGit);
                Assert.IsTrue(UIUtils.IsElementVisible(details.GetTag(PackageTag.local)) == packageInfo.IsLocal);
                Assert.IsTrue(UIUtils.IsElementVisible(details.GetTag(PackageTag.inDevelopment)) == packageInfo.IsInDevelopment);
            }
        }

        [Test]
        public void Show_CorrectLabel_UpToDate()
        {
            SetListPackages(new List<PackageInfo> {PackageSets.Instance.Single(PackageSource.Registry, "name", "1.0.0", true)});

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text.StartsWith(PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.UpToDate]));
            Assert.IsFalse(details.UpdateButton.enabledSelf);
        }

        [Test]
        public void Show_CorrectLabel_Install()
        {
            SetListPackages(new List<PackageInfo> {PackageSets.Instance.Single(PackageSource.Registry, "name", "1.0.0", false)});

            Window.Collection.SetFilter(PackageFilter.All);

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text == PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.Add]);
            Assert.IsTrue(details.UpdateButton.enabledSelf);
        }

        [Test]
        public void Show_CorrectLabel_UpdateTo()
        {
            SetListPackages(new List<PackageInfo>
            {
                PackageSets.Instance.Single(PackageSource.Registry, "name", "1.0.0", true),
                PackageSets.Instance.Single(PackageSource.Registry, "name", "2.0.0", false)
            });

            var details = Container.Q<PackageDetails>("detailsGroup");

            Assert.IsTrue(details.UpdateButton.text.StartsWith(PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.Update]));
            Assert.IsTrue(details.UpdateButton.enabledSelf);
        }

        [Test]
        public void Show_HideLabel_Embedded()
        {
            SetListPackages(new List<PackageInfo>
            {
                PackageSets.Instance.Single(PackageSource.Embedded, "name", "1.0.0", true),
                PackageSets.Instance.Single(PackageSource.Registry, "name", "2.0.0", false)
            });

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsFalse(details.UpdateBuiltIn.visible);
            Assert.IsFalse(details.UpdateButton.visible);
        }

        [Test]
        public void Show_CorrectLabel_LocalFolder()
        {
            SetListPackages(new List<PackageInfo> {PackageSets.Instance.Single(PackageSource.Local, "name", "1.0.0")});

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text == PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.UpToDate]);
            Assert.IsFalse(details.UpdateButton.enabledSelf);
        }

        [Test]
        public void Show_CorrectLabel_Git()
        {
            SetListPackages(new List<PackageInfo> {PackageSets.Instance.Single(PackageSource.Git, "name", "1.0.0")});

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text == PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.Git]);
            Assert.IsFalse(details.UpdateButton.enabledSelf);
        }

        [Test]
        public void Samples_Section_Invisible_When_No_Samples()
        {
            SetListPackages(new List<PackageInfo> {PackageSets.Instance.Single(PackageSource.Registry, "packagewithoutsample", "1.0.0")});

            var sampleList = Container.Q<PackageSampleList>("detailSampleList");
            Assert.IsFalse(sampleList.visible);
            Assert.AreEqual(sampleList.ImportStatusContainer.childCount, 0);
            Assert.AreEqual(sampleList.NameLabelContainer.childCount, 0);
            Assert.AreEqual(sampleList.SizeLabelContainer.childCount, 0);
            Assert.AreEqual(sampleList.ImportButtonContainer.childCount, 0);
        }

        [Test]
        public void Samples_Section_Visible_When_Two_Samples_Exist()
        {
            var packageWithSample = PackageSets.Instance.Single(PackageSource.Registry, "packagewithsample", "1.0.0");
            packageWithSample.Samples = new List<Sample>
            {
                new Sample("Sample A", "", "", "", false),
                new Sample("Sample B", "", "", "", false)
            };

            SetListPackages(new List<PackageInfo> { packageWithSample });

            var sampleList = Container.Q<PackageSampleList>("detailSampleList");
            Assert.IsTrue(sampleList.visible);
            Assert.AreEqual(sampleList.ImportStatusContainer.childCount, 2);
            Assert.AreEqual(sampleList.NameLabelContainer.childCount, 2);
            Assert.AreEqual(sampleList.SizeLabelContainer.childCount, 2);
            Assert.AreEqual(sampleList.ImportButtonContainer.childCount, 2);

            sampleList.Query().Children<Button>().ForEach(b => {
                Assert.IsTrue(b.enabledSelf);
            });
        }

        [Test]
        public void Samples_Import_Button_Disabled_When_Package_Not_Installed()
        {
            var packageWithSample = PackageSets.Instance.Single(PackageSource.Registry, "packagewithsamplenotinstalled", "1.0.0", false);
            packageWithSample.Samples = new List<Sample> { new Sample("Sample A", "", "", "", false), };

            Window.Collection.SetFilter(PackageFilter.All);
            SetListPackages(new List<PackageInfo> { packageWithSample });

            var sampleList = Container.Q<PackageSampleList>("detailSampleList");
            Assert.IsTrue(sampleList.visible);
            Assert.AreEqual(sampleList.ImportStatusContainer.childCount, 1);
            Assert.AreEqual(sampleList.NameLabelContainer.childCount, 1);
            Assert.AreEqual(sampleList.SizeLabelContainer.childCount, 1);
            Assert.AreEqual(sampleList.ImportButtonContainer.childCount, 1);

            sampleList.Query().Children<Button>().ForEach(b => {
                Assert.IsFalse(b.enabledSelf);
            });
        }

		[Test]
        public void Version_Not_Visible_When_Package_Core()
        {
            var packageCore = PackageSets.Instance.Single(PackageSource.BuiltIn, "core-package", "0.0.0-builtin", true);

            Window.Collection.SetFilter(PackageFilter.All);
            SetListPackages(new List<PackageInfo> { packageCore });

            var detailVersion = Container.Q<Label>("detailVersion");
            Assert.IsFalse(detailVersion.visible);
        }

		[Test]
        public void Version_Not_Visible_When_Package_Module()
        {
            var packageModule = PackageSets.Instance.Single(PackageSource.BuiltIn, "module-package", "1.1.0", true, false, PackageType.module);

            Window.Collection.SetFilter(PackageFilter.Modules);
            SetListPackages(new List<PackageInfo> { packageModule });

            var detailVersion = Container.Q<Label>("detailVersion");
            Assert.IsFalse(detailVersion.visible);
        }

		[Test]
        public void Version_Visible_When_Normal_Package()
        {
            var package = PackageSets.Instance.Single(PackageSource.Registry, "package", "1.1.0", true);

            Window.Collection.SetFilter(PackageFilter.All);
            SetListPackages(new List<PackageInfo> { package });

            var detailVersion = Container.Q<Label>("detailVersion");
            Assert.IsTrue(detailVersion.visible);
        }
    }
}
