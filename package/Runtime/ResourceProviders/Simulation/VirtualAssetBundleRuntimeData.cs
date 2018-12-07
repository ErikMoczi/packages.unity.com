#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Serialization;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Serialized data containing the asset bundle layout.
    /// </summary>
    [Serializable]
    public class VirtualAssetBundleRuntimeData
    {
        [FormerlySerializedAs("m_simulatedAssetBundles")]
        [SerializeField]
        List<VirtualAssetBundle> m_SimulatedAssetBundles = new List<VirtualAssetBundle>();
        [FormerlySerializedAs("m_remoteLoadSpeed")]
        [SerializeField]
        long m_RemoteLoadSpeed = 1024 * 100;
        [FormerlySerializedAs("m_localLoadSpeed")]
        [SerializeField]
        long m_LocalLoadSpeed = 1024 * 1024 * 10;
        /// <summary>
        /// The list of asset bundles to simulate.
        /// </summary>
        public List<VirtualAssetBundle> AssetBundles { get { return m_SimulatedAssetBundles; } }
        /// <summary>
        /// Bandwidth value (in bytes per second) to simulate loading from a remote location.
        /// </summary>
        public long RemoteLoadSpeed { get { return m_RemoteLoadSpeed; } }
        /// <summary>
        /// Bandwidth value (in bytes per second) to simulate loading from a local location.
        /// </summary>
        public long LocalLoadSpeed { get { return m_LocalLoadSpeed; } }

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
            m_LocalLoadSpeed = localSpeed;
            m_RemoteLoadSpeed = remoteSpeed;
        }

        const string k_LibraryLocation = "Library/com.unity.addressables/VirtualAssetBundleData.json";
        /// <summary>
        /// Load the runtime data for the virtual bundles.  This is loaded from Library/com.unity.addressables/VirtualAssetBundleData.json.
        /// </summary>
        /// <returns></returns>
        public static VirtualAssetBundleRuntimeData Load()
        {
            try
            {
                if (!File.Exists(k_LibraryLocation))
                    return null;
                return JsonUtility.FromJson<VirtualAssetBundleRuntimeData>(File.ReadAllText(k_LibraryLocation));
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }

        /// <summary>
        /// Save to the virtual bundle data to Library/com.unity.addressables/VirtualAssetBundleData.json.
        /// </summary>
        public void Save()
        {
            var data = JsonUtility.ToJson(this);
            var dirName = Path.GetDirectoryName(k_LibraryLocation);
            if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);
            File.WriteAllText(k_LibraryLocation, data);
        }

        /// <summary>
        /// Delete any existing virtual bundle runtime data. 
        /// </summary>
        public static void DeleteFromLibrary()
        {
            try
            {
                if (File.Exists(k_LibraryLocation))
                    File.Delete(k_LibraryLocation);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
#endif