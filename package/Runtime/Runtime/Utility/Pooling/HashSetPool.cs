using System.Collections.Generic;

namespace Unity.Tiny
{
    internal static class HashSetPool<T>
    {
        private static readonly ObjectPool<HashSet<T>> s_Pool = new ObjectPool<HashSet<T>>(null, l => l.Clear());

        public static HashSet<T> Get(LifetimePolicy lifetime = LifetimePolicy.Frame)
        {
            return s_Pool.Get(lifetime);
        }

        public static void Release(HashSet<T> toRelease)
        {
            s_Pool.Release(toRelease);
        }
    }
}