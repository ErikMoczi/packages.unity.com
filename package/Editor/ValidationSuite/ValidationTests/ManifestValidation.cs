using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Semver;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    public class ManifestValidation : BaseValidation
    {
        private const string ManifestFileName = "package.json";
        private const string PackageNamePrefix = "com.unity.";
        private const string UpmRegex = @"^[a-z0-9][a-z0-9-._]{0,213}$";
        private const int MinDescriptionSize = 20;
        private string unityVersion;

        public ManifestValidation()
        {
            TestName = "Manifest Validation";
            TestDescription = "Validate that the information found in the manifest is well formatted.";
            TestCategory = TestCategory.DataValidation;
        }

        // This method is called synchronously during initialization, 
        // and allows a test to interact with APIs, which need to run from the main thread.
        public override void Setup()
        {
            unityVersion = UnityEngine.Application.unityVersion;
        }

        public override void Run(string packagePath)
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            var manifestPath = Path.Combine(packagePath, ManifestFileName);

            if (!File.Exists(manifestPath))
            {
                TestState = TestState.Failed;
                TestOutput.Add("Did not find file package.json in directory " + packagePath);
                return;
            }

            // Read manifest json data, and convert it.
            var textManifestData = File.ReadAllText(manifestPath);
            ManifestData manifestData = null;

            try
            {
                manifestData = JsonUtility.FromJson<ManifestData>(textManifestData);
            }
            catch (Exception)
            {
                TestState = TestState.Failed;
                TestOutput.Add("File package.json, was not well formated.");
                return;
            }

            ValidateManifestData(manifestData);
        }

        private void ValidateManifestData(ManifestData manifestData)
        {
            // Check the package Name, which needs to start with "com.unity."
            if (manifestData.name == (PackageNamePrefix + "[your package name]") ||
                !manifestData.name.StartsWith(PackageNamePrefix) || 
                manifestData.name.Length == PackageNamePrefix.Length)
            {
                TestState = TestState.Failed;
                TestOutput.Add(string.Format("In package.json, \"name\" needs to start with \"{0}\", and end with your package name.", PackageNamePrefix));
            }

            // There cannot be any capital letters in package names.
            if (manifestData.name.Any(char.IsUpper))
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

            // Check Unity Version, make sure it's valid given current version of Unity
            double unityVersionNumber, packageUnityVersionNumber;

            if (!double.TryParse(unityVersion.Substring(0, unityVersion.LastIndexOf(".")), out unityVersionNumber) ||
                !double.TryParse(manifestData.unity, out packageUnityVersionNumber) ||
                unityVersionNumber != packageUnityVersionNumber)
            {
                TestState = TestState.Failed;
                TestOutput.Add("In package.json, \"unity\" is pointing to a version different from the editor you are using.  Validation needs to happen on the right version of the editor.");
            }
        }

        public class ManifestData
        {
            public string name;
            public string description;
            public string version;
            public string unity;
        }
    }
}