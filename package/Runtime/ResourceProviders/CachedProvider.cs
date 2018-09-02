using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using ResourceManagement.Util;
using UnityEngine;

namespace ResourceManagement.ResourceProviders
{
    public class CachedProvider : IResourceProvider
    {
        internal abstract class CacheEntry
        {
            protected object m_result;
            protected CacheList m_cacheList;
            abstract internal bool CanProvide<TObject>(IResourceLocation loc) where TObject : class;

            public abstract bool isDone { get; }
            public abstract float percentComplete { get; }
            public object result { get { return m_result; } }
        }

        internal class CacheEntry<TObject> : CacheEntry, IAsyncOperation<TObject>
            where TObject : class
        {
            IAsyncOperation m_operation;
            AsyncOperationStatus m_status;
            Exception m_error;
            public AsyncOperationStatus status { get { return m_status > AsyncOperationStatus.None ? m_status : m_operation.status; } }
            public Exception error { get { return m_error != null ? m_error : m_operation.error; } }
            new public TObject result { get { return m_result as TObject; } }
            public override bool isDone { get { return !(EqualityComparer<TObject>.Default.Equals(result, default(TObject))); } }
            public object Current { get { return m_result; } }
            public bool MoveNext() { return m_result == null; }
            public object context { get { return m_operation.context; } }
            public void Reset() {}
            public virtual void ResetStatus()
            {
                m_operation.ResetStatus();
                m_status = AsyncOperationStatus.None;
                m_error = null;
            }

            public override float percentComplete { get { return isDone ? 1f : m_operation.percentComplete; } }
            protected event Action<IAsyncOperation<TObject>> m_completedActionT;
            protected event Action<IAsyncOperation> m_completedAction;
            public event Action<IAsyncOperation<TObject>> completed
            {
                add
                {
                    if (isDone)
                    {
                        try
                        {
                            value(this);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            m_error = e;
                            m_status = AsyncOperationStatus.Failed;
                        }
                    }
                    else
                        m_completedActionT += value;
                }

                remove
                {
                    m_completedActionT -= value;
                }
            }

            event Action<IAsyncOperation> IAsyncOperation.completed
            {
                add
                {
                    if (isDone)
                    {
                        try
                        {
                            value(this);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            m_error = e;
                            m_status = AsyncOperationStatus.Failed;
                        }
                    }
                    else
                        m_completedAction += value;
                }

                remove
                {
                    m_completedAction -= value;
                }
            }
            public CacheEntry(CacheList cl, IAsyncOperation<TObject> op)
            {
                m_cacheList = cl;
                m_operation = op;
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, context, 0);
                op.completed += OnComplete;
            }

            void OnComplete(IAsyncOperation<TObject> op)
            {
                m_result = op.result;
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
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, context, 100);
            }

            internal override bool CanProvide<T1>(IResourceLocation loc)
            {
                return typeof(TObject) == typeof(T1);
            }
        }

        internal class CacheList
        {
            public int m_refCount;
            public float m_lastAccessTime;
            public IResourceLocation m_location;
            public List<CacheEntry> entries = new List<CacheEntry>();
            public CacheList(IResourceLocation loc) { m_location = loc; }
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
                        rc += ee.percentComplete;
                    return rc / entries.Count;
                }
            }

            public CacheEntry<TObject> FindEntry<TObject>(IResourceLocation loc)
                 where TObject : class
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (e.CanProvide<TObject>(loc))
                        return e as CacheEntry<TObject>;
                }
                return null;
            }

            public CacheEntry<TObject> CreateEntry<TObject>(IAsyncOperation<TObject> op) 
                where TObject : class
            {
                var entry = new CacheEntry<TObject>(this, op);
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
                    if (!provider.Release(m_location, e.result))
                        Debug.LogWarning("Failed to release location " + m_location);
                }
            }
        }

        class CachedProviderUpdater : MonoBehaviour
        {
            CachedProvider m_Provider;
            public void Init(CachedProvider p)
            {
                m_Provider = p;
                DontDestroyOnLoad(gameObject);
            }

            private void Update()
            {
                m_Provider.UpdateLRU();
            }
        }

        internal Dictionary<int, CacheList> m_cache = new Dictionary<int, CacheList>();
        protected IResourceProvider m_internalProvider;
        LinkedList<CacheList> m_lru;
        int m_maxLRUCount;
        float m_maxLRUAge;

        public CachedProvider(IResourceProvider prov, int lruCount = 0, float lruMaxAge = 0)
        {
            m_internalProvider = prov;
            m_maxLRUCount = lruCount;
            if (m_maxLRUCount > 0)
            {
                m_lru = new LinkedList<CacheList>();
                m_maxLRUAge = lruMaxAge;
                if (lruMaxAge > 0)
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
        public string providerId { get { return m_internalProvider.providerId; } }

        public bool CanProvide<TObject>(IResourceLocation loc)
            where TObject : class
        {
            return m_internalProvider.CanProvide<TObject>(loc);
        }

        public bool Release(IResourceLocation loc, object asset)
        {
            CacheList entryList = null;
            if (!m_cache.TryGetValue(loc.GetHashCode(), out entryList))
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

        public IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            where TObject : class
        {
            CacheList entryList = null;
            if (!m_cache.TryGetValue(loc.GetHashCode(), out entryList))
            {
                if (m_lru != null && m_lru.Count > 0)
                {
                    var node = m_lru.First;
                    while (node != null)
                    {
                        if (node.Value.m_location.GetHashCode() == loc.GetHashCode())
                        {
                            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, loc, 1);
                            entryList = node.Value;
                            m_lru.Remove(node);
                            break;
                        }
                        node = node.Next;
                    }
                }
                if (entryList == null)
                    entryList = new CacheList(loc);

                m_cache.Add(loc.GetHashCode(), entryList);
            }

            entryList.Retain();
            var entry = entryList.FindEntry<TObject>(loc);
            if (entry != null)
                return entry;
            return entryList.CreateEntry(m_internalProvider.ProvideAsync<TObject>(loc, loadDependencyOperation));
        }

    }
}
