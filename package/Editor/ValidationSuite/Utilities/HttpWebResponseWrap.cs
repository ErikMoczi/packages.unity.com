using System;
using System.IO;
using System.Net;

namespace UnityEditor.PackageManager.ValidationSuite
{
    public class HttpWebResponseWrap : IHttpWebResponse
    {
        private HttpWebResponse _response;

        public HttpStatusCode StatusCode { get; set; }

        public HttpWebResponseWrap(HttpWebResponse response)
        {
            _response = response;
            StatusCode = response.StatusCode;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_response != null)
                {
                    ((IDisposable)_response).Dispose();
                    _response = null;
                }
            }
        }

        public Stream GetResponseStream()
        {
            return _response.GetResponseStream();
        }
    }
}
