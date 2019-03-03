using System;
using System.IO;
using Semver;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class UpdateValidation : BaseValidation
    {
        public UpdateValidation()
        {
            TestName = "Package Update Validation";
            TestDescription = "If this is an update, validate that the package's metadata is correct.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.Publishing };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;
            ValidateVersion();
        }

        private void ValidateVersion()
        {
#if UNITY_2018_2_OR_NEWER
            SemVersion version;
            if (!SemVersion.TryParse(Context.ProjectPackageInfo.version, out version, true))
            {
                Error("Failed to parse previous package version \"{0}\"", Context.ProjectPackageInfo.version);
                return;
            }

            SemVersion previousVersion = null;
            if (Context.PreviousPackageInfo != null && !SemVersion.TryParse(Context.PreviousPackageInfo.version, out previousVersion, true))
            {
                Error("Failed to parse previous package version \"{0}\"", Context.ProjectPackageInfo.version);
            }

            if (string.IsNullOrEmpty(version.Prerelease))
            {
                // This is a production submission, let's make sure it meets some criteria
                if (Context.PreviousPackageInfo == null)
                {
                    Warning("This package is not a preview version, but it's the first version of the package.  Should this package version be tagged as " + Context.ProjectPackageInfo.version + "-preview?");
                }
            }

            // If it exists, get the last one from that list.
            if (Context.ValidationType == ValidationType.Publishing && Utilities.PackageExistsOnProduction(Context.ProjectPackageInfo.Id))
            {
                Error("Version " + Context.ProjectPackageInfo.version + " of this package already exists in production.");
            }
#endif
        }
    }
}
