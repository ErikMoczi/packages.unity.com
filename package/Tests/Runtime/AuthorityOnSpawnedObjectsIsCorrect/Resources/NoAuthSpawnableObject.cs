using NUnit.Framework;
using UnityEngine.Networking;

public class NoAuthSpawnableObject : NetworkBehaviour
{
    // this object is spawned without client Authority, then set
    public override void OnStartClient()
    {
        Assert.IsFalse(hasAuthority);
    }

    public override void OnStartAuthority()
    {
        Assert.IsTrue(hasAuthority);
    }

    public override void OnStopAuthority()
    {
        Assert.IsFalse(hasAuthority);
        AuthorityOnSpawnedObjectsIsCorrect.isTestDone = true;
    }
}
