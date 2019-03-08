using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using Assert = UnityEngine.Assertions.Assert;

[UnityPlatform(include = new[] { RuntimePlatform.Lumin })]
public class LuminSmokeTest : EnableXRPrebuildStep
{
    [UnityTest]
    [Explicit] // Added to ensure this is only run against Magic Leap Devices
               // Requires that --testFilter=LuminSmokeTest parameter is used to run.
    public IEnumerator CanDeployAndRunOnLuminDevice()
    {
        yield return new MonoBehaviourTest<LuminMonoBehaviourTest>();
    }
}

public class LuminMonoBehaviourTest : MonoBehaviour, IMonoBehaviourTest
{
    public bool IsTestFinished { get; private set; }

    void Awake()
    {
        Assert.IsTrue(XRSettings.enabled);
        Assert.AreEqual("lumin", XRSettings.loadedDeviceName);
        IsTestFinished = true;
    }
}
