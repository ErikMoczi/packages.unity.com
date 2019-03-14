#if UNITY_2018_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    class PackageBinaryZippingTests
    {
        [Test]
        public void ZipPackageBinariesCreatesCorrectZip()
        {
            var testFolder = Path.Combine(Path.GetTempPath(), "ZipPackageBinariesCreatesCorrectZip");
            if (Directory.Exists(testFolder))
                Directory.Delete(testFolder, true);

            Directory.CreateDirectory(testFolder);
            string zipFilePath;

            var projectPath = Path.GetDirectoryName(Application.dataPath);
            var packageRootPath = Path.Combine(projectPath, ApiValidationTests.testPackageRoot + "AsmdefWithTypeAdd");
            Assert.IsTrue(PackageBinaryZipping.TryZipPackageBinaries(packageRootPath, "AsmdefWithTypeAdd", "0.1.0", testFolder, out zipFilePath), "ZipPackageBinaries failed");

            var destPath = Path.Combine(testFolder, "zipContents");
            var expectedPaths = new[]
            {
                Path.Combine(destPath, "Unity.PackageValidationSuite.EditorTests.AsmdefWithTypeAdd.dll"),
                Path.Combine(destPath, "Unity.PackageValidationSuite.EditorTests.AsmdefWithTypeAdd.NewAsmdef.dll")
            };
            Assert.IsTrue(PackageBinaryZipping.Unzip(zipFilePath, destPath), "Unzip failed");
            var actualPaths = Directory.GetFiles(destPath, "*", SearchOption.AllDirectories);
            Assert.That(actualPaths, Is.EquivalentTo(expectedPaths));
        }

        [Test]
        public void ZipPackageBinariesCreatesEmptyZipOnEmptyPackage()
        {
            var testFolder = Path.Combine(Path.GetTempPath(), "ZipPackageBinariesCreatesEmptyZipOnEmptyPackage");
            if (Directory.Exists(testFolder))
                Directory.Delete(testFolder, true);

            Directory.CreateDirectory(testFolder);
            string zipFilePath;
            Assert.IsTrue(PackageBinaryZipping.TryZipPackageBinaries(testFolder, "TestPackage_Empty", "0.1.0", testFolder, out zipFilePath), "ZipPackageBinaries failed");

            var destPath = Path.Combine(testFolder, "zipContents");
            Assert.IsTrue(PackageBinaryZipping.Unzip(zipFilePath, destPath), "Unzip failed");
            Assert.IsFalse(Directory.Exists(destPath));
        }
    }
}
#endif
