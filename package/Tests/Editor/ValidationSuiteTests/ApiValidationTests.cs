using System;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Compilation;
using UnityEngine.Assertions.Must;
using Debug = UnityEngine.Debug;

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class ApiValidationTests
    {
        internal const string testPackageRoot =
            "Packages/com.unity.package-validation-suite/Tests/Editor/ValidationSuiteTests/ApiValidationTestAssemblies/";
        [Test]
        public void AddingPropertyInPatchReleaseFails()
        {
            List<string> messagesExpected = new List<string> { "Error: Additions require a new minor or major version" };

            var apiValidation = Validate("TestPackage_PropAdd", "0.0.2");

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }
        [Test]
        public void AddingPropertyInMinorReleasePasses()
        {
            var apiValidation = Validate("TestPackage_PropAdd", "0.1.0");
            Assert.AreEqual(TestState.Succeeded, apiValidation.TestState);
        }

        [Test]
        public void AddedEmptyAssemblyInPatchReleasePasses()
        {
            var apiValidation = Validate("TestPackage_EmptyAsmdefAdd", "0.0.2");
            Assert.AreEqual(TestState.Succeeded, apiValidation.TestState);
        }

        [Test]
        public void AddingTypeInNewAssemblyInPatchReleaseFails()
        {
            List<string> messagesExpected = new List<string> { "Error: Additions require a new minor or major version" };

            var apiValidation = Validate("TestPackage_PropAdd", "0.0.2");

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }

        [Test]
        public void AddIncludePlatformInPatchReleaseFails()
        {
            List<string> messagesExpected = new List<string> { "Error: Adding to includePlatforms requires a new minor or major version. Was:\"\" Now:\"Editor\"" };

            var apiValidation = Validate("TestPackage_WithIncludePlatform", "0.0.2");

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }
        [Test]
        public void AddIncludePlatformInMinorReleasePasses()
        {
            var apiValidation = Validate("TestPackage_WithIncludePlatform", "0.1.0");
            Assert.AreEqual(TestState.Succeeded, apiValidation.TestState);
        }

        [Test]
        public void AddExcludePlatformInMinorReleaseFails()
        {
            List<string> messagesExpected = new List<string> { "Error: Adding to excludePlatforms requires a new major version. Was:\"\" Now:\"Android\"" };

            var apiValidation = Validate("TestPackage_WithExcludePlatform", "0.1.0");

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }
        [Test]
        public void AddExcludePlatformInMajorReleasePasses()
        {
            var apiValidation = Validate("TestPackage_WithExcludePlatform", "1.0.0");
            Assert.AreEqual(TestState.Succeeded, apiValidation.TestState);
        }

        [Test]
        public void RemoveExcludePlatformInPatchReleaseFails()
        {
            List<string> messagesExpected = new List<string> { "Error: Removing from excludePlatfoms requires a new minor or major version. Was:\"Android\" Now:\"\"" };

            var apiValidation = Validate("TestPackage_Base", "0.0.2", "TestPackage_WithExcludePlatform");

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }
        [Test]
        public void RemoveExcludePlatformInMinorReleasePasses()
        {
            var apiValidation = Validate("TestPackage_Base", "0.1.0", "TestPackage_WithExcludePlatform");
            Assert.AreEqual(TestState.Succeeded, apiValidation.TestState);
        }

        [Test]
        public void RemoveIncludePlatformInMinorReleaseFails()
        {
            List<string> messagesExpected = new List<string> { "Error: Removing from includePlatfoms requires a new major version. Was:\"Editor\" Now:\"\"" };

            var apiValidation = Validate("TestPackage_Base", "0.1.0", "TestPackage_WithIncludePlatform");

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }

        [Test]
        public void RemoveIncludePlatformInMajorReleasePasses()
        {
            var apiValidation = Validate("TestPackage_Base", "1.0.0", "TestPackage_WithIncludePlatform");
            Assert.AreEqual(TestState.Succeeded, apiValidation.TestState);
        }

        [Test]
        public void BreakingChangeInMinorReleaseFails()
        {
            List<string> messagesExpected = new List<string> { "Error: Breaking changes require a new major version" };

            var apiValidation = Validate("TestPackage_BreakingChange", "0.1.0");

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }

        [Test]
        public void ChangingAsmdefToTestInMinorReleaseFails()
        {
            List<string> messagesExpected = new List<string> { "Error: Assembly \"Unity.PackageValidationSuite.EditorTests.TestPackage_Base.dll\" no longer exists or is no longer included in build. This requires a new major version." };

            var apiValidation = Validate("TestPackage_Base", "0.1.0", isPreviousPackageTest:false, isProjectPackageTest:true);

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }

        [Test]
        public void RemovingAsmdefInMinorReleaseFails()
        {
            List<string> messagesExpected = new List<string> { "Error: Assembly \"Unity.PackageValidationSuite.EditorTests.TestPackage_Base.NewAsmdef.dll\" no longer exists or is no longer included in build. This requires a new major version." };

            var apiValidation = Validate("TestPackage_Base", "0.1.0", "TestPackage_AsmdefWithTypeAdd");

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }

        [Test]
        public void RemovingAsmdefInMajorReleaseSucceeds()
        {
            var apiValidation = Validate("TestPackage_Base", "1.0.0", "TestPackage_AsmdefWithTypeAdd");
            Assert.AreEqual(TestState.Succeeded, apiValidation.TestState);
        }

        [Test]
        public void ExcludingEditorPlatformFailsDueToLackOfSupport()
        {
            List<string> messagesExpected = new List<string> { "Error: Package Validation Suite does not support .asmdefs that are not built on the \"Editor\" platform. See \"Unity.PackageValidationSuite.EditorTests.TestPackage_ExcludesEditor\"" };

            var apiValidation = Validate("TestPackage_ExcludesEditor", "0.1.0", "TestPackage_ExcludesEditor", copyAssemblies:false);

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }

        [Test]
        public void IncludingNonEditorPlatformFailsDueToLackOfSupport()
        {
            List<string> messagesExpected = new List<string> { "Error: Package Validation Suite does not support .asmdefs that are not built on the \"Editor\" platform. See \"Unity.PackageValidationSuite.EditorTests.TestPackage_IncludesAndroid\"" };

            var apiValidation = Validate("TestPackage_IncludesAndroid", "0.1.0", "TestPackage_IncludesAndroid", copyAssemblies: false);

            Assert.AreEqual(TestState.Failed, apiValidation.TestState);
            Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
        }
        //need to make decisions on dependencies and write more tests

        private static ApiValidation Validate(string projectPackageName, string projectPackageVersion,
            string previousPackageName = "TestPackage_Base", string previousPackageVersion = "0.0.1", 
            bool isPreviousPackageTest = false, bool isProjectPackageTest = false, bool copyAssemblies = true)
        {
            var previousPackagePath = Path.GetFullPath(testPackageRoot + previousPackageName);
            var projectPackagePath = Path.GetFullPath(testPackageRoot + projectPackageName);
            var assemblies = CompilationPipeline.GetAssemblies();

            if (Directory.Exists(VettingContext.PreviousVersionBinaryPath))
                Directory.Delete(VettingContext.PreviousVersionBinaryPath, true);

            Directory.CreateDirectory(VettingContext.PreviousVersionBinaryPath);

            if (copyAssemblies)
            {
                var assemblyNamesPrevious = GetAssemblyNames(previousPackagePath).ToArray();
                var assemblyNamesProject = GetAssemblyNames(projectPackagePath).ToArray();
                var assemblyPrevious = assemblies.Where(a => assemblyNamesPrevious.Any(a.name.Contains));
                var assemblyProject = assemblies.Where(a => assemblyNamesProject.Any(a.name.Contains));

                Assert.That(assemblyPrevious.Any());
                Assert.That(assemblyProject.Any());
                foreach (var assembly in assemblyPrevious)
                {
                    File.Copy(assembly.outputPath,
                        Path.Combine(VettingContext.PreviousVersionBinaryPath, assembly.name.Replace(previousPackageName, projectPackageName) + ".dll"));
                }
            }

            var apiValidationAssemblyInformation = new ApiValidationAssemblyInformation(isPreviousPackageTest, isProjectPackageTest, "TestPackage", "TestPackage");
            var apiValidation = new ApiValidation(apiValidationAssemblyInformation)
            {
                Context = new VettingContext()
                {
                    PreviousPackageInfo = new VettingContext.ManifestData()
                    {
                        path = previousPackagePath,
                        version = previousPackageVersion
                    },
                    ProjectPackageInfo = new VettingContext.ManifestData()
                    {
                        path = projectPackagePath,
                        version = projectPackageVersion
                    },
                    PreviousPackageBinaryDirectory = VettingContext.PreviousVersionBinaryPath
                }
            };
            apiValidation.Setup();
            apiValidation.RunTest();

            apiValidation.TestOutput.ForEach(Debug.Log);
            return apiValidation;
        }

        private static IEnumerable<string> GetAssemblyNames(string previousPackagePath)
        {
            foreach (var asmdefPath in Directory.GetFiles(previousPackagePath, "*.asmdef", SearchOption.AllDirectories))
            {
                var asmdef = Utilities.GetDataFromJson<AssemblyDefinition>(asmdefPath);
                yield return asmdef.name;
            }
        }
    }
}
#endif