namespace UnityEditor.PackageManager.ValidationSuite.Mocks
{
    public class MockHttpWebRequestFactory : IHttpWebRequestFactory
    {
        private IHttpWebRequest _request { get; set; }

        public MockHttpWebRequestFactory(IHttpWebRequest req)
        {
            _request = req;
        }

        public IHttpWebRequest Create(string url)
        {
            return _request;
        }
    }
}
