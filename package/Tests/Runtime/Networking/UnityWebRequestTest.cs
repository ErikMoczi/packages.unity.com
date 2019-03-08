using System.Collections;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using Assert = UnityEngine.Assertions.Assert;
using NAssert = NUnit.Framework.Assert;

public class UnityWebRequestTest
{
#if PLATFORM_LUMIN
    [UnityTest]
    public IEnumerator UnityWebRequest_HttpRequest_Works()
    {
        var test = TryToLoadUrl("http://www.example.com");
        while (test.MoveNext()) yield return test.Current;
    }

    [UnityTest]
    public IEnumerator UnityWebRequest_HttpRequest_WorksWithGzip()
    {
        var test = VerifyCompressionSupport("http://www.example.com");
        while (test.MoveNext()) yield return test.Current;
    }

    [UnityTest]
    public IEnumerator UnityWebRequest_HttpsRequest_Works()
    {
        var test = TryToLoadUrl("https://www.unity3d.com");
        while (test.MoveNext()) yield return test.Current;
    }

    [UnityTest]
    public IEnumerator UnityWebRequest_HttpsRequest_WorksWithGzip()
    {
        var test = VerifyCompressionSupport("https://www.unity3d.com");
        while (test.MoveNext()) yield return test.Current;
    }

    private IEnumerator TryToLoadUrl(string url)
    {
        yield return new WaitForEndOfFrame();
        var www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        Assert.IsFalse(www.isHttpError || www.isNetworkError, www.error);
        Assert.AreEqual(www.responseCode, 200, "Unexpected HTTP response code");
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator VerifyCompressionSupport(string url)
    {
        yield return new WaitForEndOfFrame();
        var www = UnityWebRequest.Get(url);
        //var requestEncodings = www.GetRequestHeader("Accept-Encoding").ToLower();
        //Assert.IsTrue(requestEncodings.Contains("gzip"), string.Format("Expected: {0}, Actual: {1}", "gzip", requestEncodings));
        yield return www.SendWebRequest();

        Assert.IsFalse(www.isHttpError || www.isNetworkError, www.error);
        Assert.AreEqual(www.responseCode, 200, "Unexpected HTTP response code");
        var encoding = www.GetResponseHeader("Content-Encoding").ToLower();
        Assert.IsTrue(encoding.Contains("gzip"), string.Format("Expected: {0}, Actual: {1}", "gzip", encoding));
        Assert.AreEqual(www.responseCode, 200, "Unexpected HTTP response code");
        yield return new WaitForEndOfFrame();
    }
#endif // PLATFORM_LUMIN
}
