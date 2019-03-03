using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class TestsValidation : BaseValidation
    {
        public TestsValidation()
        {
            TestName = "Tests Validation";
            TestDescription = "Verify that the package has tests, and that test coverage is good.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.Publishing };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            // If the package has c# files, it should have tests.
            List<string> matchingFiles = new List<string>();
            DirectorySearch(Context.PublishPackageInfo.path, "*.cs", matchingFiles);

            if (!matchingFiles.Any())
                return;

            var testDir = Path.Combine(Context.PublishPackageInfo.path, "Tests");
            if (!Directory.Exists(testDir) && !Context.relatedPackages.Any())
            {
                AddMissingTestsErrors();
                return;
            }

            // let's look for files in the "test" directory.
            matchingFiles.Clear();
            DirectorySearch(testDir, "*.cs", matchingFiles);
            if (!matchingFiles.Any() && !Context.relatedPackages.Any())
            {
                AddMissingTestsErrors();
                return;
            }

            //Check if there are relatedPackages that may contain the tests
            foreach (var relatedPackage in Context.relatedPackages)
            {
                if (!Directory.Exists(relatedPackage.Path))
                {
                    if (Context.ValidationType == ValidationType.Publishing ||
                        Context.ValidationType == ValidationType.VerifiedSet)
                    {
                        AddMissingTestsErrors();
                    }
                    else
                    {
                        Warning(string.Format("Related Package is missing in {0}", relatedPackage.Path));
                    }
                    return;
                }

                var relatedPackageTestDir = Path.Combine(relatedPackage.Path, "Tests");

                // let's look for files in the "test" directory.
                matchingFiles.Clear();
                DirectorySearch(relatedPackageTestDir, "*.cs", matchingFiles);
                if (!matchingFiles.Any())
                {
                    Error("Related Packages must include test for automated testing.");
                    return;
                }
            }

            // TODO: Go through files, make sure they have actual tests.

            // TODO: Can we evaluate coverage imperically for now, until we have code coverage numbers?
        }

        private void AddMissingTestsErrors()
        {
            Error("Preview and Production quality packages must include test for automated testing.");
        }
    }
}
