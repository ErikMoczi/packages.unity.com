using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using ResourceManagement.Diagnostics;

namespace ResourceManagement.ResourceProviders
{
    public class CachedProvider : IResourceProvider
    {
        public abstract class CacheEntry
        {
            protected object m_result;
            protected IResourceLocation m_location;
            protected CacheList m_cacheList;
            public abstract Type GetEntryType();
            abstract internal bool CanProvide<TObject>(IResourceLocation loc) where TObject : class;

            public abstract bool isDone { get; }
            public abstract float percentComplete { get; }
            public object result { get { return m_result; } }
        }

        public class CacheEntry<TObject> : CacheEntry, IAsyncOperation<TObject>
            where TObject : class
        {
            IAsyncOperation operation;
            public override Type GetEntryType() { return typeof(TObject); }
            public void AddCompletionEventHandler(object d)
            {
                completed += (d as Action<IAsyncOperation<TObject>>);
            }

            new public TObject result { get { return m_result as TObject; } }
            public string id { get { return m_location.id; } }
            public override bool isDone { get { return !(EqualityComparer<TObject>.Default.Equals(result, default(TObject))); } }
            object IAsyncOperation.result { get { return m_result; } }
            public object Current { get { return m_result; } }
            public bool MoveNext() { return m_result == null; }
            public void Reset() {}
            public override float percentComplete { get { return isDone ? 1f : (operation == null ? 0 : operation.percentComplete); } }
            protected event Action<IAsyncOperation<TObject>> m_onComplete;
            public event Action<IAsyncOperation<TObject>> completed
            {
                add
                {
                    if (isDone)
                        value(this);
                    else
                        m_onComplete += value;
                }

                remove
                {
                    m_onComplete -= value;
                }
            }

            public CacheEntry(CacheList cl, IResourceLocation loc, TObject res, IAsyncOperation<TObject> op)
            {
                m_cacheList = cl;
                m_location = loc;
                operation = op;
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.CacheEntryLoadPercent, m_location, 0);
                m_result = res;
                if (m_result == null)
                    op.completed += OnComplete;
            }

            void OnComplete(IAsyncOperation<TObject> op)
            {
                m_result = op.result;
                if (m_onComplete != null)
                {
                    var tmpEvent = m_onComplete;
                    m_onComplete = null;
                    tmpEvent(this);
                }
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.CacheEntryLoadPercent, m_location, 100);
            }

            internal override bool CanProvide<T1>(IResourceLocation loc)
            {
                return typeof(TObject) == typeof(T1);
            }
        }

        public class CacheList
        {
            public int m_refCount;
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

            public CacheEntry<TObject> FindOrCreateEntry<TObject>(IResourceLocation loc, Func<TObject> syncOp)
                where TObject : class
            {
                return FindOrCreateEntry_Internal(loc, syncOp, null);
            }

            public CacheEntry<TObject> FindOrCreateEntry<TObject>(IResourceLocation loc, Func<IAsyncOperation<TObject>> asyncOp)
                where TObject : class
            {
                return FindOrCreateEntry_Internal(loc, null, asyncOp);
            }

            CacheEntry<TObject> FindOrCreateEntry_Internal<TObject>(IResourceLocation loc, Func<TObject> syncOp, Func<IAsyncOperation<TObject>> asyncOp)
                where TObject : class
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (e.CanProvide<TObject>(loc))
                        return e as CacheEntry<TObject>;
                }

                var res = syncOp != null ? syncOp() : null;
                var op = asyncOp != null ? asyncOp() : null;

                var entry = new CacheEntry<TObject>(this, loc, res, op);
                entries.Add(entry);
                return entry;
            }

            internal void Retain()
            {
                m_refCount++;
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.CacheEntryRefCount, m_location, m_refCount);
            }

            internal bool Release()
            {
                m_refCount--;
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.CacheEntryRefCount, m_location, m_refCount);
                return m_refCount == 0;
            }
        }

        public Dictionary<int, CacheList> m_cache = new Dictionary<int, CacheList>();
        public override string ToString() { return "CachedProvider[" + m_internalProvider + "]"; }
        protected IResourceProvider m_internalProvider;

        public string providerId
        {
            get
            {
                return m_internalProvider.providerId;
            }
        }

        public bool CanProvide<TObject>(IResourceLocation loc)
            where TObject : class
        {
            return m_internalProvider.CanProvide<TObject>(loc);
        }

        public CachedProvider(IResourceProvider prov)
        {
            m_internalProvider = prov;
        }

        CacheList GetEntries(IResourceLocation loc, bool create)
        {
            CacheList entries = null;
            if (!m_cache.TryGetValue(loc.GetHashCode(), out entries) && create)
                m_cache.Add(loc.GetHashCode(), entries = new CacheList(loc));
            return entries;
        }

        public bool Release(IResourceLocation loc, object asset)
        {
            var entryList = GetEntries(loc, false);
            if (entryList == null)
                return false;

            if (entryList.Release())
            {
                foreach (var e in entryList.entries)
                {
                    if (!m_internalProvider.Release(loc, e.result))
                        UnityEngine.Debug.LogWarning("Failed to release location " + loc);
                }

                m_cache.Remove(loc.GetHashCode());
                ResourceManagerProfiler.PostEvent(ResourceManagerEvent.Type.CacheEntryLoadPercent, loc, 0);

                return true;
            }

            return false;
        }

        public IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation)
            where TObject : class
        {
            var entryList = GetEntries(loc, true);
            entryList.Retain();

            return entryList.FindOrCreateEntry(loc, () => { return m_internalProvider.ProvideAsync<TObject>(loc, loadDependencyOperation); });
        }
    }
}
