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
            SupportedValidations = new[] { ValidationType.PackageManager };
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
                Error(String.Format("Failed to parse previous package version \"{0}\"", Context.ProjectPackageInfo.version));
                return;
            }

            SemVersion previousVersion = null;
            if (Context.PreviousPackageInfo != null && !SemVersion.TryParse(Context.PreviousPackageInfo.version, out previousVersion, true))
            {
                Error(string.Format("Failed to parse previous package version \"{0}\"", Context.ProjectPackageInfo.version));
            }

            // List out available versions for a package.
            var request = Client.Search(Context.ProjectPackageInfo.name);
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (string.IsNullOrEmpty(version.Prerelease))
            {
                // This is a production submission, let's make sure it meets some criteria
                if (Context.PreviousPackageInfo == null)
                {
                    TestOutput.Add("WARNING: This package is not a preview version, but it's the first version of the package.  Should this package version be tagged as " + Context.ProjectPackageInfo.version + "-preview?");
                }
            }

            // If it exists, get the last one from that list.
            try
            {
                Utilities.DownloadPackage(Context.ProjectPackageInfo.Id, Path.GetTempPath());
                Error("Version " + Context.ProjectPackageInfo.version + " of this package already exists in production.");
            }
            catch (Exception)
            {
                // This is the expectation, that the package doesn't exist.
            }
#endif
        }
    }
}