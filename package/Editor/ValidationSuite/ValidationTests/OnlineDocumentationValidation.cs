using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Semver;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class OnlineDocumentationValidation : BaseValidation
    {
        private const string docsurl = "https://docs.unity3d.com/Packages/{0}/manual/index.html";
        private IHttpWebRequestFactory httpWebRequestFactory;

        public OnlineDocumentationValidation()
        {
            Initialize(new HttpWebRequestFactory());
        }

        public OnlineDocumentationValidation(IHttpWebRequestFactory httpFactory)
        {
            Initialize(httpFactory);
        }

        private void Initialize(IHttpWebRequestFactory httpFactory)
        {
            TestName = "Online Documentation Validation";
            TestDescription = "Make sure the package has online documentation. It is required for a verified package and optional for a preview package";
            TestCategory = TestCategory.DataValidation;
#if UNITY_2019_1_OR_NEWER
            SupportedValidations = new[] { ValidationType.LocalDevelopment, ValidationType.Publishing };
#else
            SupportedValidations = new[] { ValidationType.Publishing };
#endif
            httpWebRequestFactory = httpFactory;
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
            var url = string.Format(docsurl, version);
            var request = RequestDocs(url);

            try
            {
                ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
                using (var webResponse = request.GetResponse())
                {
                    if (webResponse.StatusCode == HttpStatusCode.OK)
                    {
                        var responseReader = new StreamReader(webResponse.GetResponseStream());
                        responseReader.ReadToEnd();
                    }
                    else
                    {
                        if (!Context.ProjectPackageInfo.IsPreview)
                        {
                            Error(
                                "The documentation website could not complete your request. Please contact the docs team to ensure a site is up before you publish to production. (StatusCode: " +
                                webResponse.StatusCode + ")");
                            TestOutput.Add("Expected Website: " + url);
                        }
                        else
                        {
                            Warning(
                                "The documentation website could not complete your request, which is required before it can be removed from \"Preview\".  Contact the documentation team for assistance. (StatusCode: " +
                                webResponse.StatusCode + ")");
                        }
                    }
                }
            }
            catch (WebException e)
            {
                if (!Context.ProjectPackageInfo.IsPreview)
                {
                    Error(
                        "Couldn't find a documentation website for this package.  Please contact the docs team to ensure a site is up before you publish to production.");
                    TestOutput.Add("Expected Website: " + url);
                    TestOutput.Add(e.Message);
                }
                else
                {
                    Warning(
                        "This package contains no web based documentation, which is required before it can be removed from \"Preview\".  Contact the documentation team for assistance.");
                }
            }
        }

        /**
         * This is to make https requests work
         */
        public bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;
            // If there are errors in the certificate chain,
            // look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        continue;
                    }
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2)certificate);
                    if (!chainIsValid)
                    {
                        isOk = false;
                        break;
                    }
                }
            }
            return isOk;
        }

        private IHttpWebRequest RequestDocs(string url)
        {
            IHttpWebRequest request = httpWebRequestFactory.Create(url);
            request.Method = "GET";
            request.Timeout = 60 * 1000; // 60 seconds time out
            request.UserAgent = "UnityAgent";

            return request;
        }
    }
}
