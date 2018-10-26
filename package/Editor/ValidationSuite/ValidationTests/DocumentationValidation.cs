
using System.IO;
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
            // TODO:  Add check for local feature doc.
            //        Check for XMLDocs

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
                    TestState = TestState.Succeeded;
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