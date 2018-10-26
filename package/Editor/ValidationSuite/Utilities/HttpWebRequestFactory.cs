using System.Net;

namespace UnityEditor.PackageManager.ValidationSuite
{
    public class HttpWebRequestFactory : IHttpWebRequestFactory
    {

        public IHttpWebRequest Create(string url)
        {
            return new HttpWebRequestWrap(HttpWebRequest.Create(url) as HttpWebRequest);
        }
    }
}
