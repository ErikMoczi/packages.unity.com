using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.AsyncOperations;
using ResourceManagement.Util;
namespace ResourceManagement.ResourceProviders.Simulation
{
    public class VirtualAssetBundleManager : MonoBehaviour
    {
        Dictionary<string, VirtualAssetBundle> m_allBundles = new Dictionary<string, VirtualAssetBundle>();
        Dictionary<string, LoadAssetBundleOp> m_loadBundleOperations = new Dictionary<string, LoadAssetBundleOp>();
        Dictionary<string, VirtualAssetBundle> m_updatingBundles = new Dictionary<string, VirtualAssetBundle>();

        public int m_remoteLoadSpeed = 1024 * 100; //100 KB per second
        public int m_localLoadSpeed = 1024 * 1024 * 10; //10 MB per second

        internal void SetBundles(List<VirtualAssetBundle> bundles)
        {
            foreach (var b in bundles)
            {
                b.name = Config.ExpandPathWithGlobalVars(b.name);
                m_allBundles.Add(b.name, b);
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private bool Unload(string id)
        {
            VirtualAssetBundle vab = null;
            if(!m_allBundles.TryGetValue(id, out vab))
                Debug.LogWarning("Simulated assetbundle " + id + " not found.");
            if(!vab.loaded)
                Debug.LogWarning("Simulated assetbundle " + id + " is already unloaded.");
            vab.loaded = false;
            return true;
        }

        private VirtualAssetBundle Load(string id)
        {
            VirtualAssetBundle vab = null;
            if (!m_allBundles.TryGetValue(id, out vab))
                Debug.LogWarning("Simulated assetbundle " + id + " not found.");
            if (vab.loaded)
                Debug.LogWarning("Simulated assetbundle " + id + " is already loaded.");
            vab.m_manager = this;
            vab.loaded = true;
            return vab;
        }

        class LoadAssetBundleOp : AsyncOperationBase<VirtualAssetBundle>
        {
            VirtualAssetBundleManager manager;
            string bundleName;
            float loadTime;
            public LoadAssetBundleOp(VirtualAssetBundleManager mgr, IResourceLocation loc, float delay)
            {
                manager = mgr;
                m_context = loc;
                bundleName = Config.ExpandPathWithGlobalVars(loc.id);
                loadTime = Time.unscaledTime + delay;
            }

            public bool Update()
            {
                if (Time.unscaledTime > loadTime)
                {
                    m_result = manager.Load(bundleName);
                    InvokeCompletionEvent();
                    return false;
                }
                return true;
            }
        }

        public static void AddProviders()
        {
            var virtualBundleData = JsonUtility.FromJson<VirtualAssetBundleRuntimeData>(System.IO.File.ReadAllText(Application.streamingAssetsPath + "/VirtualAssetBundleData.json"));
            if (virtualBundleData != null)
            {
                var go = new GameObject("AssetBundleSimulator", typeof(VirtualAssetBundleManager));
                var simABManager = go.GetComponent<VirtualAssetBundleManager>();
                simABManager.m_localLoadSpeed = virtualBundleData.localLoadSpeed;
                simABManager.m_remoteLoadSpeed = virtualBundleData.remoteLoadSpeed;
                simABManager.SetBundles(virtualBundleData.simulatedAssetBundles);
                ResourceManager.resourceProviders.Insert(0, new CachedProvider(new VirtualAssetBundleProvider(simABManager, typeof(RemoteAssetBundleProvider).FullName)));
                ResourceManager.resourceProviders.Insert(0, new CachedProvider(new VirtualAssetBundleProvider(simABManager, typeof(LocalAssetBundleProvider).FullName)));
                ResourceManager.resourceProviders.Insert(0, new CachedProvider(new VirtualBundledAssetProvider(simABManager.m_localLoadSpeed)));
            }
        }

        float GetBundleLoadTime(string id)
        {
            return m_allBundles[id].GetLoadTime(m_localLoadSpeed, m_remoteLoadSpeed);
        }

        public bool Unload(IResourceLocation loc)
        {
            return Unload(Config.ExpandPathWithGlobalVars(loc.id));
        }

        public IAsyncOperation<VirtualAssetBundle> LoadAsync(IResourceLocation loc)
        {
            LoadAssetBundleOp op = null;
            var bundleName = Config.ExpandPathWithGlobalVars(loc.id);
            if (!m_loadBundleOperations.TryGetValue(bundleName, out op))
                m_loadBundleOperations.Add(bundleName, op = new LoadAssetBundleOp(this, loc, GetBundleLoadTime(bundleName)));
            return op;
        }

        public void AddToUpdateList(VirtualAssetBundle b)
        {
            if (!m_updatingBundles.ContainsKey(b.name))
                m_updatingBundles.Add(b.name, b);
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
