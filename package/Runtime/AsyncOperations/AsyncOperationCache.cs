using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public class AsyncOperationCache
    {
        public static readonly AsyncOperationCache Instance = new AsyncOperationCache();
        readonly Dictionary<CacheKey, Stack<IAsyncOperation>> m_cache = new Dictionary<CacheKey, Stack<IAsyncOperation>>(new CacheKeyComparer());
        readonly Dictionary<CacheKey, Stack<IAsyncOperation>> m_delayedReleases = new Dictionary<CacheKey, Stack<IAsyncOperation>>(new CacheKeyComparer());
        int m_delayedFrame = -1;

        internal struct CacheKey
        {
            public Type opType { get; private set; }
            public Type objType { get; private set; }

            internal CacheKey(Type opType, Type objType)
            {
                this.opType = opType;
                this.objType = objType;
            }

            public override string ToString()
            {
                return objType.Name + "|" + opType.Name;
            }

            public override int GetHashCode()
            {
                var hash = 23 * 37 + opType.GetHashCode();
                return hash * 37 + objType.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is CacheKey))
                    return false;

                var test = (CacheKey)obj;

                return (test.opType == opType && test.objType == objType);
            }
        }

        class CacheKeyComparer : EqualityComparer<CacheKey>
        {
            public override int GetHashCode(CacheKey bx)
            {
                return bx.GetHashCode();
            }

            public override bool Equals(CacheKey b1, CacheKey b2)
            {
                return b1.opType == b2.opType && b1.objType == b2.objType;
            }
        }

        public void Release<TObject>(IAsyncOperation operation)
        {
            Debug.Assert(m_delayedReleases != null, "AsyncOperationCache.Release - m_cache == null.");
            if (operation == null)
                throw new ArgumentNullException("operation");
            operation.Validate();

            MoveDelayedOperationsToCache();

            var key = new CacheKey(operation.GetType(), typeof(TObject));
            Stack<IAsyncOperation> operationStack;
            if (!m_delayedReleases.TryGetValue(key, out operationStack))
                m_delayedReleases.Add(key, operationStack = new Stack<IAsyncOperation>());
            operationStack.Push(operation);
        }

        public TAsyncOperation Acquire<TAsyncOperation, TObject>()
            where TAsyncOperation : IAsyncOperation, new()
        {
            Debug.Assert(m_cache != null, "AsyncOperationCache.Acquire - m_cache == null.");

            MoveDelayedOperationsToCache();

            Stack<IAsyncOperation> operationStack;
            var key = new CacheKey(typeof(TAsyncOperation), typeof(TObject));
            if (m_cache.TryGetValue(key, out operationStack) && operationStack.Count > 0)
            {
                var op = (TAsyncOperation)operationStack.Pop();
                op.IsValid = true;
                op.ResetStatus();
                return op;
            }
            var op2 = new TAsyncOperation();
            op2.IsValid = true;
            op2.ResetStatus();
            return op2;
        }

        void MoveDelayedOperationsToCache()
        {
            if (Time.frameCount > m_delayedFrame)
            {
                m_delayedFrame = Time.frameCount;
                foreach (var kvp in m_delayedReleases)
                {
                    Stack<IAsyncOperation> operationStack;
                    if (!m_cache.TryGetValue(kvp.Key, out operationStack))
                        m_cache.Add(kvp.Key, operationStack = new Stack<IAsyncOperation>());
                    foreach (var o in kvp.Value)
                    {
                        o.Validate();
                        o.IsValid = false;
                        operationStack.Push(o);
                    }
                    kvp.Value.Clear();
                }
            }
        }

        public void Clear()
        {
            Debug.Assert(m_cache != null, "AsyncOperationCache.Clear - m_cache == null.");
            m_cache.Clear();
        }
    }
}
