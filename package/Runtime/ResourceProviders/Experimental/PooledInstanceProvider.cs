using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.ResourceManagement
{
    public class PooledInstanceProvider : IInstanceProvider
    {
        internal Dictionary<IResourceLocation, InstancePool> m_pools = new Dictionary<IResourceLocation, InstancePool>();

        float m_releaseTime;
        public PooledInstanceProvider(string name, float releaseTime)
        {
            m_releaseTime = releaseTime;
            var go = new GameObject(name, typeof(PooledInstanceProviderBehavior));
            go.GetComponent<PooledInstanceProviderBehavior>().Init(this);
            go.hideFlags = HideFlags.HideAndDontSave;
        }

        public bool CanProvideInstance<TObject>(IResourceProvider loadProvider, IResourceLocation location) where TObject : Object
        {
            return loadProvider!= null && loadProvider.CanProvide<TObject>(location) && ResourceManagerConfig.IsInstance<TObject, GameObject>();
        }

        public IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, InstantiationParameters instantiateParameters) where TObject : Object
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");
            if (loadProvider == null)
                throw new ArgumentNullException("loadProvider");
            InstancePool pool;
            if (!m_pools.TryGetValue(location, out pool))
                m_pools.Add(location, pool = new InstancePool(loadProvider, location));

            pool.m_holdCount++;
            return pool.ProvideInstanceAsync<TObject>(loadProvider, loadDependencyOperation, instantiateParameters);
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

        void HoldPool(IResourceProvider provider, IResourceLocation location)
        {
            InstancePool pool;
            if (!m_pools.TryGetValue(location, out pool))
                m_pools.Add(location, pool = new InstancePool(provider, location));
            pool.m_holdCount++;
        }

        void ReleasePool(IResourceProvider provider, IResourceLocation location)
        {
            InstancePool pool;
            if (!m_pools.TryGetValue(location, out pool))
                m_pools.Add(location, pool = new InstancePool(provider, location));
            pool.m_holdCount--;
        }

        internal class InternalOp<TObject> : AsyncOperationBase<TObject> where TObject : Object
        {
            TObject prefabResult;
            int m_startFrame;
            Action<IAsyncOperation<TObject>> m_onLoadOperationCompleteAction;
            Action<TObject> m_onValidResultCompleteAction;
            InstantiationParameters m_instParams;
            public InternalOp() 
            {
                m_onLoadOperationCompleteAction = OnLoadComplete;
                m_onValidResultCompleteAction = OnInstantComplete;
            }

            public InternalOp<TObject> Start(IAsyncOperation<TObject> loadOperation, IResourceLocation location, TObject value, InstantiationParameters instantiateParameters)
            {
                Validate();
                prefabResult = null;
                m_instParams = instantiateParameters;
                Result = value;
                Context = location;
                m_startFrame = Time.frameCount;
                if (loadOperation != null)
                    loadOperation.Completed += m_onLoadOperationCompleteAction;
                else
                    DelayedActionManager.AddAction(m_onValidResultCompleteAction, Result);

                return this;
            }

            void OnInstantComplete(TObject res)
            {
                Validate();
                Result = res;
                var go = Result as GameObject;
                if (go != null)
                {
                    if(m_instParams.Parent != null)
                        go.transform.SetParent(m_instParams.Parent);
                    if (m_instParams.SetPositionRotation)
                    {
                        if (m_instParams.InstantiateInWorldPosition)
                        {
                            go.transform.position = m_instParams.Position;
                            go.transform.rotation = m_instParams.Rotation;
                        }
                        else
                        {
                            go.transform.SetPositionAndRotation(m_instParams.Position, m_instParams.Rotation);
                        }
                    }
                }
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncCompletion, Context, Time.frameCount - m_startFrame);
                InvokeCompletionEvent();
            }

            void OnLoadComplete(IAsyncOperation<TObject> operation)
            {
                Validate();
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.InstantiateAsyncCompletion, Context, Time.frameCount - m_startFrame);
                prefabResult = operation.Result;

                if (prefabResult == null)
                {
                    Debug.LogWarning("NULL prefab on instantiate: " + Context);
                }
                else if (Result == null)
                {
                    Result = m_instParams.Instantiate(prefabResult);
                }

                InvokeCompletionEvent();
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
            public InstancePool(IResourceProvider provider, IResourceLocation location)
            {
                m_location = location;
                m_loadProvider = provider;
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

            public void Put(Object gameObject)
            {
                (gameObject as GameObject).SetActive(false);
                m_instances.Push(gameObject);
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

            internal IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IAsyncOperation<IList<object>> loadDependencyOperation, InstantiationParameters instantiateParameters) where TObject : Object
            {
                if (m_instances.Count > 0)
                    return AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>().Start(null, m_location, Get<TObject>(), instantiateParameters);

                var depOp = loadProvider.ProvideAsync<TObject>(m_location, loadDependencyOperation);
                return AsyncOperationCache.Instance.Acquire<InternalOp<TObject>, TObject>().Start(depOp, m_location, null, instantiateParameters);
            }
        }
    }
}
