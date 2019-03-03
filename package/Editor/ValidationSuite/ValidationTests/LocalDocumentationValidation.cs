using System.IO;
using System.Linq;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class LocalDocumentationValidation : BaseValidation
    {
        public LocalDocumentationValidation()
        {
            TestName = "Documentation Validation";
            TestDescription = "Make sure the package has local documentation. It is required for a verified package and recommended for a preview package";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.Publishing, ValidationType.VerifiedSet };
        }

        protected override void Run()
        {
            TestState = TestState.Succeeded;
            CheckLocalDocumentation();
        }

        protected void CheckLocalDocumentation()
        {
            // Check for a documentation directory.
            string[] rootDirs = Directory.GetDirectories(Context.ProjectPackageInfo.path);
            var wrongNameDocsDir = rootDirs.FirstOrDefault(d =>
            {
                var path = Path.GetFileName(d).ToLower();
                return path == ".documentation" || path == "documentation";
            });
            var docsDir = rootDirs.FirstOrDefault(d =>
            {
                var path = Path.GetFileName(d).ToLower();
                return path == ".documentation~" || path == "documentation~";
            });

            string[] allDocsDir = Directory.GetDirectories(Context.ProjectPackageInfo.path, "*Documentation*");
            if (allDocsDir.Length > 1)
            {
                Error("You have multiple documentation folders. Please keep only the one named \"Documentation~\".");
                return;
            }
            else if (!string.IsNullOrEmpty(wrongNameDocsDir))
            {
                Error("Please rename your \"Documentation\" folder to \"Documentation~\" so that its files are ignored by the asset database.");
                return;
            }
            else if (string.IsNullOrEmpty(docsDir))
            {
                Error("Your package must contain a \"Documentation~\" folder at the root, which holds your package's documentation.");
                return;
            }

            var defaultFiles = Directory.GetFiles(docsDir, "your-package-name.md");
            // Check the default file is not there anymore
            if (defaultFiles.Length > 0)
            {
                Error("File \"your-package-name.md\" found in \"Documentation~\" directory, which comes from the package template.  Please take the time to work on your documentation.");
            }

            var docFiles = Directory.GetFiles(docsDir, "*.md");
            // Check for at least 1 md file in that directory.
            if (docFiles.Length == 0)
            {
                Error("Your package must contain a \"Documentation~\" folder, with at least one \"*.md\" file in order for documentation to properly get built.");
            }
            // Check that documentation files (except the default file) have at least 10 characters in them
            else if (docFiles.Length > defaultFiles.Length)
            {
                foreach (string filePath in docFiles)
                {
                    if (filePath.IndexOf("your-package-name.md") == -1)
                    {
                        long fileLength = new FileInfo(filePath).Length;
                        if (fileLength < 10)
                        {
                            Error("Your documentation file " + filePath + " should contain at least 10 characters.");
                        }
                    }
                }
            }
        }
    }
}
