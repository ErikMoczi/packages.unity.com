using System;
using System.Collections.Generic;
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

            var manifestData = Context.ProjectPackageInfo;
            if (manifestData == null)
            {
                Error("Manifest not available. Not validating manifest contents.");
                return;
            }

            ValidateManifestData();
            ValidateVersion();
            ValidateDependencyChanges();
        }

        private void ValidateDependencyChanges()
        {
            var versionChangeType = Context.VersionChangeType;

            var previousRefs = Context.PreviousPackageInfo == null ? null : Context.PreviousPackageInfo.dependencies;
            var projectRefs = Context.ProjectPackageInfo.dependencies ?? new Dictionary<string, string>();

            foreach (var projectRef in projectRefs)
            {
                SemVersion projectRefSemver;
                if (!SemVersion.TryParse(projectRef.Value, out projectRefSemver))
                {
                    Error(String.Format(@"Invalid version number in dependency ""{0}"" : ""{1}""", projectRef.Key, projectRef.Value));
                    continue;
                }

                string previousRefVersion;
                if (previousRefs.TryGetValue(projectRef.Key, out previousRefVersion))
                {
                    SemVersion previousRefSemver;
                    if (SemVersion.TryParse(previousRefVersion, out previousRefSemver))
                    {
                        if (previousRefSemver.Major != projectRefSemver.Major &&
                            (versionChangeType == VersionChangeType.Patch || versionChangeType == VersionChangeType.Minor))
                        {
                            Error(String.Format(@"Dependency major versions may only change in major releases. ""{0}"": ""{1}"" -> ""{2}""", 
                                projectRef.Key, previousRefVersion, projectRef.Value));
                        }
                    }
                }
                else
                {
                    this.TestOutput.Add(string.Format(@"Added dependency: ""{0}"": ""{1}""", projectRef.Key, projectRef.Value));
                    if (versionChangeType == VersionChangeType.Patch ||
                        versionChangeType == VersionChangeType.Minor)
                        Error("Adding package dependencies requires a new major version.");
                }
            }

            if (previousRefs != null)
            {
                foreach (var previousRef in previousRefs)
                {
                    SemVersion previousSemver;
                    if (!SemVersion.TryParse(previousRef.Value, out previousSemver))
                        TestOutput.Add(String.Format(@"Invalid version number in previous package dependency ""{0}"" : ""{1}""", previousRef.Key, previousRef.Value));

                    if (!projectRefs.ContainsKey(previousRef.Key) && versionChangeType == VersionChangeType.Patch)
                        Error(string.Format("Removing dependencies is not forwards-compatible and requires a new major or minor version. Removed dependency: {0}", previousRef.Key));
                }
            }
        }

        private void ValidateManifestData()
        {
            var manifestData = Context.ProjectPackageInfo;
            // Check the package Name, which needs to start with "com.unity."
            if (manifestData.name == (PackageNamePrefix + "[your package name]") ||
                !manifestData.name.StartsWith(PackageNamePrefix) || 
                manifestData.name.Length == PackageNamePrefix.Length)
            {
                Error(string.Format("In package.json, \"name\" needs to start with \"{0}\", and end with your package name.", PackageNamePrefix));
            }

            // There cannot be any capital letters in package names.
            if (manifestData.name.ToLower(CultureInfo.InvariantCulture) != manifestData.name)
            {
                Error("In package.json, \"name\" cannot contain capital letter");
            }

            // Check name against our regex.
            Match match = Regex.Match(manifestData.name, UpmRegex);
            if (!match.Success)
            {
                Error("In package.json, \"name\" is not a valid name.");
            }

            if (string.IsNullOrEmpty(manifestData.displayName))
            {
                Error("In package.json, \"DisplayName\" must be set.");
            }
            else if (manifestData.displayName.Length > 25)
            {
                Error("In package.json, \"DisplayName\" is too long. Max Length = 25");
            }

            // Check Description, make sure it's there, and not too short.
            if (manifestData.description.Length < MinDescriptionSize)
            {
                Error("In package.json, \"description\" must be fleshed out and informative, as it is used in the user interface.");
            }

            // Check package version, make sure it's a valid SemVer string.
            SemVersion packageVersionNumber;
            if (!SemVersion.TryParse(manifestData.version, out packageVersionNumber))
            {
                Error("In package.json, \"version\" needs to be a valid \"Semver\".");
            }
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

            // If it exists, get the last one from that list.
            if (request.Result != null && request.Result.Length > 0)
            {
                if (request.Result[0].versions.all.Any(v => v == Context.ProjectPackageInfo.version))
                {
                    Error("Version " + Context.ProjectPackageInfo.version + " of this package already exists in production.");
                }
            }

            if (string.IsNullOrEmpty(version.Prerelease))
            {
                // This is a production submission, let's make sure it meets some criteria
                if (Context.PreviousPackageInfo == null)
                {
                    TestOutput.Add("WARNING: This package is not a preview version, but it's the first version of the package.  Should this package version be tagged as " + Context.ProjectPackageInfo.version + "-preview?");
                }
            }
#endif
        }
    }
}