using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

namespace ResourceManagement.ResourceProviders.Simulation
{
    [Serializable]
    public class VirtualAssetBundleRuntimeData
    {
        public List<VirtualAssetBundle> simulatedAssetBundles = new List<VirtualAssetBundle>();
        public string[] sceneGUIDS;
        public int remoteLoadSpeed = 1024 * 100;
        public int localLoadSpeed = 1024 * 1024 * 10;
        const string PlayerLocation = "Assets/StreamingAssets/VirtualAssetBundleData.json";
        public VirtualAssetBundleRuntimeData() {}
        public VirtualAssetBundleRuntimeData(int localSpeed, int remoteSpeed)
        {
            localLoadSpeed = localSpeed;
            remoteLoadSpeed = remoteSpeed;
        }

        public static VirtualAssetBundleRuntimeData Load()
        {
            try
            {
                if (!File.Exists(PlayerLocation))
                    return null;
                return JsonUtility.FromJson<VirtualAssetBundleRuntimeData>(File.ReadAllText(PlayerLocation));
            }
            catch (Exception e)
            {
                Debug.Log("Unable to load VirtualAssetBundleData from " + PlayerLocation + ", Exception: " + e);
            }
            return null;
        }

#if UNITY_EDITOR
        const string LibraryLocation = "Library/VirtualAssetBundleData.json";
        public static VirtualAssetBundleRuntimeData LoadFromLibrary()
        {
            try
            {
                if (!File.Exists(LibraryLocation))
                    return null;
                return JsonUtility.FromJson<VirtualAssetBundleRuntimeData>(File.ReadAllText(LibraryLocation));
            }
            catch (Exception)
            {
            }
            return null;
        }

        public static void Cleanup()
        {
            if (File.Exists(PlayerLocation))
                File.Delete(PlayerLocation);
        }

        public void Save()
        {
            var data = JsonUtility.ToJson(this);
            if (!Directory.Exists(Path.GetDirectoryName(PlayerLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(PlayerLocation));
            if (!Directory.Exists(Path.GetDirectoryName(LibraryLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryLocation));
            File.WriteAllText(PlayerLocation, data);
            File.WriteAllText(LibraryLocation, data);
        }

#endif
    }
}
