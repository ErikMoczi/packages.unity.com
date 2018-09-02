using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.UIElements;
using NUnit.Framework;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageDetailsTests : UITests<PackageManagerWindow>
    {
        [SetUp]
        public void Setup()
        {
            PackageCollection.Instance.SetFilter(PackageFilter.Local);
            PackageCollection.Instance.UpdatePackageCollection(true);
            SetPackages(null);
            Factory.ResetOperations();
        }

        [Test]
        public void Show_CorrectTag()
        {
            var packageInfo = PackageSets.Instance.Single();
            foreach (var tag in new List<string>
            {
                PackageTag.preview.ToString(),
                PackageTag.verified.ToString(),
                "usertag"        // Any other unsupported tag a user might use
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
            }
        }

        [Test]
        public void Show_CorrectLabel_UpToDate()
        {
            SetPackages(new List<PackageInfo> {PackageSets.Instance.Single(PackageSource.Registry, "name", "1.0.0", true)});

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text == PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.UpToDate]);
            Assert.IsFalse(details.UpdateButton.enabledSelf);
            Assert.IsTrue(details.VersionPopup.enabledSelf);
        }

        [Test]
        public void Show_CorrectLabel_Install()
        {
            SetPackages(new List<PackageInfo> {PackageSets.Instance.Single(PackageSource.Registry, "name", "1.0.0", false)});

            PackageCollection.Instance.SetFilter(PackageFilter.All);

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text == PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.Add]);
            Assert.IsTrue(details.UpdateButton.enabledSelf);
            Assert.IsTrue(details.VersionPopup.enabledSelf);
        }

        [Test]
        public void Show_CorrectLabel_UpdateTo()
        {
            SetPackages(new List<PackageInfo> 
            {
                PackageSets.Instance.Single(PackageSource.Registry, "name", "1.0.0", true),
                PackageSets.Instance.Single(PackageSource.Registry, "name", "2.0.0", false)
            });

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text == PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.Update]);
            Assert.IsTrue(details.UpdateButton.enabledSelf);
            Assert.IsTrue(details.VersionPopup.enabledSelf);
        }
        
        [Test]
        public void Show_CorrectLabel_Embedded()
        {
            SetPackages(new List<PackageInfo> 
            {
                PackageSets.Instance.Single(PackageSource.Embedded, "name", "1.0.0", true),
                PackageSets.Instance.Single(PackageSource.Registry, "name", "2.0.0", false)
            });

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text == PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.Embedded]);
            Assert.IsFalse(details.UpdateButton.enabledSelf);
            Assert.IsFalse(details.VersionPopup.enabledSelf);
        }
        
        [Test]
        public void Show_CorrectLabel_LocalFolder()
        {
            SetPackages(new List<PackageInfo> {PackageSets.Instance.Single(PackageSource.Local, "name", "1.0.0")});

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text == PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.Local]);
            Assert.IsFalse(details.UpdateButton.enabledSelf);
            Assert.False(details.VersionPopup.enabledSelf);
        }
        
        [Test]
        public void Show_CorrectLabel_Git()
        {
            SetPackages(new List<PackageInfo> {PackageSets.Instance.Single(PackageSource.Git, "name", "1.0.0")});

            var details = Container.Q<PackageDetails>("detailsGroup");
            Assert.IsTrue(details.UpdateButton.text == PackageDetails.PackageActionVerbs[(int)PackageDetails.PackageAction.Git]);
            Assert.IsFalse(details.UpdateButton.enabledSelf);
            Assert.IsFalse(details.VersionPopup.enabledSelf);
        }
        
    }
}
