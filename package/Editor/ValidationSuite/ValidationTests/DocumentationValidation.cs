
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
        }

        protected override void Run()
        {
            SemVersion packageJsonVersion;

            if (!SemVersion.TryParse(Context.ProjectPackageInfo.version, out packageJsonVersion))
            {
                TestState = TestState.Failed;
                TestOutput.Add(string.Format("Version format is not valid: {0} in: [{1}]", Context.ProjectPackageInfo.version, Context.ProjectPackageInfo.path));
                return;
            }

            var version = string.Format("{0}@{1}", Context.ProjectPackageInfo.name.ToLower(), packageJsonVersion.ShortVersion());
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
                TestState = TestState.Failed;
                TestOutput.Add("Couldn't find a documentation website for this package.  Please contact the docs team to ensure a site is up before you publish to production.");
                TestOutput.Add("Expected Website: " + url);
                TestOutput.Add(e.Message);
            }
        }
    }
}