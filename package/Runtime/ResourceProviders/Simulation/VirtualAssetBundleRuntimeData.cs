#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using System.IO;

namespace UnityEngine.ResourceManagement
{
    [Serializable]
    public class VirtualAssetBundleRuntimeData
    {
        [SerializeField]
        List<VirtualAssetBundle> m_simulatedAssetBundles = new List<VirtualAssetBundle>();
        [SerializeField]
        int m_remoteLoadSpeed = 1024 * 100;
        [SerializeField]
        int m_localLoadSpeed = 1024 * 1024 * 10;
        public static string PlayerLocation { get { return Path.Combine(Application.streamingAssetsPath, "VirtualAssetBundleData.json").Replace('\\', '/'); } }
        public IList<VirtualAssetBundle> AssetBundles { get { return m_simulatedAssetBundles; } }
        public int RemoteLoadSpeed { get { return m_remoteLoadSpeed; } }
        public int LocalLoadSpeed { get { return m_localLoadSpeed; } }

        public VirtualAssetBundleRuntimeData() {}
        public VirtualAssetBundleRuntimeData(int localSpeed, int remoteSpeed)
        {
            m_localLoadSpeed = localSpeed;
            m_remoteLoadSpeed = remoteSpeed;
        }

        public static VirtualAssetBundleRuntimeData Load()
        {
            try
            {
                if (!File.Exists(PlayerLocation))
                    return null;
                return JsonUtility.FromJson<VirtualAssetBundleRuntimeData>(File.ReadAllText(PlayerLocation));
            }
            catch (Exception)
            {
            }
            return null;
        }

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
            {
                File.Delete(PlayerLocation);
                var metaFile = PlayerLocation + ".meta";
                if (File.Exists(metaFile))
                    System.IO.File.Delete(metaFile);
            }
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

        public static void DeleteFromLibrary()
        {
            try
            {
                if (File.Exists(LibraryLocation))
                    File.Delete(LibraryLocation);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static bool CopyFromLibraryToPlayer()
        {
            try
            {
                if (!File.Exists(LibraryLocation))
                    return false;

                if (File.Exists(PlayerLocation))
                    File.Delete(PlayerLocation);

                var dirName = Path.GetDirectoryName(PlayerLocation);
                if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);
                File.Copy(LibraryLocation, PlayerLocation);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

    }
}
#endif