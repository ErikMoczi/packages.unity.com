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
        long m_remoteLoadSpeed = 1024 * 100;
        [SerializeField]
        long m_localLoadSpeed = 1024 * 1024 * 10;
        public List<VirtualAssetBundle> AssetBundles { get { return m_simulatedAssetBundles; } }
        public long RemoteLoadSpeed { get { return m_remoteLoadSpeed; } }
        public long LocalLoadSpeed { get { return m_localLoadSpeed; } }

        public VirtualAssetBundleRuntimeData() {}
        public VirtualAssetBundleRuntimeData(long localSpeed, long remoteSpeed)
        {
            m_localLoadSpeed = localSpeed;
            m_remoteLoadSpeed = remoteSpeed;
        }

        const string LibraryLocation = "Library/VirtualAssetBundleData.json";
        public static VirtualAssetBundleRuntimeData Load()
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

        public void Save()
        {
            var data = JsonUtility.ToJson(this);
            if (!Directory.Exists(Path.GetDirectoryName(LibraryLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryLocation));
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
    }
}
#endif