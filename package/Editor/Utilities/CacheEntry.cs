using System;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    [Serializable]
    public struct CacheEntry
    {
        public Hash128 hash;
        public GUID guid;

        public override bool Equals(object obj)
        {
            if (!(obj is CacheEntry))
                return false;

            var rhs = (CacheEntry)obj;
            return rhs == this;
        }

        public static bool operator ==(CacheEntry x, CacheEntry y)
        {
            return x.hash == y.hash && x.guid == y.guid;
        }

        public static bool operator !=(CacheEntry x, CacheEntry y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return hash.GetHashCode() ^ guid.GetHashCode();
        }
    }
}
