using System;
using System.IO;
using System.Net;

public interface IHttpWebResponse : IDisposable
{
    HttpStatusCode StatusCode { get; set; }

    Stream GetResponseStream();
}
