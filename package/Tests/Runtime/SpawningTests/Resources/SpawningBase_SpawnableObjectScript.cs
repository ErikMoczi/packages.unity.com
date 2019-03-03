using UnityEngine.Networking;

public class SpawningBase_SpawnableObjectScript : NetworkBehaviour
{
    public override void OnStartServer()
    {
        SpawningTestBase.IncrementStartServer();
    }

    public override void OnStartClient()
    {
        SpawningTestBase.IncrementStartClient();
    }

    public override void OnNetworkDestroy()
    {
        SpawningTestBase.IncrementDestroyClient();
    }
}
