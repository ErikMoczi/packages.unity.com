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

            // This test needs work
            TestState = TestState.NotImplementedYet;
        }

        void DirectorySearch(string path, string searchPattern, List<string> matches)
        {
            foreach (string subDir in Directory.GetDirectories(path))
            {
                var files = Directory.GetFiles(subDir, searchPattern);
                if (files.Any())
                    matches.AddRange(files);

                DirectorySearch(subDir, searchPattern, matches);
            }
        }

        private readonly string[] restrictedFileList =
        {
            "*.ttz",
        };
    }
}