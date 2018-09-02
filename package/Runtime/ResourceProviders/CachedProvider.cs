using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.ResourceManagement
{
    public class CachedProvider : IResourceProvider
    {
        internal abstract class CacheEntry
        {
            protected object m_result;
            protected CacheList m_cacheList;
            protected AsyncOperationStatus m_status;
            protected Exception m_error;
            abstract internal bool CanProvide<TObject>(IResourceLocation location) where TObject : class;

            public abstract bool IsDone { get; }
            public abstract float PercentComplete { get; }
            public object Result { get { return m_result; } }
            public abstract void ReleaseInternalOperation();

            public void Reset() { }

            public void ResetStatus()
            {
                //should never be called as this operation doe not end up in cache
            }
        }

        internal class CacheEntry<TObject> : CacheEntry, IAsyncOperation<TObject>
            where TObject : class
        {
            IAsyncOperation<TObject> m_operation;

            public AsyncOperationStatus Status
            {
                get
                {
                    Validate();
                    return m_status > AsyncOperationStatus.None ? m_status : m_operation.Status;
                }
            }

            public Exception OperationException
            {
                get
                {
                    Validate();
                    return m_error != null ? m_error : m_operation.OperationException;
                }
            }

            new public TObject Result
            {
                get
                {
                    Validate();
                    return m_result as TObject;
                }
            }

            public override bool IsDone
            {
                get
                {
                    Validate();
                    return !(EqualityComparer<TObject>.Default.Equals(Result, default(TObject)));
                }
            }

            public object Current
            {
                get
                {
                    Validate();
                    return m_result;
                }
            }

            public bool MoveNext()
            {
                Validate();
                return m_result == null;
            }

            public object Context
            {
                get
                {
                    Validate();
                    return m_operation.Context;
                }
            }


            public bool IsValid { get { return m_operation != null && m_operation.IsValid; } set { } }


            public override void ReleaseInternalOperation()
            {
                m_operation.Release();
                m_operation = null;
            }

            public override float PercentComplete
            {
                get
                {
                    Validate();
                    return IsDone ? 1f : m_operation.PercentComplete;
                }
            }
            List<Action<IAsyncOperation<TObject>>> m_completedActionT;
            protected event Action<IAsyncOperation> m_completedAction;
            public event Action<IAsyncOperation<TObject>> Completed
            {
                add
                {
                    Validate();
                    if (IsDone)
                    {
                        DelayedActionManager.AddAction(value, 0, this);
                    }
                    else
                    {
                        if (m_completedActionT == null)
                            m_completedActionT = new List<Action<IAsyncOperation<TObject>>>(2);
                        m_completedActionT.Add(value);
                    }
                }

                remove
                {
                    m_completedActionT.Remove(value);
                }
            }

            event Action<IAsyncOperation> IAsyncOperation.Completed
            {
                add
                {
                    Validate();
                    if (IsDone)
                        DelayedActionManager.AddAction(value, 0, this);
                    else
                        m_completedAction += value;
                }

                remove
                {
                    m_completedAction -= value;
                }
            }

            public CacheEntry(CacheList cacheList, IAsyncOperation<TObject> operation)
            {
                m_cacheList = cacheList;
                m_operation = operation.Acquire();
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 0);
                operation.Completed += OnComplete;
            }

            void OnComplete(IAsyncOperation<TObject> operation)
            {
                Validate();
                m_result = operation.Result;
                if (m_completedActionT != null)
                {
                    for (int i = 0; i < m_completedActionT.Count; i++)
                    {
                        try
                        {
                            m_completedActionT[i](this);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            m_error = e;
                            m_status = AsyncOperationStatus.Failed;
                        }
                    }
                    m_completedActionT.Clear();
                }
                if (m_completedAction != null)
                {
                    var tmpEvent = m_completedAction;
                    m_completedAction = null;
                    try
                    {
                        tmpEvent(this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        m_error = e;
                        m_status = AsyncOperationStatus.Failed;
                    }
                }
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 100);
            }

            internal override bool CanProvide<T1>(IResourceLocation location)
            {
                Validate();
                return typeof(TObject) == typeof(T1);
            }

            public bool Validate()
            {
                if (!IsValid)
                {
                    Debug.LogError("IAsyncOperation Validation Failed!");
                    return false;
                }
                return true;
            }

            public IAsyncOperation<TObject> Acquire()
            {
                return this;
            }

            public void Release()
            {
                //do nothing
            }
        }

        internal class CacheList
        {
            public int m_refCount;
            public float m_lastAccessTime;
            public IResourceLocation m_location;
            public List<CacheEntry> entries = new List<CacheEntry>();
            public CacheList(IResourceLocation location) { m_location = location; }
            public int RefCount
            {
                get
                {
                    return m_refCount;
                }
            }

            public override int GetHashCode()
            {
                return m_location.GetHashCode();
            }

            public bool IsDone
            {
                get
                {
                    foreach (var ee in entries)
                        if (!ee.IsDone)
                            return false;
                    return true;
                }
            }

            public float CompletePercent
            {
                get
                {
                    if (entries.Count == 0)
                        return 0;
                    float rc = 0;
                    foreach (var ee in entries)
                        rc += ee.PercentComplete;
                    return rc / entries.Count;
                }
            }

            public CacheEntry<TObject> FindEntry<TObject>(IResourceLocation location)
                 where TObject : class
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (e.CanProvide<TObject>(location))
                        return e as CacheEntry<TObject>;
                }
                return null;
            }

            public CacheEntry<TObject> CreateEntry<TObject>(IAsyncOperation<TObject> operation) 
                where TObject : class
            {
                var entry = new CacheEntry<TObject>(this, operation);
                entries.Add(entry);
                return entry;
            }


            internal void Retain()
            {
                m_lastAccessTime = Time.unscaledTime;
                m_refCount++;
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryRefCount, m_location, m_refCount);
            }

            internal bool Release()
            {
                m_refCount--;
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryRefCount, m_location, m_refCount);
                return m_refCount == 0;
            }

            internal void ReleaseAssets(IResourceProvider provider)
            {
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, m_location, 0);
                foreach (var e in entries)
                {
                    Debug.Assert(e.IsDone);
                    provider.Release(m_location, e.Result);
                    e.ReleaseInternalOperation();
                }
            }
        }

        class CachedProviderUpdater : MonoBehaviour
        {
            CachedProvider m_Provider;
            public void Init(CachedProvider provider)
            {
                m_Provider = provider;
                DontDestroyOnLoad(gameObject);
            }

            private void Update()
            {
                m_Provider.UpdateLRU();
            }
        }

        Dictionary<int, CacheList> m_cache = new Dictionary<int, CacheList>();
        IResourceProvider m_internalProvider;
        LinkedList<CacheList> m_lru;
        int m_maxLRUCount;
        float m_maxLRUAge;

        public CachedProvider(IResourceProvider provider, int maxCacheItemCount = 0, float maxCacheItemAge = 0)
        {
            m_internalProvider = provider;
            m_maxLRUCount = maxCacheItemCount;
            if (m_maxLRUCount > 0)
            {
                m_lru = new LinkedList<CacheList>();
                m_maxLRUAge = maxCacheItemAge;
                if (maxCacheItemAge > 0)
                {
                    var go = new GameObject("CachedProviderUpdater", typeof(CachedProviderUpdater));
                    go.GetComponent<CachedProviderUpdater>().Init(this);
                    go.hideFlags = HideFlags.HideAndDontSave;
                }
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheLRUCount, m_internalProvider.ProviderId, m_lru.Count);
            }
        }

        private void UpdateLRU()
        {          
            if (m_lru != null)
            {
                float time = Time.unscaledTime;
                while (m_lru.Last != null && (m_lru.Last.Value.m_lastAccessTime - time) > m_maxLRUAge && m_lru.Last.Value.IsDone)
                {
                    m_lru.Last.Value.ReleaseAssets(m_internalProvider);
                    m_lru.RemoveLast();
                }
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheLRUCount, m_internalProvider.ProviderId, m_lru.Count);
            }
        }

 
        public override string ToString() { return "CachedProvider[" + m_internalProvider + "]"; }
        public string ProviderId { get { return m_internalProvider.ProviderId; } }

        public bool CanProvide<TObject>(IResourceLocation location)
            where TObject : class
        {
            return m_internalProvider.CanProvide<TObject>(location);
        }

        Action<CacheList> m_retryReleaseEntryAction;
        public bool Release(IResourceLocation location, object asset)
        {
            CacheList entryList = null;
            if (location == null || !m_cache.TryGetValue(location.GetHashCode(), out entryList))
                return false;

            return ReleaseCache(entryList);
        }

        bool ReleaseCache(CacheList entryList)
        {
            if (entryList.Release())
            {
                if (!entryList.IsDone)
                {
                    if (m_retryReleaseEntryAction == null)
                        m_retryReleaseEntryAction = RetryEntryRelease;
                    entryList.Retain(); //hold on since this will be retried...
                    DelayedActionManager.AddAction(m_retryReleaseEntryAction, .2f, entryList);
                    return false;
                }
                else
                {
                    if (m_lru != null)
                    {
                        m_lru.AddFirst(entryList);
                        while (m_lru.Count > m_maxLRUCount && m_lru.Last.Value.IsDone)
                        {
                            m_lru.Last.Value.ReleaseAssets(m_internalProvider);
                            m_lru.RemoveLast();
                        }
                        ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheLRUCount, m_internalProvider.ProviderId + " LRU", m_lru.Count);
                    }
                    else
                    {
                        entryList.ReleaseAssets(m_internalProvider);
                    }

                    if (!m_cache.Remove(entryList.GetHashCode()))
                        Debug.LogWarningFormat("Unable to find entryList {0} in cache.", entryList.m_location);
                }
                return true;
            }
            return false;
        }


        internal void RetryEntryRelease(CacheList e)
        {
            ReleaseCache(e);
        }

        public IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            where TObject : class
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");

            CacheList entryList = null;
            if (!m_cache.TryGetValue(location.GetHashCode(), out entryList))
            {
                if (m_lru != null && m_lru.Count > 0)
                {
                    var node = m_lru.First;
                    while (node != null)
                    {
                        if (node.Value.m_location.GetHashCode() == location.GetHashCode())
                        {
                            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, location, 1);
                            entryList = node.Value;
                            m_lru.Remove(node);
                            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheLRUCount, m_internalProvider.ProviderId, m_lru.Count);
                            break;
                        }
                        node = node.Next;
                    }
                }
                if (entryList == null)
                    entryList = new CacheList(location);

                m_cache.Add(location.GetHashCode(), entryList);
            }

            entryList.Retain();
            var entry = entryList.FindEntry<TObject>(location);
            if (entry != null)
                return entry;
            return entryList.CreateEntry(m_internalProvider.ProvideAsync<TObject>(location, loadDependencyOperation));
        }

    }
}
