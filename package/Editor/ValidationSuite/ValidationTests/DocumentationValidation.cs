
using System.IO;
using System.Linq;
using System.Net;
using Semver;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class DocumentationValidation : BaseValidation
    {
        public DocumentationValidation()
        {
            TestName = "Documentation Validation";
            TestDescription = "Make sure the documentation site exists for this version of the package.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        internal static string ShortVersionId(string packageName, SemVersion version)
        {
            var shortVersion = string.Format("{0}.{1}", version.Major, version.Minor);
            var shortVersionId = string.Format("{0}@{1}", packageName.ToLower(), shortVersion);

            return shortVersionId;
        }

        protected override void Run()
        {
            TestState = TestState.Succeeded;
            CheckOnlineDocumentation();
            CheckLocalDocumentation();

        }

        protected void CheckLocalDocumentation()
        {
            // Check for a documentation directory.
            var rootDirs = Directory.GetDirectories(Context.ProjectPackageInfo.path);
            var docsDir = rootDirs.SingleOrDefault(d =>
            {
            	var path = Path.GetFileName(d).ToLower();
            	return path == "documentation" || path == ".documentation" || path == "documentation~";
            });
            if (string.IsNullOrEmpty(docsDir))
            {
                Error("Your package must contain a \"Documentation\" folder, which holds your package's documentation.");
                return;
            }

            var docFiles = Directory.GetFiles(docsDir, "your-package-name.md");
            // Check for at least 1 md file in that directory.
            if (docFiles.Length > 0)
            {
                Error("File \"your-package-name.md\" found in \"Documentation\" directory, which comes from the package template.  Please take the time to work on your documentation.");
            }

            docFiles = Directory.GetFiles(docsDir, "*.md");

            // Check for at least 1 md file in that directory.
            if (docFiles.Length == 0)
            {
                Error("Your package must contain a \"Documentation\" folder, with at least one \"*.md\" file in order for documentation to properly get built.");
            }
            
            // TODO:  Add check for local feature doc.
            //        Check for XMLDocs
        }

        protected void CheckOnlineDocumentation()
        {
            SemVersion packageJsonVersion;

            if (!SemVersion.TryParse(Context.ProjectPackageInfo.version, out packageJsonVersion))
            {
                TestState = TestState.Failed;
                TestOutput.Add(string.Format("Version format is not valid: {0} in: [{1}]", Context.ProjectPackageInfo.version, Context.ProjectPackageInfo.path));
                return;
            }

            var version = ShortVersionId(Context.ProjectPackageInfo.name, packageJsonVersion);
            var url = string.Format("https://docs.unity3d.com/Packages/{0}/index.html", version);

            var request = HttpWebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";

            request.UserAgent = "UnityAgent";

            try
            {
                using (var webResponse = request.GetResponse())
                {
                    var responseReader = new StreamReader(webResponse.GetResponseStream());
                    responseReader.ReadToEnd();
                }
            }
            catch (WebException e)
            {
                if (!Context.ProjectPackageInfo.IsPreview)
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(
                        "Couldn't find a documentation website for this package.  Please contact the docs team to ensure a site is up before you publish to production.");
                    TestOutput.Add("Expected Website: " + url);
                    TestOutput.Add(e.Message);
                }
                else
                {
                    TestOutput.Add("Warning: this package contains no web based documentation, which is required before it can be removed from \"Preview\".  Contact the documentation team for assistance.");
                }
            }
        }
    }
}