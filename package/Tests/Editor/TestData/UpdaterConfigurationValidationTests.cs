using NUnit.Framework;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using System.Linq;
#if UNITY_2019_1_OR_NEWER

namespace UnityEditor.PackageManager.ValidationSuite.Tests
{
    // To debug these tests in the APIUpdater.ConfigurationValidator.exe code:
    // * Set a breakpoint in UpdateConfigurationValidation.cs after processStartInfo is built
    // * Attach to Unity
    // * Run the test from the Test Runner
    // * Copy the command line arguments from processStartInfo
    // * While still paused in the debugger, copy the response file (first parameter) from the tmp folder to a different folder
    // * Update the path in the copied command line arguments
    // * Configure the APIUpdater.ConfigurationValidator to run with the command line parameters rooted in the project directory
    // * Run with debugging
    // For more accuracy, set the configuration to run `build\WindowsEditor\Data\Tools\ScriptUpdater\APIUpdater.ConfigurationValidator.exe`. This is the exe built by jam.
    internal class UpdaterConfigurationValidationTests
    {
        internal const string testPackageRoot =
            "Packages/com.unity.package-validation-suite/Tests/Editor/TestData/CVAssemb/";
        [Test]
        public void GoodRenameGivesNoErrors()
        {
            var updateConfigurationValidation = new UpdateConfigurationValidation()
            {
                Context = new VettingContext()
                {
                    ProjectPackageInfo = new VettingContext.ManifestData()
                    {
                        path = testPackageRoot + "WithGoodRenameConfig",
                    },
                    ValidationType = ValidationType.Publishing
                }
            };
            updateConfigurationValidation.RunTest();
            Assert.AreEqual(TestState.Succeeded, updateConfigurationValidation.TestState, string.Join("\n", updateConfigurationValidation.TestOutput));
        }

        [Test]
        public void GoodRenameOnDllWithSpacesGivesNoErrors()
        {
            var updateConfigurationValidation = new UpdateConfigurationValidation()
            {
                Context = new VettingContext()
                {
                    ProjectPackageInfo = new VettingContext.ManifestData()
                    {
                        path = testPackageRoot + "WithSpacesAndGoodRename",
                    },
                    ValidationType = ValidationType.Publishing
                }
            };
            updateConfigurationValidation.RunTest();
            Assert.AreEqual(TestState.Succeeded, updateConfigurationValidation.TestState, string.Join("\n", updateConfigurationValidation.TestOutput));
        }

        [Test]
        public void InvalidRenameGivesErrors()
        {
            var updateConfigurationValidation = new UpdateConfigurationValidation()
            {
                Context = new VettingContext()
                {
                    ProjectPackageInfo = new VettingContext.ManifestData()
                    {
                        path = testPackageRoot + "WithInvalidRenameConfig"
                    },
                    ValidationType = ValidationType.Publishing
                }
            };
            updateConfigurationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, updateConfigurationValidation.TestState, string.Join("\n", updateConfigurationValidation.TestOutput));
            Assert.That(updateConfigurationValidation.TestOutput.Single(o => o.StartsWith("Error")), Contains.Substring("Error: Failed to resolve target member in configuration [*] System.Void [*] WithInvalidRenameConfig.Foo::Bar -> * WithInvalidRenameConfig.Foo::Baz"));
        }

        [Test]
        public void GoodRenameOnDllGivesNoErrors()
        {
            var updateConfigurationValidation = new UpdateConfigurationValidation()
            {
                Context = new VettingContext()
                {
                    ProjectPackageInfo = new VettingContext.ManifestData()
                    {
                        path = testPackageRoot + "WithGoodRenameConfigInDll"
                    },
                    ValidationType = ValidationType.Publishing
                }
            };
            updateConfigurationValidation.RunTest();
            Assert.AreEqual(TestState.Succeeded, updateConfigurationValidation.TestState, string.Join("\n", updateConfigurationValidation.TestOutput));
        }

        [Test]
        public void NativeDllsGiveNoErrors()
        {
            var updateConfigurationValidation = new UpdateConfigurationValidation()
            {
                Context = new VettingContext()
                {
                    ProjectPackageInfo = new VettingContext.ManifestData()
                    {
                        path = testPackageRoot + "WithNativeDll"
                    },
                    ValidationType = ValidationType.Publishing
                }
            };
            updateConfigurationValidation.RunTest();
            Assert.AreEqual(TestState.Succeeded, updateConfigurationValidation.TestState, string.Join("\n", updateConfigurationValidation.TestOutput));
        }

        [Test]
        public void InvalidRenameOnDllGivesErrors()
        {
            var updateConfigurationValidation = new UpdateConfigurationValidation()
            {
                Context = new VettingContext()
                {
                    ProjectPackageInfo = new VettingContext.ManifestData()
                    {
                        path = testPackageRoot + "WithInvalidRenameConfigInDll"
                    },
                    ValidationType = ValidationType.Publishing
                }
            };
            updateConfigurationValidation.RunTest();

            Assert.AreEqual(TestState.Failed, updateConfigurationValidation.TestState, string.Join("\n", updateConfigurationValidation.TestOutput));
            Assert.That(updateConfigurationValidation.TestOutput.Single(o => o.StartsWith("Error")), Contains.Substring("Error: Failed to resolve target member in configuration [*] System.Void [*] TestPackage_WithInvalidRenameConfigInDll.Foo::Bar([*] UnityEngine.MonoBehaviour) -> * TestPackage_WithInvalidRenameConfigInDll.Foo::Baz(UnityEngine.MonoBehaviour)"));
        }
    }
}
#endif
