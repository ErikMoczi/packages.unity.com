using System;
using System.Collections;

namespace Unity.MemoryProfiler.Editor.Database.Operation
{
    // Handles cache logic for an array of entries.
    // Each entry has a value of DataT type and a string.
    internal struct ValueStringArrayCache<DataT> where DataT : IComparable
    {
        DataT[] m_Cache;
        string[] m_CacheString;
        BitArray m_CacheDirty;
        enum State
        {
            Uninitialized,
            Cache_Direct, // cache each value when requested. only cache indices in [0, Size[ range
            Cache_Single, // cache only one value and assume all indices have the same value
            NoCache, // nothing is cached
        }
        State m_State;
        public int Size { get { return m_Cache == null ? 0 : m_Cache.Length; } }
        public bool Initialized { get { return m_State != State.Uninitialized; } }

        // cache only one value and assume all indices have the same value
        public void InitSingle()
        {
            m_State = State.Cache_Single;
            m_Cache = new DataT[1];
            m_CacheString = new string[1];
            m_CacheDirty = new BitArray(1);
            SetAllDirty();
        }

        // nothing is cached
        public void InitNoCache()
        {
            m_State = State.NoCache;
        }

        // cache each value when requested. only cache indices in [0, Size[ range
        public void InitDirect(int size)
        {
            m_State = State.Cache_Direct;
            m_Cache = new DataT[size];
            m_CacheString = new string[size];
            m_CacheDirty = new BitArray(size);
            SetAllDirty();
        }

        // Initialize the cache using the appropriate associativity for a given expression 
        public void InitForExpression(TypedExpression<DataT> exp)
        {
            if (exp.HasMultipleRow())
            {
                long rowCount = exp.RowCount();
                if (rowCount < 0)
                {
                    //unknown row count. cannot use cache
                    InitNoCache();
                }
                else
                {
                    InitDirect((int)rowCount);
                }
            }
            else
            {
                //only one result. all index requested will yield the same result
                InitSingle();
            }
        }

        bool TryGetCacheIndex(int indexIn, out int cacheIndex)
        {
            switch (m_State)
            {
                case State.Cache_Single:
                    cacheIndex = 0;
                    return true;
                case State.Cache_Direct:
                    if (indexIn < 0 || indexIn > Size) throw new IndexOutOfRangeException("Index requested '" + indexIn + "' is outside of cache range [0, " + Size + "[");
                    cacheIndex = indexIn;
                    return true;
                case State.NoCache:
                    cacheIndex = 0;
                    return false;
                case State.Uninitialized:
                    throw new InvalidOperationException("Cannot use 'ValueStringArrayCache' while in uninitialized state");
            }
            throw new InvalidOperationException();
        }

        public void SetAllDirty(bool dirty = true)
        {
            if (m_CacheDirty == null) return;
            m_CacheDirty.SetAll(dirty);
        }

        public void SetEntryDirty(int index, bool dirty = true)
        {
            if (m_CacheDirty == null) return;
            m_CacheDirty.Set(index, dirty);
        }

        // put value from expression in cache only if entry is dirty
        public bool UpdateFromExpression(int cacheIndex, TypedExpression<DataT> exp, long row)
        {
            if (m_CacheDirty[cacheIndex])
            {
                if (exp == null)
                {
                    m_Cache[cacheIndex] = default(DataT);
                    m_CacheString[cacheIndex] = "";
                }
                else
                {
                    m_Cache[cacheIndex] = exp.GetValue(row);
                    if (exp.StringValueDiffers)
                    {
                        m_CacheString[cacheIndex] = exp.GetValueString(row);
                    }
                    else
                    {
                        m_CacheString[cacheIndex] = m_Cache[cacheIndex].ToString();
                    }
                }
                m_CacheDirty[cacheIndex] = false;
                return true;
            }
            return false;
        }

        // put value in cache only if entry is dirty
        public bool Update(int index, DataT value, string valueString)
        {
            int cacheIndex;
            if (TryGetCacheIndex(index, out cacheIndex))
            {
                // use cache
                if (m_CacheDirty[index])
                {
                    m_Cache[index] = value;
                    m_CacheString[index] = valueString;
                    m_CacheDirty[index] = false;
                    return true;
                }
                return false;
            }
            else
            {
                // not caching
                throw new InvalidOperationException("Cannot call 'ValueStringArrayCache.Update' while in NoCache state");
            }
        }

        // put value in cache only if entry is dirty
        public bool Update(int index, Func<DataT> value, Func<string> valueString)
        {
            int cacheIndex;
            if (TryGetCacheIndex(index, out cacheIndex))
            {
                // use cache
                if (m_CacheDirty[index])
                {
                    m_Cache[index] = value();
                    m_CacheString[index] = valueString();
                    m_CacheDirty[index] = false;
                    return true;
                }
                return false;
            }
            else
            {
                // not caching
                throw new InvalidOperationException("Cannot call 'ValueStringArrayCache.Update' while in NoCache state");
            }
        }

        // retrieve cached value.
        public bool TryGetCachedValue(int index, out DataT value)
        {
            int cacheIndex;
            if (TryGetCacheIndex(index, out cacheIndex))
            {
                if (!m_CacheDirty[cacheIndex])
                {
                    value = m_Cache[cacheIndex];
                    return true;
                }
            }
            value = default(DataT);
            return false;
        }

        // retrieve cached string value.
        public bool TryGetCachedValueString(int index, out string value)
        {
            int cacheIndex;
            if (TryGetCacheIndex(index, out cacheIndex))
            {
                if (!m_CacheDirty[cacheIndex])
                {
                    value = m_CacheString[cacheIndex];
                    return true;
                }
            }
            value = null;
            return false;
        }

        public DataT GetValueFromExpression(int index, TypedExpression<DataT> exp, long row)
        {
            int cacheIndex;
            if (TryGetCacheIndex(index, out cacheIndex))
            {
                // use cache
                UpdateFromExpression(cacheIndex, exp, row);
                return m_Cache[cacheIndex];
            }
            else
            {
                // not caching
                return exp.GetValue(row);
            }
        }

        public string GetValueStringFromExpression(int index, TypedExpression<DataT> exp, long row)
        {
            int cacheIndex;
            if (TryGetCacheIndex(index, out cacheIndex))
            {
                // use cache
                UpdateFromExpression(cacheIndex, exp, row);
                return m_CacheString[cacheIndex];
            }
            else
            {
                // not caching
                return exp.GetValueString(row);
            }
        }
    }

}