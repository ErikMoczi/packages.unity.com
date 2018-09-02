using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.UIElements;
using NUnit.Framework;
using UnityEditor.Experimental.Build;

namespace UnityEditor.PackageManager.UI.Tests
{
    internal class PackageDetailsTests : UITests<PackageManagerWindow>
    {
        [SetUp]
        public void Setup()
        {
            PackageCollection.Instance.Reset();
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
                details.SetPackage(package, PackageFilter.Local);

                // Check for every UI-supported tags that visibility is correct
                foreach (var itemTag in PackageDetails.SupportedTags())
                {
                    var uiItem = details.GetTag(itemTag);
                    if (tag == itemTag.ToString()) 
                        Assert.IsTrue(UIUtils.IsElementVisible(uiItem));
                    else 
                        Assert.IsFalse(UIUtils.IsElementVisible(uiItem));
                }
            }
        }

        [Test]
        public void Show_CorrectPackage()
        {
            var packageInfos = PackageSets.Instance.Outdated();
            SetPackages(packageInfos);

            var package = PackageCollection.Instance.GetPackageByName(packageInfos.First().Name);
                
            var details = Container.Q<PackageDetails>("detailsGroup");
            PackageCollection.Instance.SetFilter(PackageFilter.Local);
            Assert.IsTrue(details.Display(package) == packageInfos[0]);

            PackageCollection.Instance.SetFilter(PackageFilter.All);
            Assert.IsTrue(details.Display(package) == packageInfos[1]);
        }
    }
}