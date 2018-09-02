using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using ResourceManagement.AsyncOperations;
using ResourceManagement.Util;

namespace ResourceManagement.ResourceProviders.Experimental
{
    public class PooledInstanceProvider : IInstanceProvider
    {
        class PooledProviderUpdater : MonoBehaviour
        {
            PooledInstanceProvider m_Provider;
            public void Init(PooledInstanceProvider p)
            {
                m_Provider = p;
                DontDestroyOnLoad(gameObject);
            }

            private void Update()
            {
                m_Provider.Update();
            }
        }
        internal Dictionary<IResourceLocation, InstancePool> m_pools = new Dictionary<IResourceLocation, InstancePool>();

        float m_releaseTime;
        public PooledInstanceProvider(string name, float releaseTime)
        {
            m_releaseTime = releaseTime;
            var go = new GameObject(name, typeof(PooledProviderUpdater));
            go.GetComponent<PooledProviderUpdater>().Init(this);
            go.hideFlags = HideFlags.HideAndDontSave;
        }

        public bool CanProvideInstance<TObject>(IResourceProvider loadProvider, IResourceLocation loc) where TObject : Object
        {
            return loadProvider.CanProvide<TObject>(loc) && typeof(TObject).IsAssignableFrom(typeof(GameObject));
        }

        public IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation) where TObject : Object
        {
            InstancePool pool;
            if (!m_pools.TryGetValue(location, out pool))
                m_pools.Add(location, pool = new InstancePool(loadProvider, location));

            pool.m_holdCount++;
            return pool.ProvideInstanceAsync<TObject>(loadProvider, loadDependencyOperation);
        }

        public bool ReleaseInstance(IResourceProvider loadProvider, IResourceLocation location, Object instance)
        {
            InstancePool pool;
            if (!m_pools.TryGetValue(location, out pool))
                m_pools.Add(location, pool = new InstancePool(loadProvider, location));
            pool.m_holdCount--;
            pool.Put(instance);
            return false;
        }

        public void Update()
        {
            foreach (var p in m_pools)
            {
                if (!p.Value.Update(m_releaseTime))
                {
                    m_pools.Remove(p.Key);
                    break;
                }
            }
        }

        void HoldPool(IResourceProvider prov, IResourceLocation loc)
        {
            InstancePool pool;
            if (!m_pools.TryGetValue(loc, out pool))
                m_pools.Add(loc, pool = new InstancePool(prov, loc));
            pool.m_holdCount++;
        }

        void ReleasePool(IResourceProvider prov, IResourceLocation loc)
        {
            InstancePool pool;
            if (!m_pools.TryGetValue(loc, out pool))
                m_pools.Add(loc, pool = new InstancePool(prov, loc));
            pool.m_holdCount--;
        }

        internal class InternalOp<TObject> : AsyncOperationBase<TObject> where TObject : class
        {
            TObject prefabResult;
            int m_startFrame;
            Action<IAsyncOperation<TObject>> m_onCompleteAction;

            public InternalOp() 
            {
                m_onCompleteAction = OnComplete;
            }

            public InternalOp<TObject> Start(IAsyncOperation<TObject> loadOp, IResourceLocation loc, TObject val)
            {
                prefabResult = null;
                m_result = val;
                m_context = loc;
                m_startFrame = Time.frameCount;
                if (loadOp != null)
                    loadOp.completed += m_onCompleteAction;
                else
                    OnComplete(m_result);
                return this;
            }

            void OnComplete(TObject res)
            {
                m_result = res;
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncCompletion, m_context as IResourceLocation, Time.frameCount - m_startFrame);
                InvokeCompletionEvent();
                AsyncOperationCache.Instance.Release<TObject>(this);
            }

            void OnComplete(IAsyncOperation<TObject> op)
            {
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncCompletion, m_context as IResourceLocation, Time.frameCount - m_startFrame);
                prefabResult = op.result;

                if (prefabResult == null)
                    Debug.LogWarning("NULL prefab on instantiate: " + m_context);
                else if (m_result == null)
                    m_result = Object.Instantiate(prefabResult as GameObject) as TObject;

                InvokeCompletionEvent();
                AsyncOperationCache.Instance.Release<TObject>(this);
            }
        }

        internal class InstancePool
        {
            public IResourceLocation m_location;
            public float m_lastRefTime = 0;
            float m_lastReleaseTime;
            public int m_holdCount = 0;
            public Stack<Object> m_instances = new Stack<Object>();
            public bool Empty { get { return m_instances.Count == 0; } }
            IResourceProvider m_loadProvider;
            public InstancePool(IResourceProvider prov, IResourceLocation loc)
            {
                m_location = loc;
                m_loadProvider = prov;
                m_lastRefTime = Time.unscaledTime;
            }

            public T Get<T>() where T : class
            {
                m_lastRefTime = Time.unscaledTime;
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.PoolCount, m_location, m_instances.Count - 1);
                var o = m_instances.Pop() as T;
                (o as GameObject).SetActive(true);
                return o;
            }

            public void Put(Object o)
            {
                (o as GameObject).SetActive(false);
                m_instances.Push(o);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.PoolCount, m_location, m_instances.Count);
            }

            internal bool Update(float releaseTime)
            {
                if (m_instances.Count > 0)
                {
                    if ((m_instances.Count > 1 && Time.unscaledTime - m_lastReleaseTime > releaseTime) || Time.unscaledTime - m_lastRefTime > (1f / m_instances.Count) * releaseTime)  //the last item will take releaseTime seconds to drop...
                    {
                        m_lastReleaseTime = m_lastRefTime = Time.unscaledTime;
                        var inst = m_instances.Pop();
                        m_loadProvider.Release(m_location, null);
                        GameObject.Destroy(inst);
                        ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.PoolCount, m_location, m_instances.Count);
                        if (m_instances.Count == 0 && m_holdCount == 0)
                            return false;
                    }
                }
                return true;
            }

            internal IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IAsyncOperation<IList<object>> loadDependencyOperation) where TObject : Object
            {
                if (m_instances.Count > 0)
                    return AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>().Start(null, m_location, Get<TObject>());

                var depOp = loadProvider.ProvideAsync<TObject>(m_location, loadDependencyOperation);
                return AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>().Start(depOp, m_location, null);
            }
        }
    }
}
