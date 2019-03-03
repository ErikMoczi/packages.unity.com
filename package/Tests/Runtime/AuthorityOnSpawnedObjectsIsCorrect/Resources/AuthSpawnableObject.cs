using NUnit.Framework;
using UnityEngine.Networking;

public class AuthSpawnableObject : NetworkBehaviour
{
    // this object is spawned with client Authority
    public override void OnStartAuthority()
    {
        Assert.IsTrue(hasAuthority);
    }

    public override void OnStopAuthority()
    {
        Assert.Fail("OnStopAuthority on AuthSpawnableObject should not be called");
    }
}
