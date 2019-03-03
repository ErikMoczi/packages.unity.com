using System.Net;

namespace UnityEditor.PackageManager.ValidationSuite
{
    public class HttpWebRequestWrap : IHttpWebRequest
    {
        private HttpWebRequest _request;

        public string Method
        {
            get { return _request.Method; }
            set { _request.Method = value;  }
        }

        public int Timeout
        {
            get { return _request.Timeout; }
            set { _request.Timeout = value; }
        }

        public string UserAgent
        {
            get { return _request.UserAgent; }
            set { _request.UserAgent = value; }
        }

        public HttpWebRequestWrap(HttpWebRequest req)
        {
            _request = req;
        }

        public IHttpWebResponse GetResponse()
        {
            return new HttpWebResponseWrap((HttpWebResponse)_request.GetResponse());
        }
    }
}
