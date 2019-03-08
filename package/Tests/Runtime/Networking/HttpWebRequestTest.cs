using System;
using System.Collections;
using System.Net;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using Lumin.Common;

using Assert = UnityEngine.Assertions.Assert;
using NAssert = NUnit.Framework.Assert;

public class HttpWebRequestTest
{
#if PLATFORM_LUMIN
    [UnityTest]
    public IEnumerator HttpWebRequest_HttpRequest_Works()
    {
        var test = TryToLoadUrl("http://www.example.com");
        while (test.MoveNext()) yield return test.Current;
    }

    [UnityTest]
    public IEnumerator HttpWebRequest_HttpRequest_WorksWithGzip()
    {
        var test = VerifyCompressionSupport("http://www.example.com");
        while (test.MoveNext()) yield return test.Current;
    }

    [UnityTest]
    public IEnumerator HttpWebRequest_HttpsRequest_Works()
    {
        var test = TryToLoadUrl("https://www.google.com");
        while (test.MoveNext()) yield return test.Current;
    }

    [UnityTest]
    public IEnumerator HttpWebRequest_HttpsRequest_WorksWithGzip()
    {
        var test = VerifyCompressionSupport("https://www.google.com");
        while (test.MoveNext()) yield return test.Current;
    }

    private IEnumerator TryToLoadUrl(string url)
    {
        yield return new WaitForEndOfFrame();
        var request = (HttpWebRequest)WebRequest.Create(url);
        var result = request.BeginGetResponse(null, null);

        yield return new WaitForIAsyncResult(result);

        NAssert.DoesNotThrow(() =>
            {
                using (var response = (HttpWebResponse)request.EndGetResponse(result))
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Unexpected HTTP response code");
                }
            });
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator VerifyCompressionSupport(string url)
    {
        yield return new WaitForEndOfFrame();
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.AutomaticDecompression = DecompressionMethods.GZip;
        var result = request.BeginGetResponse(null, null);

        yield return new WaitForIAsyncResult(result);

        NAssert.DoesNotThrow(() =>
            {
                using (var response = (HttpWebResponse)request.EndGetResponse(result))
                {
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Unexpected HTTP response code");
                    var encoding = response.ContentEncoding.ToLower();
                    Assert.IsTrue(encoding.Contains("gzip"), string.Format("Expected: {0}, Actual: {1}", "gzip", encoding));
                }
            });
        yield return new WaitForEndOfFrame();
    }
#endif // PLATFORM_LUMIN
}
