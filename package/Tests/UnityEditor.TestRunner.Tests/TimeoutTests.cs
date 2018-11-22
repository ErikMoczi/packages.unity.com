using UnityEngine.TestTools;
using System.Collections;
using NUnit.Framework;

[Timeout(200)]
[Ignore("TimeOut are not currently supported, but we will add a standin for this, and maybe remove Timeout")]
[Description("Timeouts are currently not supported in EditMode but we want to test if they don't break the runner.")]
public class TimeoutTests
{
    [UnityTest]
    [Timeout(100)]
    public IEnumerator UnityTestWithTimeout()
    {
        yield return null;
    }

    [Test]
    [Timeout(100)]
    public void TestWithTimeout()
    {
    }
}
