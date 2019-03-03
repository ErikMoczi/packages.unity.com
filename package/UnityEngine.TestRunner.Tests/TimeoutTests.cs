using UnityEngine.TestTools;
using System.Collections;
using FrameworkTests;
using NUnit.Framework;

#if !UNITY_WSA
[Timeout(200)]
[Ignore("TimeOut dont work properly, we should find another type that we can support at documented")]
public class TimeoutTests
{
    [UnityTest]
    [Timeout(200)]
    public IEnumerator UnityTestWillFailOnSetTimeout()
    {
        while (true)
            yield return null;
    }

    [UnityTest]
    [Timeout(100)]
    public IEnumerator UnityTestWillFailWithDefaultTimeout()
    {
        while (true)
            yield return null;
    }
}
#endif
