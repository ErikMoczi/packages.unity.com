using System;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    interface ICachedData { }

    [Serializable]
    public class CachedInfo : ICachedData
    {
        public CacheEntry Asset { get; set; }
        public CacheEntry[] Dependencies { get; set; }
        public object[] Data { get; set; }
    }

    [Serializable]
    public struct CacheEntry
    {
        public enum EntryType
        {
            Asset,
            File,
            Data
        }

        public Hash128 Hash { get; internal set; }
        public GUID Guid { get; internal set; }
        public EntryType Type { get; internal set; }
        public string File { get; internal set; }

        public bool IsValid()
        {
            return Hash.isValid && !Guid.Empty();
        }

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

        public override string ToString()
        {
            if (Type == EntryType.File)
                return string.Format("{{{0}, {1}}}", File, Hash);
            return string.Format("{{{0}, {1}}}", Guid, Hash);
        }
    }
}
