#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides a simulation layer for asset bundles in order to decrease iteration times.
    /// </summary>
    public class VirtualAssetBundleManager : MonoBehaviour
    {
        Dictionary<string, VirtualAssetBundle> m_AllBundles = new Dictionary<string, VirtualAssetBundle>();
        Dictionary<string, VirtualAssetBundle> m_ActiveBundles = new Dictionary<string, VirtualAssetBundle>();

        long m_RemoteLoadSpeed = 1024 * 100; //100 KB per second
        long m_LocalLoadSpeed = 1024 * 1024 * 10; //10 MB per second

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Loads runtime data and creates a VirtualAssetBundleManager object.
        /// </summary>
        /// <param name="bundleNameConverter">Func used to expand variables in bundle names.</param>
        /// <returns>The created VirtualAssetBundleManager.</returns>
        public static VirtualAssetBundleManager CreateManager(Func<string, string> bundleNameConverter)
        {
            var virtualBundleData = VirtualAssetBundleRuntimeData.Load();
            if (virtualBundleData == null)
                return null;

            var mgr = new GameObject("VirtualAssetBundleManager", typeof(VirtualAssetBundleManager)).GetComponent<VirtualAssetBundleManager>();
            mgr.Initialize(virtualBundleData, bundleNameConverter);
            return mgr;
        }

        /// <summary>
        /// Initialize the VirtualAssetBundleManager object
        /// </summary>
        /// <param name="virtualBundleData">Runtime data that contains the virtual bundles.</param>
        /// <param name="bundleNameConverter">Func used to expand variables in bundle names.</param>
        public void Initialize(VirtualAssetBundleRuntimeData virtualBundleData, Func<string, string> bundleNameConverter)
        {
            Debug.Assert(virtualBundleData != null);
            m_LocalLoadSpeed = virtualBundleData.LocalLoadSpeed;
            m_RemoteLoadSpeed = virtualBundleData.RemoteLoadSpeed;
            foreach (var b in virtualBundleData.AssetBundles)
                m_AllBundles.Add(bundleNameConverter(b.Name), b);
        }

        internal bool Unload(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");

            VirtualAssetBundle bundle;
            if (!m_AllBundles.TryGetValue(location.InternalId, out bundle))
            {
                Debug.LogWarningFormat("Unable to unload virtual bundle {0}.", location);
                return false;
            }
            if (m_UpdatingActiveBundles)
                m_PendingOperations.Add(location.InternalId, null);
            else
                m_ActiveBundles.Remove(location.InternalId);
            return bundle.Unload();
        }

        internal IAsyncOperation<VirtualAssetBundle> LoadAsync(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");
            VirtualAssetBundle bundle;
            if (!m_AllBundles.TryGetValue(location.InternalId, out bundle))
                return new CompletedOperation<VirtualAssetBundle>().Start(location, location, default(VirtualAssetBundle), new ResourceManagerException(string.Format("Unable to unload virtual bundle {0}.", location)));

            if (m_UpdatingActiveBundles)
                m_PendingOperations.Add(location.InternalId, bundle);
            else
                m_ActiveBundles.Add(location.InternalId, bundle);
            return bundle.StartLoad(location);
        }

        bool m_UpdatingActiveBundles;
        Dictionary<string, VirtualAssetBundle> m_PendingOperations = new Dictionary<string, VirtualAssetBundle>();
        internal void Update()
        {
            long localCount = 0;
            long remoteCount = 0;
            foreach (var b in m_ActiveBundles)
                b.Value.CountBandwidthUsage(ref localCount, ref remoteCount);

            long localBw = localCount > 1 ? (m_LocalLoadSpeed / localCount) : m_LocalLoadSpeed;
            long remoteBw = remoteCount > 1 ? (m_RemoteLoadSpeed / remoteCount) : m_RemoteLoadSpeed;
            m_UpdatingActiveBundles = true;
            foreach (var b in m_ActiveBundles)
                b.Value.UpdateAsyncOperations(localBw, remoteBw);
            m_UpdatingActiveBundles = false;
            if (m_PendingOperations.Count > 0)
            {
                foreach (var o in m_PendingOperations)
                {
                    if (o.Value == null)
                        m_ActiveBundles.Remove(o.Key);
                    else
                        m_ActiveBundles.Add(o.Key, o.Value);
                }
                m_PendingOperations.Clear();
            }
        }
    }
}
#endif
