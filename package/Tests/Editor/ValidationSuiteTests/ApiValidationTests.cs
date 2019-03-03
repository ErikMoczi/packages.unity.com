using NUnit.Framework;
using System.IO;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using System.Collections.Generic;
using System.Linq;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Compilation;
using Debug = UnityEngine.Debug;

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    internal class ApiValidationTests
    {
        internal const string testPackageRoot =
            "Packages/com.unity.package-validation-suite/Tests/Editor/ValidationSuiteTests/ApiValidationTestAssemblies/";
        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInPatch")]
        public void AddingProperty_FailsInPatch(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Additions require a new minor or major version." };
            var apiValidation = Validate("TestPackage_PropAdd", releaseType);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInPatch")]
        public void AddingInternalOnlyAssembly_FailsInPatch(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: New assembly \"Unity.PackageValidationSuite.EditorTests.TestPackage_InternalOnlyAsmdefAdd.NewAsmdef\" may only be added in a new minor or major version." };
            var apiValidation = Validate("TestPackage_InternalOnlyAsmdefAdd", releaseType);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInPatch")]
        public void AddingTypeInNewAssembly_FailsInPatch(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Additions require a new minor or major version." };

            var apiValidation = Validate("TestPackage_PropAdd", releaseType);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInPatch")]
        public void AddIncludePlatform_FailsInPatch(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Adding to includePlatforms requires a new minor or major version. Was:\"Editor\" Now:\"Editor, Android\"" };
            var apiValidation = Validate("TestPackage_WithTwoIncludePlatforms", releaseType, "TestPackage_WithIncludePlatform");
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void AddFirstIncludePlatform_FailsInMinor(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Adding the first entry in inlcudePlatforms requires a new major version. Was:\"\" Now:\"Editor\"" };
            var apiValidation = Validate("TestPackage_WithIncludePlatform", releaseType);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void AddExcludePlatform_FailsInMinor(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Adding to excludePlatforms requires a new major version. Was:\"\" Now:\"Android\"" };
            var apiValidation = Validate("TestPackage_WithExcludePlatform", releaseType);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInPatch")]
        public void RemoveExcludePlatform_FailsInPatch(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Removing from excludePlatfoms requires a new minor or major version. Was:\"Android\" Now:\"\"" };
            var apiValidation = Validate("TestPackage_Base", releaseType, "TestPackage_WithExcludePlatform");
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void RemoveIncludePlatform_FailsInMinor(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Removing from includePlatfoms requires a new major version. Was:\"Editor, Android\" Now:\"Editor\"" };
            var apiValidation = Validate("TestPackage_WithIncludePlatform", releaseType, "TestPackage_WithTwoIncludePlatforms");
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void RemoveAllIncludePlatforms_FailsInMinor(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Removing from includePlatfoms requires a new major version. Was:\"Editor\" Now:\"\"" };
            var apiValidation = Validate("TestPackage_Base", releaseType, "TestPackage_WithIncludePlatform");
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void BreakingChange_FailsInMinor(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Breaking changes require a new major version." };

            var apiValidation = Validate("TestPackage_BreakingChange", releaseType);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void ChangingAsmdefToTest_FailsInMinor(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Assembly \"Unity.PackageValidationSuite.EditorTests.TestPackage_Base\" no longer exists or is no longer included in build. This requires a new major version." };

            var apiValidation = Validate("TestPackage_Base", VersionComparisonTestUtilities.VersionForReleaseType(releaseType), isPreviousPackageTest: false, isProjectPackageTest: true);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInPatch")]
        public void ChangingAsmdefToNonTest_FailsInPatch(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string>
            {
                "Error: Additions require a new minor or major version.",
                "Error: New assembly \"Unity.PackageValidationSuite.EditorTests.TestPackage_Base\" may only be added in a new minor or major version."
            };

            var apiValidation = Validate("TestPackage_Base", VersionComparisonTestUtilities.VersionForReleaseType(releaseType), isPreviousPackageTest: true, isProjectPackageTest: false);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void RemovingAsmdef_FailsInMinor(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Assembly \"Unity.PackageValidationSuite.EditorTests.TestPackage_Base.NewAsmdef\" no longer exists or is no longer included in build. This requires a new major version." };

            var apiValidation = Validate("TestPackage_Base", releaseType, "TestPackage_AsmdefWithTypeAdd");
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void RenamingAsmdef_FailsInMinor(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected;
            if (releaseType == ReleaseType.Patch)
            {
                messagesExpected = new List<string>
                {
                    "Error: Additions require a new minor or major version.",
                    "Error: Assembly \"Unity.PackageValidationSuite.EditorTests.TestPackage_RenamedAsmdef\" no longer exists or is no longer included in build. This requires a new major version.",
                    "Error: New assembly \"SomeNewName\" may only be added in a new minor or major version."
                };
            }
            else
            {
                messagesExpected = new List<string>
                {
                    "Error: Assembly \"Unity.PackageValidationSuite.EditorTests.TestPackage_RenamedAsmdef\" no longer exists or is no longer included in build. This requires a new major version.",
                };
            }

            var apiValidation = Validate("TestPackage_RenamedAsmdef", releaseType);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        [Test]
        public void ExcludingEditorPlatformFailsDueToLackOfSupport()
        {
            List<string> messagesExpected = new List<string> { "Error: Package Validation Suite does not support .asmdefs that are not built on the \"Editor\" platform. See \"Unity.PackageValidationSuite.EditorTests.TestPackage_ExcludesEditor\"" };

            var apiValidation = Validate("TestPackage_ExcludesEditor", "0.1.0", "TestPackage_ExcludesEditor", copyAssemblies: false);
            ExpectResult(apiValidation, true, messagesExpected);
        }

        [Test]
        public void IncludingNonEditorPlatformFailsDueToLackOfSupport()
        {
            List<string> messagesExpected = new List<string> { "Error: Package Validation Suite does not support .asmdefs that are not built on the \"Editor\" platform. See \"Unity.PackageValidationSuite.EditorTests.TestPackage_IncludesAndroid\"" };

            var apiValidation = Validate("TestPackage_IncludesAndroid", "0.1.0", "TestPackage_IncludesAndroid", copyAssemblies: false);
            ExpectResult(apiValidation, true, messagesExpected);
        }

        [Test]
        [TestCaseSource(typeof(VersionComparisonTestUtilities), "FailsInMinor")]
        public void AddingUpdaterConfig_FailsInMinor(ReleaseType releaseType, bool expectError)
        {
            List<string> messagesExpected = new List<string> { "Error: Breaking changes require a new major version." };

            var apiValidation = Validate("TestPackage_WithObsoleteUpdater", releaseType);
            ExpectResult(apiValidation, expectError, messagesExpected);
        }

        //need to make decisions on dependencies and write more tests

        private static ApiValidation Validate(string projectPackageName, ReleaseType releaseType, string previousPackageName = "TestPackage_Base")
        {
            return Validate(projectPackageName, VersionComparisonTestUtilities.VersionForReleaseType(releaseType), previousPackageName);
        }

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

            //when the previous package is all test, there would be no binaries in the zip
            if (copyAssemblies && !isPreviousPackageTest)
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
                    PreviousPackageBinaryDirectory = VettingContext.PreviousVersionBinaryPath,
                    ValidationType = ValidationType.Publishing
                }
            };
            apiValidation.Setup();
            apiValidation.RunTest();
            return apiValidation;
        }

        private static void ExpectResult(ApiValidation apiValidation, bool expectError, List<string> messagesExpected)
        {
            if (expectError)
            {
                Assert.AreEqual(TestState.Failed, apiValidation.TestState);
                Assert.That(apiValidation.TestOutput.Where(o => o.StartsWith("Error")), Is.EquivalentTo(messagesExpected));
            }
            else
                Assert.AreEqual(TestState.Succeeded, apiValidation.TestState);
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
