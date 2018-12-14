

using System.Collections.Generic;

namespace Unity.Tiny
{
    internal static class ListPool<T>
    {
        private static readonly ObjectPool<List<T>> s_Pool = new ObjectPool<List<T>>(null, l => l.Clear());

        public static List<T> Get(LifetimePolicy lifetime = LifetimePolicy.Frame)
        {
            return s_Pool.Get(lifetime);
        }

        public static void Release(List<T> toRelease)
        {
            s_Pool.Release(toRelease);
        }
    }
}

