using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WeaverILGenerationTests_SyncLists_Base : NetworkBehaviour
{
    void Awake()
    {
        Debug.Log("just here so compiler does not optimize away this");
    }
}

public class WeaverILGenerationTests_SyncLists : WeaverILGenerationTests_SyncLists_Base
{
    public SyncListInt Inited = new SyncListInt();

    [SyncVar]
    public SyncListInt NotInited;
}
