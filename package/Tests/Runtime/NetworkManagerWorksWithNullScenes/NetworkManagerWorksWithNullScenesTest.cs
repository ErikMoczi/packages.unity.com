using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine.Networking; 

public class NetworkManagerWorksWithNullScenesTest
{    
    [UnityTest]
    public IEnumerator TestNetworkManageNullScenes ()
    {
		NetworkServer.Reset();
		NetworkClient.ShutdownAll();
		
        var networkManagerObj = new GameObject();
		NetworkManager nmanager = networkManagerObj.AddComponent<NetworkManager>();
		nmanager.playerPrefab = Resources.Load("CleanPlayerPrefab", typeof(GameObject)) as GameObject;

		nmanager.offlineScene = null;
		nmanager.onlineScene = null;

        NetworkServer.Reset();
		
        if (!nmanager.isNetworkActive)
        {
			nmanager.StartHost();
            yield return null; 
        }

        Assert.IsTrue(nmanager.isNetworkActive, 
                      "Network is not active.");

		nmanager.StopHost();
		GameObject.Destroy(networkManagerObj);
	}
}
