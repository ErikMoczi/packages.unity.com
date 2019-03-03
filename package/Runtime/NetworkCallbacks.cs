using UnityEngine;
using UnityEngine.Networking;

public class NetworkCallbacks : MonoBehaviour
{
    void LateUpdate()
    {
        NetworkIdentity.UNetStaticUpdate();
    }
}