#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class VirtualAssetBundleManager : MonoBehaviour
    {
        Dictionary<string, VirtualAssetBundle> m_allBundles = new Dictionary<string, VirtualAssetBundle>();
        Dictionary<string, VirtualAssetBundle> m_activeBundles = new Dictionary<string, VirtualAssetBundle>();

        long m_remoteLoadSpeed = 1024 * 100; //100 KB per second
        long m_localLoadSpeed = 1024 * 1024 * 10; //10 MB per second

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
 
        public static void AddProviders(Func<string, string> bundleNameConverter, int assetCacheSize, float assetCacheAge, int bundleCacheSize, float bundleCacheAge)
        {
            var virtualBundleData = VirtualAssetBundleRuntimeData.Load();
            if (virtualBundleData != null)
                new GameObject("AssetBundleSimulator", typeof(VirtualAssetBundleManager)).GetComponent<VirtualAssetBundleManager>().Initialize(virtualBundleData, bundleNameConverter, assetCacheSize, assetCacheAge, bundleCacheSize, bundleCacheAge);
        }


        public void Initialize(VirtualAssetBundleRuntimeData virtualBundleData, Func<string, string> bundleNameConverter, int assetCacheSize, float assetCacheAge, int bundleCacheSize, float buncleCacheAge)
        {
            Debug.Assert(virtualBundleData != null);
            m_localLoadSpeed = virtualBundleData.LocalLoadSpeed;
            m_remoteLoadSpeed = virtualBundleData.RemoteLoadSpeed;
            foreach (var b in virtualBundleData.AssetBundles)
                m_allBundles.Add(bundleNameConverter(b.Name), b);
            ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new VirtualAssetBundleProvider(this, typeof(AssetBundleProvider).FullName), bundleCacheSize, buncleCacheAge));
            ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new VirtualBundledAssetProvider(), assetCacheSize, assetCacheAge));
        }

        public bool Unload(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");

            VirtualAssetBundle bundle = null;
            if (!m_allBundles.TryGetValue(location.InternalId, out bundle))
            {
                Debug.LogWarningFormat("Unable to unload virtual bundle {0}.", location);
                return false;
            }

            m_activeBundles.Remove(location.InternalId);
            return bundle.Unload();
        }

        public IAsyncOperation<VirtualAssetBundle> LoadAsync(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");
            VirtualAssetBundle bundle;
            if (!m_allBundles.TryGetValue(location.InternalId, out bundle))
                return new EmptyOperation<VirtualAssetBundle>().Start(location, default(VirtualAssetBundle), new Exception(string.Format("Unable to unload virtual bundle {0}.", location)));

            m_activeBundles.Add(location.InternalId, bundle);
            return bundle.StartLoad(location);
        }

        public void Update()
        {
            long localCount = 0;
            long remoteCount = 0;
            foreach (var b in m_activeBundles)
                b.Value.CountBandwidthUsage(ref localCount, ref remoteCount);

            long localBW = localCount > 1 ? (m_localLoadSpeed / localCount) : m_localLoadSpeed;
            long remoteBW = remoteCount > 1 ? (m_remoteLoadSpeed / remoteCount) : m_remoteLoadSpeed;
            foreach (var b in m_activeBundles)
                b.Value.UpdateAsyncOperations(localBW, remoteBW);
        }
    }
}
#endif
