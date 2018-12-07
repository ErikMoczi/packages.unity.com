using System.Collections.Generic;

namespace Unity.Tiny
{
    internal static class DictionaryPool<TKey, TValue>
    {
        private static readonly ObjectPool<Dictionary<TKey, TValue>> s_Pool = new ObjectPool<Dictionary<TKey, TValue>>(null, l => l.Clear());

        public static Dictionary<TKey, TValue> Get(LifetimePolicy lifetime = LifetimePolicy.Frame)
        {
            return s_Pool.Get(lifetime);
        }

        public static void Release(Dictionary<TKey, TValue> toRelease)
        {
            s_Pool.Release(toRelease);
        }
    }
}