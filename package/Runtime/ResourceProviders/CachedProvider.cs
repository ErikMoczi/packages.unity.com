using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provider that can wrap other IResourceProvders and add caching and reference counting of objects.
    /// </summary>
    public class CachedProvider : IResourceProvider, IInitializableObject
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

            public object Key
            {
                get
                {
                    Validate();
                    return m_operation.Key;
                }
                set
                {
                    m_operation.Key = value;
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
                m_operation = operation.Retain();
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 0);
                operation.Completed += OnComplete;
            }

            void OnComplete(IAsyncOperation<TObject> operation)
            {
                Validate();
                m_result = operation.Result;
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, Context, 100);
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

            public IAsyncOperation<TObject> Retain()
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
        string m_providerId;
        internal int m_maxLRUCount;
        internal float m_maxLRUAge;

        /// <summary>
        /// Settings object used to initialize the cache provider.
        /// </summary>
        [Serializable]
        public class Settings
        {
            [SerializeField]
            int m_maxLRUCount;
            /// <summary>
            /// The maximum number of items to hold in the LRU.  If set to 0, the LRU is not used and items will be released as soon as the reference count drops to 0.
            /// </summary>
            public int MaxLRUCount { get { return m_maxLRUCount; } set { m_maxLRUCount = value; } }
            [SerializeField]
            float m_maxLRUAge;
            /// <summary>
            /// The time, in seconds, to hold on to items in the cache.  This value scales inversely to how full the LRU is.  The last item will take this long to be removed.  If set to 0, items are not automatically removed.
            /// </summary>
            public float MaxLRUAge { get { return m_maxLRUAge; } set { m_maxLRUAge = value; } }
            [SerializeField]
            ObjectInitializationData m_internalProvider;
            /// <summary>
            /// The initialization data for the internal provider.
            /// </summary>
            public ObjectInitializationData InternalProviderData { get { return m_internalProvider; } set { m_internalProvider = value; } }
        }

        /// <summary>
        /// Construct a new CachedProvider object.  Initialize should be called after construction.
        /// </summary>
        public CachedProvider() { }

        /// <summary>
        /// Construct a new CachedProvider object.
        /// </summary>
        /// <param name="provider">The internal provider that will handle the loading and releasing of the objects.</param>
        /// <param name="maxCacheItemCount">How many items to keep in the cache.  If set to 0, items are not cached.</param>
        /// <param name="maxCacheItemAge">How long to keep items in the cache. If set to 0, cached items are kept indefinitely.</param>
        public CachedProvider(IResourceProvider provider, int maxCacheItemCount = 0, float maxCacheItemAge = 0)
        {
            InitInternal(provider, provider.GetType().FullName, maxCacheItemCount, maxCacheItemAge);
        }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <param name="id">The provider id.</param>
        /// <param name="data">Serialized data.  This can be generated by calling CreateInitializationData.</param>
        public bool Initialize(string id, string data)
        {
            var settings = JsonUtility.FromJson<Settings>(data);
            if (settings == null)
                return false;
            return InitInternal(settings.InternalProviderData.CreateInstance<IResourceProvider>(), id, settings.MaxLRUCount, settings.MaxLRUAge);
        }

        internal bool InitInternal(IResourceProvider provider, string providerId, int maxCacheItemCount, float maxCacheItemAge)
        {
            if (provider == null || string.IsNullOrEmpty(providerId))
                return false;

            m_internalProvider = provider;
            m_providerId = providerId;
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
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheLRUCount, ProviderId, m_lru.Count);
            }
            return true;
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
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheLRUCount, ProviderId, m_lru.Count);
            }
        }


        /// <inheritdoc/>
        public override string ToString() { return "CachedProvider[" + m_internalProvider + "]"; }
        /// <inheritdoc/>
        public string ProviderId { get { return m_providerId; } }

        /// <inheritdoc/>
        public bool CanProvide<TObject>(IResourceLocation location)
            where TObject : class
        {
            return m_internalProvider.CanProvide<TObject>(location);
        }

        Action<CacheList> m_retryReleaseEntryAction;
        /// <summary>
        /// Releasing an object to a CachedProvider will decrease it reference count, which may result in the object getting actually released.  Released objects are added to an in memory cache.
        /// </summary>
        /// <param name="location">The location of the object.</param>
        /// <param name="asset">The object to release.</param>
        /// <returns>True if the reference count reaches 0 and the asset is released.</returns>
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
                        ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheLRUCount, ProviderId + " LRU", m_lru.Count);
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

        /// <summary>
        /// Provide the requested object.  The cache will be checked first for existing objects.  If not found, the internal IResourceProvider will be used to provide the object.  The reference count for the asset will be incremented.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="location"></param>
        /// <param name="loadDependencyOperation"></param>
        /// <returns></returns>
        public IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation)
            where TObject : class
        {
            if (location == null)
                throw new System.ArgumentNullException("location");

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
                            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.CacheLRUCount, ProviderId, m_lru.Count);
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
            return entryList.CreateEntry(m_internalProvider.Provide<TObject>(location, loadDependencyOperation));
        }

    }
}
