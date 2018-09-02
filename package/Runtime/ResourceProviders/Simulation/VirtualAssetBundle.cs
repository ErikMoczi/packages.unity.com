#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    [Serializable]
    public class VirtualAssetBundle
    {
        [SerializeField]
        string m_name;
        [SerializeField]
        bool m_isLocal;
        [SerializeField]
        int m_size;
        [SerializeField]
        List<string> m_assets = new List<string>();
        [SerializeField]
        List<int> m_sizes = new List<int>();

        bool m_loaded;
        List<IAsyncOperation> m_operations = new List<IAsyncOperation>();
        VirtualAssetBundleManager m_manager;

        public string Name { get { return m_name; } }

        public VirtualAssetBundle() {}
        public VirtualAssetBundle(string name, bool local, int size, IEnumerable<string> assets)
        {
            m_name = name;
            m_size = size;
            m_isLocal = local;
            m_assets.AddRange(assets);
            //TODO: pass in real size from VirtualAssetBundleRuntimeData
            foreach (var aa in m_assets)
                m_sizes.Add(1024 * 1024); //each asset is 1MB for now...
        }

        internal bool Unload()
        {
            if (!m_loaded)
                Debug.LogWarning("Simulated assetbundle " + Name + " is already unloaded.");
            m_loaded = false;
            return true;
        }

        internal VirtualAssetBundle Load(VirtualAssetBundleManager manager)
        {
            if (m_loaded)
                Debug.LogWarning("Simulated assetbundle " + Name + " is already loaded.");
            m_manager = manager;
            m_loaded = true;
            return this;

        }

        public IAsyncOperation<TObject> LoadAssetAsync<TObject>(IResourceLocation location, int speed) where TObject : class
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");

            if (m_loaded && m_assets.Contains(location.InternalId))
            {
                var op = new LoadAssetOp<TObject>(location, m_sizes[m_assets.IndexOf(location.InternalId)] / (float)speed);
                m_operations.Add(op);
                m_manager.AddToUpdateList(this);
                return op;
            }
            Debug.Log("Unable to load asset " + location.InternalId + " from simulated bundle " + Name);
            return null;
        }

        class LoadAssetOp<TObject> : AsyncOperationBase<TObject>  where TObject : class
        {
            float loadTime;
            float startTime;
            public LoadAssetOp(IResourceLocation location, float delay)
            {
                Context = location;
                loadTime = (startTime = Time.unscaledTime) + delay;
            }

            public override float PercentComplete { get { return Mathf.Clamp01((Time.unscaledTime - startTime) / (loadTime - startTime)); } }

            public override bool IsDone
            {
                get
                {
                    Validate();
                    if (base.IsDone)
                        return true;
                    if (Time.unscaledTime > loadTime)
                    {
                        var assetPath = (Context as IResourceLocation).InternalId;
                        Result = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(assetPath) as TObject;
                        InvokeCompletionEvent();
                        return true;
                    }
                    return false;
                }
            }
        }

        public bool UpdateAsyncOperations()
        {
            foreach (var o in m_operations)
            {
                if (o.IsDone)
                {
                    m_operations.Remove(o);
                    break;
                }
            }
            return m_operations.Count > 0;
        }

        //TODO: this needs to take into account the load of the entire system, not just a single asset load
        internal float GetLoadTime(int localLoadSpeed, int remoteLoadSpeed)
        {
            if (m_isLocal)
                return m_size / (float)localLoadSpeed;
            return m_size / (float)remoteLoadSpeed;
        }

    }
}
#endif
