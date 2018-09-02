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
            abstract internal bool CanProvide<TObject>(IResourceLocation location) where TObject : class;

            public abstract bool IsDone { get; }
            public abstract float PercentComplete { get; }
            public object Result
            {
                get
                {
                    return m_result;
                }
            }
            public abstract void ReleaseToCacheInternal();
        }

        internal class CacheEntry<TObject> : CacheEntry, IAsyncOperation<TObject>
            where TObject : class
        {
            IAsyncOperation m_operation;
            AsyncOperationStatus m_status;
            Exception m_error;
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
            public void Reset()
            {
            }
            public bool IsValid { get { return m_operation.IsValid; } set { } }

            public void ResetStatus()
            {
                //should never be called as this operation doe not end up in cache
            }

            public bool BlockReleaseToCache { get; set; }
            public void ReleaseToCache(bool force = false)
            {
                //This object will never be released to cache
            }

            public override void ReleaseToCacheInternal()
            {
                m_operation.ReleaseToCache(true);
            }

            public override float PercentComplete
            {
                get
                {
                    Validate();
                    return IsDone ? 1f : m_operation.PercentComplete;
                }
            }
            protected event Action<IAsyncOperation<TObject>> m_completedActionT;
            protected event Action<IAsyncOperation> m_completedAction;
            public event Action<IAsyncOperation<TObject>> Completed
            {
                add
                {
                    Validate();
                    if (IsDone)
                        DelayedActionManager.AddAction(value, this);
                    else
                        m_completedActionT += value;
                }

                remove
                {
                    m_completedActionT -= value;
                }
            }

            event Action<IAsyncOperation> IAsyncOperation.Completed
            {
                add
                {
                    Validate();
                    if (IsDone)
                        DelayedActionManager.AddAction(value, this);
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
                IsValid = true;
                m_cacheList = cacheList;
                m_operation = operation;
                m_operation.BlockReleaseToCache = true;
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 0);
                operation.Completed += OnComplete;
            }

            void OnComplete(IAsyncOperation<TObject> operation)
            {
                Validate();
                m_result = operation.Result;
                if (m_completedActionT != null)
                {
                    var tmpEvent = m_completedActionT;
                    m_completedActionT = null;
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
                    if (!provider.Release(m_location, e.Result))
                        Debug.LogWarning("Failed to release location " + m_location);
                    e.ReleaseToCacheInternal();
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
            }
        }

        private void UpdateLRU()
        {          
            if (m_lru != null)
            {
                float time = Time.unscaledTime;
                while (m_lru.Last != null && m_lru.Last.Value.m_lastAccessTime > m_maxLRUAge)
                {
                    m_lru.Last.Value.ReleaseAssets(m_internalProvider);
                    m_lru.RemoveLast();
                }
            }
        }

 
        public override string ToString() { return "CachedProvider[" + m_internalProvider + "]"; }
        public string ProviderId { get { return m_internalProvider.ProviderId; } }

        public bool CanProvide<TObject>(IResourceLocation location)
            where TObject : class
        {
            return m_internalProvider.CanProvide<TObject>(location);
        }

        public bool Release(IResourceLocation location, object asset)
        {
            CacheList entryList = null;
            if (location == null || !m_cache.TryGetValue(location.GetHashCode(), out entryList))
                return false;

            if (entryList.Release())
            {
                if (m_lru != null)
                {
                    m_lru.AddFirst(entryList);
                    while (m_lru.Count > m_maxLRUCount)
                    {
                        m_lru.Last.Value.ReleaseAssets(m_internalProvider);
                        m_lru.RemoveLast();
                    }
                }
                else
                {
                    entryList.ReleaseAssets(m_internalProvider);
                }

                m_cache.Remove(entryList.GetHashCode());
                return true;
            }
            return false;
        }

        public IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            where TObject : class
        {
            if (location == null)
                throw new System.ArgumentNullException("location");
            if (loadDependencyOperation == null)
                throw new System.ArgumentNullException("loadDependencyOperation");

            CacheList entryList = null;
            if (location == null)
                return null;

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
