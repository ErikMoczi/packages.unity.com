using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;
using UnityEngine.Windows;

class NetworkManagerSpawnSpecialPrefab
{
    [UnityTest]
    public IEnumerator NetworkManagerSpawnSpecialPrefabTest()
    {
        GameObject obj = new GameObject("NetworkManagerSpawnSpecialPrefab_player");
        var netId = obj.AddComponent<NetworkIdentity>();
        // Certain conditions can lead to a prefab containing a set scene ID
        // for example if you set up a scene object linked to a prefab, start playmode (which
        // assigns a new scene ID) and then click apply changes to prefab on the scene object
        netId.ForceSceneId(1);
        obj.AddComponent<CharacterController>();
        var netTransform = obj.AddComponent<NetworkTransform>();
        netTransform.transformSyncMode = NetworkTransform.TransformSyncMode.SyncCharacterController;
        obj.AddComponent<NetworkManagerSpawnSpecialPrefabObject>();
        var prefab = PrefabUtility.CreatePrefab("Assets/UNetManagerSpawnSpecialPrefab.prefab", obj, ReplacePrefabOptions.ConnectToPrefab);
        GameObject.DestroyImmediate(obj);

        obj = new GameObject("NetworkManagerSpawnerScript");
        var manager = obj.AddComponent<NetworkManagerSpawnerScript>();
        manager.playerPrefab = prefab;

        NetworkManager.singleton.StartHost();
        
        DateTime timelimit = DateTime.Now;
        while (!NetworkManagerSpawnerScript.serverReady)
        {
            if ((DateTime.Now - timelimit).TotalSeconds > 30)
            {
                Assert.Fail("Network manager didn't get to ready state");
            }
            yield return null;
        }

        // If invalid scene ID (forced to 1) has not been corrected in the prefab we have a problem (the bug this test covers only happened on standalone players)
        if (!NetworkManagerSpawnSpecialPrefabObject.didSpawnWithValidSceneId)
        {
            Assert.Fail("Server ready but scene ID is invalid.");
        }

        NetworkManager.singleton.StopServer();
        GameObject.DestroyImmediate(obj);
        File.Delete("Assets/UNetManagerSpawnSpecialPrefab.prefab");
    }

    public class NetworkManagerSpawnerScript : NetworkManager
    {
        public static bool serverReady;

        public override void OnServerReady(NetworkConnection conn)
        {
            base.OnServerReady(conn);
            serverReady = true;
        }
    }

    public class NetworkManagerSpawnSpecialPrefabObject : NetworkBehaviour
    {
        public static bool didSpawnWithValidSceneId;

        public override void OnStartServer()
        {
            // The scene ID was forced to 1 on the prefab, it should have been corrected to 0 on this intantiated copy of that prafab
            if (GetComponent<NetworkIdentity>().sceneId.Value == 0)
            {
                didSpawnWithValidSceneId = true;
            }
        }
    }
}
