using System;
using System.IO;
using System.Text;
using System.Net;

namespace UnityEditor.PackageManager.ValidationSuite.Mocks
{
    public class MockHttpWebResponseWrap : IHttpWebResponse
    {
        private Stream _stream;
        public HttpStatusCode StatusCode { get; set; }

        public MockHttpWebResponseWrap(string url)
        {
            var expected = "usual response content";
            var expectedBytes = Encoding.UTF8.GetBytes(expected);

            if (url == "200")
            {
                StatusCode = HttpStatusCode.OK;
                expected = "valid response content";
                expectedBytes = Encoding.UTF8.GetBytes(expected);
            }
            if (url == "204")
            {
                StatusCode = HttpStatusCode.NoContent;
                expected = "";
                expectedBytes = Encoding.UTF8.GetBytes(expected);
            }

            if (url == "404")
            {
                StatusCode = HttpStatusCode.NotFound;
                expected = "404 response content";
                expectedBytes = Encoding.UTF8.GetBytes(expected);
                throw new WebException("The remote server returned an error: (404) Not Found.");
            }

            if (url == "500")
            {
                StatusCode = HttpStatusCode.InternalServerError;
                expected = "500 response content";
                expectedBytes = Encoding.UTF8.GetBytes(expected);
                throw new WebException("The remote server returned an error: (500) Internal Server error.");
            }

            var responseStream = new MemoryStream();
            responseStream.Write(expectedBytes, 0, expectedBytes.Length);
            responseStream.Seek(0, SeekOrigin.Begin);

            _stream = responseStream;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
        }

        public Stream GetResponseStream()
        {
            return _stream;
        }
    }
}
