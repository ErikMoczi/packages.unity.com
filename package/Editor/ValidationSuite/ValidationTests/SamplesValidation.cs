using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class SamplesValidation : BaseValidation
    {
        public SamplesValidation()
        {
            TestName = "Samples Validation";
            TestDescription = "Verify that samples meet expectation, if the package has samples.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.Publishing, ValidationType.VerifiedSet };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            var samplesDirExists = Directory.Exists(Path.Combine(Context.PublishPackageInfo.path, "Samples"));
            var sampledTildeDirExists = Directory.Exists(Path.Combine(Context.PublishPackageInfo.path, "Samples~"));
            if (!samplesDirExists && !sampledTildeDirExists && Context.PublishPackageInfo.samples.Count == 0)
            {
                TestOutput.Add("No samples found. Skipping Samples Validation.");
                TestState = TestState.NotRun;
                return;
            }

            if (samplesDirExists && sampledTildeDirExists)
            {
                Error("`Samples` and `Samples~` cannot both be present in the package.");
            }

            if ((Context.ValidationType == ValidationType.Publishing || Context.ValidationType == ValidationType.VerifiedSet) && samplesDirExists)
            {
                Error("In a published package, the `Samples` needs to be renamed to `Samples~`. It should have been done automatically in the CI publishing process.");
            }

            var samplesDir = samplesDirExists ? "Samples" : "Samples~";
            var matchingFiles = new List<string>();
            DirectorySearch(Path.Combine(Context.PublishPackageInfo.path, samplesDir), ".sample.json", matchingFiles);

            if (matchingFiles.Count == 0)
            {
                Error(samplesDir + " folder exists but no `.sample.json` files found in it." +
                    "A `.sample.json` file is required for a sample to be recognized." +
                    "Please refer to https://gitlab.internal.unity3d.com/upm-packages/upm-package-template/blob/master/Samples/Example/.sample.json for more info.");
            }

            if (Context.ValidationType == ValidationType.Publishing || Context.ValidationType == ValidationType.VerifiedSet)
            {
                if (Context.PublishPackageInfo.samples.Count != matchingFiles.Count)
                {
                    Error("The number of samples in `package.json` does not match the number of `.sample.json` files found in `" + samplesDir + "`.");
                }

                foreach (var sample in Context.PublishPackageInfo.samples)
                {
                    if (string.IsNullOrEmpty(sample.path))
                        Error("Sample path must be set and non-empty in `package.json`.");
                    if (string.IsNullOrEmpty(sample.displayName))
                        Error("Sample display name will be shown in the UI, and it must be set and non-empty in `package.json`.");
                    var samplePath = Path.Combine(Context.PublishPackageInfo.path, sample.path);
                    var sampleJsonPath = Path.Combine(samplePath, ".sample.json");
                    if (!Directory.Exists(samplePath))
                    {
                        Error("Sample path set in `package.json` does not exist: " + sample.path + ".");
                    }
                    else if (!File.Exists(sampleJsonPath))
                    {
                        Error("Cannot find `.sample.json` file in the sample path: " + sample.path + ".");
                    }
                }
            }
        }
    }
}
