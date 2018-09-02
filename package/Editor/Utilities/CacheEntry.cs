using System;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    [Serializable]
    public struct CacheEntry
    {
        public Hash128 Hash  { get; set; }
        public GUID Guid  { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is CacheEntry))
                return false;

            var rhs = (CacheEntry)obj;
            return rhs == this;
        }

        public static bool operator ==(CacheEntry x, CacheEntry y)
        {
            return x.Hash == y.Hash && x.Guid == y.Guid;
        }

        public static bool operator !=(CacheEntry x, CacheEntry y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode() ^ Guid.GetHashCode();
        }
    }
}
