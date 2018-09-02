using System;
using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.AsyncOperations;

namespace ResourceManagement.ResourceProviders.Simulation
{
    [Serializable]
    public class VirtualAssetBundle
    {
        public string name;
        public bool loaded;
        public bool isLocal;
        public int size;
        public List<string> assets = new List<string>();
        public List<int> sizes = new List<int>();
        List<IAsyncOperation> operations = new List<IAsyncOperation>();
        public VirtualAssetBundle() {}
        public VirtualAssetBundle(string n, bool local, int size, IEnumerable<string> a)
        {
            name = n;
            this.size = size;
            isLocal = local;
            assets.AddRange(a);
            //TODO: pass in real size from VirtualAssetBundleRuntimeData
            foreach (var aa in assets)
                sizes.Add(1024 * 1024); //each asset is 1MB for now...
        }

        public TObject LoadAsset<TObject>(string id, int speed) where TObject : class
        {
#if UNITY_EDITOR
            if (loaded && assets.Contains(id))
            {
                if (speed > 0)
                    System.Threading.Thread.Sleep((int)((sizes[assets.IndexOf(id)] / (float)speed) * 1000));
                return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(id) as TObject;
            }
#endif
            Debug.Log("Unable to load asset " + id + " from simulated bundle " + name);
            return default(TObject);
        }

        public IAsyncOperation<TObject> LoadAssetAsync<TObject>(string id, int speed) where TObject : class
        {
#if UNITY_EDITOR
            if (loaded && assets.Contains(id))
            {
                var op = new LoadAssetOp<TObject>(this, id, sizes[assets.IndexOf(id)] / (float)speed);
                operations.Add(op);
                return op;
            }
#endif
            Debug.Log("Unable to async load asset " + id + " from simulated bundle " + name);
            return null;
        }

        class LoadAssetOp<TObject> : AsyncOperationBase<TObject>  where TObject : class
        {
            VirtualAssetBundle bundle;
            float loadTime;
            float startTime;
            public LoadAssetOp(VirtualAssetBundle b, string id, float delay) : base(id)
            {
                bundle = b;
                loadTime = (startTime = Time.unscaledTime) + delay;
            }

            public override float percentComplete { get { return Mathf.Clamp01((Time.unscaledTime - startTime) / (loadTime - startTime)); } }

            public override bool isDone
            {
                get
                {
                    if (base.isDone)
                        return true;
                    if (Time.unscaledTime > loadTime)
                    {
                        m_result = bundle.LoadAsset<TObject>(id, 0);
                        InvokeCompletionEvent(this);
                        return true;
                    }
                    return false;
                }
            }
        }

        public void UpdateAsyncOperations()
        {
            foreach (var o in operations)
            {
                if (o.isDone)
                {
                    operations.Remove(o);
                    break;
                }
            }
        }

        //TODO: this needs to take into account the load of the entire system, not just a single asset load
        internal float GetLoadTime(int localLoadSpeed, int remoteLoadSpeed)
        {
            if (isLocal)
                return size / (float)localLoadSpeed;
            return size / (float)remoteLoadSpeed;
        }
    }
}
