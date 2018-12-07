using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class RestrictedFilesValidation : BaseValidation
    {
        public RestrictedFilesValidation()
        {
            TestName = "Restricted File Type Validation";
            TestDescription = "Make sure no restricted file types are included with this package.";
            TestCategory = TestCategory.ContentScan;
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            // from the published project dir, check if each file type is present.
            foreach (var fileType in restrictedFileList)
            {
                List<string> matchingFiles = new List<string>();
                DirectorySearch(Context.PublishPackageInfo.path, fileType, matchingFiles);

                if (matchingFiles.Any())
                {
                    foreach (var file in matchingFiles)
                        TestOutput.Add(file + " cannot be included in a package.");

                    TestState = TestState.Failed;
                }
            }

            // from the published project dir, check if each file type is present.
            foreach (var fileType in unapprovedFileList)
            {
                List<string> matchingFiles = new List<string>();
                DirectorySearch(Context.PublishPackageInfo.path, fileType, matchingFiles);

                if (matchingFiles.Any())
                {
                    foreach (var file in matchingFiles)
                        Warning(file + " should not be included in packages unless absolutely necessary.  " + 
                                "Please confirm that it's inclusion is deliberate and intentional.");
                }
            }

        }

        private readonly string[] restrictedFileList =
        {
            "*.js",
            "*.jpg",
            "*.jpeg",
            "*.exe",
            "AssetStoreTools.dll",
            "AssetStoreToolsExtra.dll",
            "DroidSansMono.ttf",
            "AssetStoreToolsExtra.dll",
        };

        private readonly string[] unapprovedFileList =
        {
            "Standard Assets.*",
            "*.unitypackage",
            "*.zip",
            "*.rar",
            "*.lib",
            "*.dll"
        };
    }
}