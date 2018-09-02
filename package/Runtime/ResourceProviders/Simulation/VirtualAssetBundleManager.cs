#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class VirtualAssetBundleManager : MonoBehaviour
    {
        Dictionary<string, VirtualAssetBundle> m_allBundles = new Dictionary<string, VirtualAssetBundle>();
        Dictionary<string, LoadAssetBundleOp> m_loadBundleOperations = new Dictionary<string, LoadAssetBundleOp>();
        Dictionary<string, VirtualAssetBundle> m_updatingBundles = new Dictionary<string, VirtualAssetBundle>();

        uint m_remoteLoadSpeed = 1024 * 100; //100 KB per second
        uint m_localLoadSpeed = 1024 * 1024 * 10; //10 MB per second

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private bool Unload(string id)
        {
            VirtualAssetBundle bundle = null;
            if(!m_allBundles.TryGetValue(id, out bundle))
                Debug.LogWarning("Simulated assetbundle " + id + " not found.");
            return bundle.Unload();
        }

        private VirtualAssetBundle Load(string id)
        {
            VirtualAssetBundle bundle = null;
            if (!m_allBundles.TryGetValue(id, out bundle))
                Debug.LogWarning("Simulated assetbundle " + id + " not found.");
            return bundle.Load(this);
        }

        class LoadAssetBundleOp : AsyncOperationBase<VirtualAssetBundle>
        {
            VirtualAssetBundleManager manager;
            string bundleName;
            float loadTime;
            public LoadAssetBundleOp(VirtualAssetBundleManager manager, IResourceLocation location, float delay)
            {
                this.manager = manager;
                Context = location;
                Acquire();
                bundleName = location.InternalId;
                loadTime = Time.unscaledTime + delay;
            }

            public bool Update()
            {
                Validate();
                if (Time.unscaledTime > loadTime)
                {
                    Result = manager.Load(bundleName);
                    InvokeCompletionEvent();
                    return false;
                }
                return true;
            }
        }

        public static void AddProviders(Func<string, string> bundleNameConverter)
        {
            var virtualBundleData = VirtualAssetBundleRuntimeData.Load();
            if (virtualBundleData != null)
            {
                var go = new GameObject("AssetBundleSimulator", typeof(VirtualAssetBundleManager));
                var simABManager = go.GetComponent<VirtualAssetBundleManager>();
                simABManager.Initialize(virtualBundleData, bundleNameConverter);
                ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new VirtualAssetBundleProvider(simABManager, typeof(RemoteAssetBundleProvider).FullName)));
                ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new VirtualAssetBundleProvider(simABManager, typeof(LocalAssetBundleProvider).FullName)));
                ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new VirtualBundledAssetProvider(simABManager.m_localLoadSpeed)));
            }
        }

        private void Initialize(VirtualAssetBundleRuntimeData virtualBundleData, Func<string, string> bundleNameConverter)
        {
            Debug.Assert(virtualBundleData != null);
            m_localLoadSpeed = virtualBundleData.LocalLoadSpeed;
            m_remoteLoadSpeed = virtualBundleData.RemoteLoadSpeed;
            foreach (var b in virtualBundleData.AssetBundles)
                m_allBundles.Add(bundleNameConverter(b.Name), b);
        }

        float GetBundleLoadTime(string id)
        {
            return m_allBundles[id].GetLoadTime(m_localLoadSpeed, m_remoteLoadSpeed);
        }

        public bool Unload(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");
            return Unload(location.InternalId);
        }

        public IAsyncOperation<VirtualAssetBundle> LoadAsync(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");

            LoadAssetBundleOp op = null;
            var bundleName = location.InternalId;
            if (!m_loadBundleOperations.TryGetValue(bundleName, out op))
                m_loadBundleOperations.Add(bundleName, op = new LoadAssetBundleOp(this, location, GetBundleLoadTime(bundleName)));
            return op;
        }

        public void AddToUpdateList(VirtualAssetBundle bundle)
        {
            if (bundle == null)
                throw new ArgumentException("VirtualAssetBundle bundle cannot be null.");
            if (!m_updatingBundles.ContainsKey(bundle.Name))
                m_updatingBundles.Add(bundle.Name, bundle);
        }

        public void Update()
        {
            foreach (var o in m_loadBundleOperations)
            {
                if (!o.Value.Update())
                {
                    m_loadBundleOperations.Remove(o.Key);
                    break;
                }
            }
            foreach (var b in m_updatingBundles)
            {
                if (!b.Value.UpdateAsyncOperations())
                {
                    m_updatingBundles.Remove(b.Key);
                    break;
                }
            }
        }
    }
}
#endif
