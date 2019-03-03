using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WeaverILGenerationTests_TargetRPCServerClientChecks : NetworkBehaviour
{
    [TargetRpc]
    public void TargetRpcTest(NetworkConnection connection)
    {
    }

    [ClientRpc]
    public void RpcWithEnumArray(System.AttributeTargets[] array)
    {
    }
}
