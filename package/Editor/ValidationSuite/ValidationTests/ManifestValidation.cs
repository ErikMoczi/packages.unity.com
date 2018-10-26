using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Semver;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class ManifestValidation : BaseValidation
    {
        private const string PackageNamePrefix = "com.unity.";
        private const string UpmRegex = @"^[a-z0-9][a-z0-9-._]{0,213}$";
        private const int MinDescriptionSize = 50;

        public ManifestValidation()
        {
            TestName = "Manifest Validation";
            TestDescription = "Validate that the information found in the manifest is well formatted.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;
            ValidateManifestData();
            ValidateVersion();
        }

        private void ValidateManifestData()
        {
            var manifestData = Context.ProjectPackageInfo;
            if (manifestData == null)
            {
                TestState = TestState.Failed;
                TestOutput.Add("Manifest is null");
                return;
            }
                
            // Check the package Name, which needs to start with "com.unity."
            if (manifestData.name == (PackageNamePrefix + "[your package name]") ||
                !manifestData.name.StartsWith(PackageNamePrefix) || 
                manifestData.name.Length == PackageNamePrefix.Length)
            {
                TestState = TestState.Failed;
                TestOutput.Add(string.Format("In package.json, \"name\" needs to start with \"{0}\", and end with your package name.", PackageNamePrefix));
            }

            // There cannot be any capital letters in package names.
            if (manifestData.name.ToLower(CultureInfo.InvariantCulture) != manifestData.name)
            {
                TestState = TestState.Failed;
                TestOutput.Add("In package.json, \"name\" cannot contain capital letter");
            }

            // Check name against our regex.
            Match match = Regex.Match(manifestData.name, UpmRegex);
            if (!match.Success)
            {
                TestState = TestState.Failed;
                TestOutput.Add("In package.json, \"name\" is not a valid name.");
            }

            if (string.IsNullOrEmpty(manifestData.displayName))
            {
                TestState = TestState.Failed;
                TestOutput.Add("In package.json, \"DisplayName\" must be set.");
            }
            else if (manifestData.displayName.Length > 25)
            {
                TestState = TestState.Failed;
                TestOutput.Add("In package.json, \"DisplayName\" is too long. Max Length = 25");
            }

            // Check Description, make sure it's there, and not too short.
            if (manifestData.description.Length < MinDescriptionSize)
            {
                TestState = TestState.Failed;
                TestOutput.Add("In package.json, \"description\" must be fleshed out and informative, as it is used in the user interface.");
            }

            // Check package version, make sure it's a valid SemVer string.
            SemVersion packageVersionNumber;
            if (!SemVersion.TryParse(manifestData.version, out packageVersionNumber))
            {
                TestState = TestState.Failed;
                TestOutput.Add("In package.json, \"version\" needs to be a valid \"Semver\".");
            }
        }

        private void ValidateVersion()
        {
#if UNITY_2018_2_OR_NEWER
            var version = SemVersion.Parse(Context.ProjectPackageInfo.version);
            var previousVersion = Context.PreviousPackageInfo != null ? SemVersion.Parse(Context.PreviousPackageInfo.version) : null;

            // List out available versions for a package.
            var request = Client.Search(Context.ProjectPackageInfo.name);
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }

            // If it exists, get the last one from that list.
            if (request.Result != null && request.Result.Length > 0)
            {
                if (request.Result[0].versions.all.Any(v => v == Context.ProjectPackageInfo.version))
                {
                    TestState = TestState.Failed;
                    TestOutput.Add("Version " + Context.ProjectPackageInfo.version + " of this package already exists in production.");
                }
            }

            if (string.IsNullOrEmpty(version.Prerelease))
            {
                // This is a production submission, let's make sure it meets some criteria
                if (previousVersion == null)
                {
                    TestOutput.Add("WARNING: This package is not a preview version, but it's the first version of the package.  Should this package version be tagged as " + Context.ProjectPackageInfo.version + "-preview?");
                }
                else
                {
                    
                }
            }
            else
            {

            }
#endif
        }
    }
}