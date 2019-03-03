using UnityEngine;
using UnityEngine.Networking;

namespace UnityEngine.Networking
{
    public class NetworkCallbacks : MonoBehaviour
    {
        void LateUpdate()
        {
            NetworkIdentity.UNetStaticUpdate();
        }
    }
}
