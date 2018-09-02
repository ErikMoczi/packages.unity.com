//#define POST_ASYNCOPERATIONCACHE__EVENTS

using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.Diagnostics;


namespace UnityEngine.ResourceManagement
{
    public class AsyncOperationCache
    {
        public static readonly AsyncOperationCache Instance = new AsyncOperationCache();
        readonly Dictionary<CacheKey, Stack<IAsyncOperation>> m_cache = new Dictionary<CacheKey, Stack<IAsyncOperation>>(new CacheKeyComparer());
#if POST_ASYNCOPERATIONCACHE__EVENTS
        class Stats
        {
            internal int m_hits;
            internal int m_misses;
            internal string m_name;
            internal int Value { get { return (int)(((float)m_hits / (m_hits + m_misses)) * 100); } }
        }
        Dictionary<CacheKey, Stats> m_stats = new Dictionary<CacheKey, Stats>(new CacheKeyComparer());
#endif
        internal struct CacheKey
        {
            public Type opType { get; private set; }
            public Type objType { get; private set; }
            internal int m_hashCode;

            internal CacheKey(Type opType, Type objType)
            {
                this.opType = opType;
                this.objType = typeof(object);
                m_hashCode = opType.GetHashCode();
            }

            public override string ToString()
            {
                return objType.Name + "|" + opType.Name;
            }

            public override int GetHashCode()
            {
                return m_hashCode;
            }

            public override bool Equals(object obj)
            {
                return ((CacheKey)obj).m_hashCode == m_hashCode;
            }
        }

        class CacheKeyComparer : EqualityComparer<CacheKey>
        {
            public override int GetHashCode(CacheKey bx)
            {
                return bx.m_hashCode;
            }

            public override bool Equals(CacheKey b1, CacheKey b2)
            {
                return b1.m_hashCode == b2.m_hashCode;
            }
        }

        public void Release<TObject>(IAsyncOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");
            operation.Validate();

            var key = new CacheKey(operation.GetType(), typeof(TObject));
            Stack<IAsyncOperation> operationStack;
            if (!m_cache.TryGetValue(key, out operationStack))
                m_cache.Add(key, operationStack = new Stack<IAsyncOperation>(5));
            operationStack.Push(operation);

#if POST_ASYNCOPERATIONCACHE__EVENTS
            Stats stat;
            if (!m_stats.TryGetValue(key, out stat))
                m_stats.Add(key, stat = new Stats() { m_name = string.Format("AsyncOperationCache[{0}]", key.opType.Name) });
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.AsyncOpCacheCount, stat.m_name, operationStack.Count);
#endif
        }

        public TAsyncOperation Acquire<TAsyncOperation, TObject>()
            where TAsyncOperation : IAsyncOperation, new()
        {
            Debug.Assert(m_cache != null, "AsyncOperationCache.Acquire - m_cache == null.");

            var key = new CacheKey(typeof(TAsyncOperation), typeof(TObject));

            Stack<IAsyncOperation> operationStack;
#if POST_ASYNCOPERATIONCACHE__EVENTS
            Stats stat;
            if (!m_stats.TryGetValue(key, out stat))
                m_stats.Add(key, stat = new Stats() { m_name = string.Format("AsyncOperationCache[{0}]", key.opType.Name) });
#endif
            if (m_cache.TryGetValue(key, out operationStack) && operationStack.Count > 0)
            {
                var op = (TAsyncOperation)operationStack.Pop();
                op.IsValid = true;
                op.ResetStatus();
#if POST_ASYNCOPERATIONCACHE__EVENTS
                stat.m_hits++;
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.AsyncOpCacheHitRatio, stat.m_name, stat.Value);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.AsyncOpCacheCount, stat.m_name, operationStack.Count);
#endif
                return op;
            }
#if POST_ASYNCOPERATIONCACHE__EVENTS
            stat.m_misses++;
            ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.AsyncOpCacheHitRatio, stat.m_name, stat.Value);
#endif
            var op2 = new TAsyncOperation();
            op2.IsValid = true;
            op2.ResetStatus();
            return op2;
        }

        public void Clear()
        {
            Debug.Assert(m_cache != null, "AsyncOperationCache.Clear - m_cache == null.");
            m_cache.Clear();
        }
    }
}
