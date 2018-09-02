using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.AsyncOperations;

namespace ResourceManagement.ResourceProviders.Simulation
{
    public class VirtualAssetBundleManager : MonoBehaviour
    {
        public List<VirtualAssetBundle> bundles = new List<VirtualAssetBundle>();
        List<LoadAssetBundleOp> operations = new List<LoadAssetBundleOp>();
        public int remoteLoadSpeed = 1024 * 100; //100 KB per second
        public int localLoadSpeed = 1024 * 1024 * 10; //10 MB per second

        internal void SetBundles(List<VirtualAssetBundle> b)
        {
            bundles = b;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Unload(string id)
        {
            foreach (var b in bundles)
            {
                if (b.name == id)
                {
                    if (!b.loaded)
                    {
                        Debug.Log("Simulated assetbundle " + id + " is already unloaded.");
                    }
                    else
                    {
                        b.loaded = false;
                    }
                }
            }
        }

        public VirtualAssetBundle Load(string id, bool forceAllowLocal)
        {
            foreach (var b in bundles)
            {
                if (b.name == id)
                {
                    if (!b.isLocal && !forceAllowLocal)
                    {
                        Debug.Log("Simulated assetbundle " + id + " cannot be loaded synchronously.");
                    }
                    else if (b.loaded)
                    {
                        Debug.Log("Simulated assetbundle " + id + " is already loaded.");
                    }
                    else
                    {
                        if (!forceAllowLocal)
                            System.Threading.Thread.Sleep((int)(b.GetLoadTime(localLoadSpeed, remoteLoadSpeed) * 1000));
                        b.loaded = true;
                        return b;
                    }
                }
            }
            return null;
        }

        class LoadAssetBundleOp : AsyncOperationBase<VirtualAssetBundle>
        {
            VirtualAssetBundleManager manager;
            float loadTime;
            public LoadAssetBundleOp(VirtualAssetBundleManager mgr, string id, float delay) : base(id)
            {
                manager = mgr;
                loadTime = Time.unscaledTime + delay;
            }

            public bool Update()
            {
                if (Time.unscaledTime > loadTime)
                {
                    m_result = manager.Load(id, true);
                    InvokeCompletionEvent(this);
                    return false;
                }
                return true;
            }
        }

        internal static void AddProviders()
        {
            var virtualBundleData = JsonUtility.FromJson<VirtualAssetBundleRuntimeData>(System.IO.File.ReadAllText(Application.streamingAssetsPath + "/VirtualAssetBundleData.json"));
            if (virtualBundleData != null)
            {
                var go = new GameObject("AssetBundleSimulator", typeof(VirtualAssetBundleManager));
                var simABManager = go.GetComponent<VirtualAssetBundleManager>();
                simABManager.localLoadSpeed = virtualBundleData.localLoadSpeed;
                simABManager.remoteLoadSpeed = virtualBundleData.remoteLoadSpeed;
                simABManager.SetBundles(virtualBundleData.simulatedAssetBundles);
                ResourceManager.resourceProviders.Insert(0, new CachedProvider(new VirtualAssetBundleProvider(simABManager, false)));
                ResourceManager.resourceProviders.Insert(0, new CachedProvider(new VirtualAssetBundleProvider(simABManager, true)));
                ResourceManager.resourceProviders.Insert(0, new CachedProvider(new VirtualBundledAssetProvider(simABManager.localLoadSpeed)));
            }
        }

        float GetBundleLoadTime(string id)
        {
            foreach (var b in bundles)
                if (b.name == id)
                    return b.GetLoadTime(localLoadSpeed, remoteLoadSpeed);
            return 0;
        }

        public IAsyncOperation<VirtualAssetBundle> LoadAsync(string id)
        {
            var op = new LoadAssetBundleOp(this, id, GetBundleLoadTime(id));
            operations.Add(op);
            return op;
        }

        public void Update()
        {
            foreach (LoadAssetBundleOp o in operations)
            {
                if (!o.Update())
                {
                    operations.Remove(o);
                    break;
                }
            }
            foreach (var b in bundles)
                b.UpdateAsyncOperations();
        }
    }
}
