using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;

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
            if (!Directory.Exists(testDir))
            {
                AddMissingTestsErrors();
                return;
            }

            // let's look for files in the "test" directory.
            matchingFiles.Clear();
            DirectorySearch(testDir, "*.cs", matchingFiles);
            if (!matchingFiles.Any())
            {
                AddMissingTestsErrors();
                return;
            }

            // TODO: Go through files, make sure they have actual tests.

            // TODO: Can we evaluate coverage imperically for now, until we have code coverage numbers?

        }

        private void AddMissingTestsErrors()
        {
            if (Context.ProjectPackageInfo.IsPreview)
                Warning("Package must include tests before it can come out of preview.");
            else
                Error("Production quality packages must include test for automated testing.");
        }
    }
}