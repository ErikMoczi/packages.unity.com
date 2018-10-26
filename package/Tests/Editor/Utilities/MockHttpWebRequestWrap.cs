
using UnityEditor.PackageManager.ValidationSuite.Tests;

namespace UnityEditor.PackageManager.ValidationSuite.Mocks
{
    internal class MockHttpWebRequestWrap : IHttpWebRequest
    {        
        public string Method { get; set; }
        public int Timeout { get; set; }
        public string UserAgent { get; set; }
        private string _urlAsked;
        
        public MockHttpWebRequestWrap(string url)
        {
            _urlAsked = url;
        }

        public IHttpWebResponse GetResponse()
        {
            return new MockHttpWebResponseWrap(_urlAsked);
        }
    }
}
