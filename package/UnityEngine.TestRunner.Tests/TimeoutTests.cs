using UnityEngine.TestTools;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

#if !UNITY_WSA
public class TimeoutTests
{
    [UnityTest]
    [Timeout(200)]
    [Ignore("TimeOut dont work properly, we should find another type that we can support at documented")]
    public IEnumerator UnityTestWillFailOnSetTimeout()
    {
        while (true)
            yield return null;
    }

    [UnityTest]
    [Timeout(100)]
    [Ignore("TimeOut dont work properly, we should find another type that we can support at documented")]
    public IEnumerator UnityTestWillFailWithDefaultTimeout()
    {
        while (true)
            yield return null;
    }

    [UnityTest]
    public IEnumerator TimeoutShouldUseRealTime()
    {
        Time.timeScale = 100.0f;
        const int defaultTimeoutInSeconds = CoroutineRunner.k_DefaultTimeout / 1000;
        yield return new WaitForSecondsRealtime(1.0f + defaultTimeoutInSeconds / 100.0f);
    }
}
#endif
