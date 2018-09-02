#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using System.IO;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Serialized data containing the asset bundle layout.
    /// </summary>
    [Serializable]
    public class VirtualAssetBundleRuntimeData
    {
        [SerializeField]
        List<VirtualAssetBundle> m_simulatedAssetBundles = new List<VirtualAssetBundle>();
        [SerializeField]
        long m_remoteLoadSpeed = 1024 * 100;
        [SerializeField]
        long m_localLoadSpeed = 1024 * 1024 * 10;
        /// <summary>
        /// The list of asset bundles to simulate.
        /// </summary>
        public List<VirtualAssetBundle> AssetBundles { get { return m_simulatedAssetBundles; } }
        /// <summary>
        /// Bandwidth value (in bytes per second) to simulate loading from a remote location.
        /// </summary>
        public long RemoteLoadSpeed { get { return m_remoteLoadSpeed; } }
        /// <summary>
        /// Bandwidth value (in bytes per second) to simulate loading from a local location.
        /// </summary>
        public long LocalLoadSpeed { get { return m_localLoadSpeed; } }

        /// <summary>
        /// Construct a new VirtualAssetBundleRuntimeData object.
        /// </summary>
        public VirtualAssetBundleRuntimeData() { }
        /// <summary>
        /// Construct a new VirtualAssetBundleRuntimeData object.
        /// </summary>
        /// <param name="localSpeed">Bandwidth value (in bytes per second) to simulate loading from a local location.</param>
        /// <param name="remoteSpeed">Bandwidth value (in bytes per second) to simulate loading from a remote location.</param>
        public VirtualAssetBundleRuntimeData(long localSpeed, long remoteSpeed)
        {
            m_localLoadSpeed = localSpeed;
            m_remoteLoadSpeed = remoteSpeed;
        }

        const string LibraryLocation = "Library/com.unity.addressables/VirtualAssetBundleData.json";
        /// <summary>
        /// Load the runtime data for the virtual bundles.  This is loaded from Library/com.unity.addressables/VirtualAssetBundleData.json.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Save to the virtual bundle data to Library/com.unity.addressables/VirtualAssetBundleData.json.
        /// </summary>
        public void Save()
        {
            var data = JsonUtility.ToJson(this);
            if (!Directory.Exists(Path.GetDirectoryName(LibraryLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryLocation));
            File.WriteAllText(LibraryLocation, data);
        }

        /// <summary>
        /// Delete any existing virtual bundle runtime data. 
        /// </summary>
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