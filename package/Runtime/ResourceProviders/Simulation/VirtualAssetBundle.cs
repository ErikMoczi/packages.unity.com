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
        [NonSerialized]
        public VirtualAssetBundleManager m_manager;
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

        public IAsyncOperation<TObject> LoadAssetAsync<TObject>(IResourceLocation loc, int speed) where TObject : class
        {
#if UNITY_EDITOR
            if (loaded && assets.Contains(loc.id))
            {
                var op = new LoadAssetOp<TObject>(loc, sizes[assets.IndexOf(loc.id)] / (float)speed);
                operations.Add(op);
                m_manager.AddToUpdateList(this);
                return op;
            }
#endif
            Debug.Log("Unable to load asset " + loc.id + " from simulated bundle " + name);
            return null;
        }

        class LoadAssetOp<TObject> : AsyncOperationBase<TObject>  where TObject : class
        {
            float loadTime;
            float startTime;
            public LoadAssetOp(IResourceLocation loc, float delay)
            {
                m_context = loc;
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
#if UNITY_EDITOR        //this only works in the editor
                        m_result = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>((m_context as IResourceLocation).id) as TObject;
#endif
                        InvokeCompletionEvent();
                        return true;
                    }
                    return false;
                }
            }
        }

        public bool UpdateAsyncOperations()
        {
            foreach (var o in operations)
            {
                if (o.isDone)
                {
                    operations.Remove(o);
                    break;
                }
            }
            return operations.Count > 0;
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
