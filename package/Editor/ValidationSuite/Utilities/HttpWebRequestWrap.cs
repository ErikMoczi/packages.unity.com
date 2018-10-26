using System.Net;

namespace UnityEditor.PackageManager.ValidationSuite { 
    public class HttpWebRequestWrap : IHttpWebRequest {

        private HttpWebRequest _request;

        private string _method;
        public string Method {
            get { return _method; }
            set { _request.Method = value;  }
        }
        private string _timeout;
        public int Timeout
        {
            get { return Timeout; }
            set { _request.Timeout = value; }
        }
        private string _userAgent;
        public string UserAgent
        {
            get { return _userAgent; }
            set { _request.UserAgent = value; }
        }

        public HttpWebRequestWrap(HttpWebRequest req) {
            _request = req;
        }

        public IHttpWebResponse GetResponse()
        {
            return new HttpWebResponseWrap((HttpWebResponse)_request.GetResponse());
        }
    }
}
