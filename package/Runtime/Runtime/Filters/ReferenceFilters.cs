

using System;
using System.Linq;
using System.Collections.Generic;

namespace Unity.Tiny
{
    using Tiny;

    internal static partial class Filter
    {
        #region API
        public static IEnumerable<TinyModule> Deref(this IEnumerable<TinyModule.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<TinyModule>>().Deref(registry);
        }

        public static IEnumerable<TinyEntityGroup> Deref(this IEnumerable<TinyEntityGroup.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<TinyEntityGroup>>().Deref(registry);
        }

        public static IEnumerable<TinyType> Deref(this IEnumerable<TinyType.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<TinyType>>().Deref(registry);
        }

        public static IEnumerable<TinyEntity> Deref(this IEnumerable<TinyEntity.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<TinyEntity>>().Deref(registry);
        }
        
        public static IEnumerable<TinyPrefabInstance> Deref(this IEnumerable<TinyPrefabInstance.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<TinyPrefabInstance>>().Deref(registry);
        }

        public static IEnumerable<TinyModule.Reference> MissingRef(this IEnumerable<TinyModule.Reference> source, IRegistry registry)
        {
            return source.MissingRef<TinyModule, TinyModule.Reference>(registry);
        }

        public static IEnumerable<TinyEntityGroup.Reference> MissingRef(this IEnumerable<TinyEntityGroup.Reference> source, IRegistry registry)
        {
            return source.MissingRef<TinyEntityGroup, TinyEntityGroup.Reference>(registry);
        }

        public static IEnumerable<TinyType.Reference> MissingRef(this IEnumerable<TinyType.Reference> source, IRegistry registry)
        {
            return source.MissingRef<TinyType, TinyType.Reference>(registry);
        }

        public static IEnumerable<TinyEntity.Reference> MissingRef(this IEnumerable<TinyEntity.Reference> source, IRegistry registry)
        {
            return source.MissingRef<TinyEntity, TinyEntity.Reference>(registry);
        }

        public static IEnumerable<TinyModule.Reference> Ref(this IEnumerable<TinyModule> source)
        {
            return source.Select(m => (TinyModule.Reference)m);
        }

        public static IEnumerable<TinyEntityGroup.Reference> Ref(this IEnumerable<TinyEntityGroup> source)
        {
            return source.Select(g => (TinyEntityGroup.Reference)g);
        }

        public static IEnumerable<TinyType.Reference> Ref(this IEnumerable<TinyType> source)
        {
            return source.Select(t => (TinyType.Reference)t);
        }

        public static IEnumerable<TinyEntity.Reference> Ref(this IEnumerable<TinyEntity> source)
        {
            return source.Select(e => (TinyEntity.Reference)e);
        }

        #endregion // API

        #region Implementation
        private static IEnumerable<T> Deref<T>(this IEnumerable<IReference<T>> source, IRegistry registry) where T : class
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (null == registry)
            {
                throw new ArgumentNullException(nameof(registry));
            }
            return source.DerefImpl(registry);
        }

        private static IEnumerable<T> DerefImpl<T>(this IEnumerable<IReference<T>> source, IRegistry registry) where T : class
        {
            foreach (var sRef in source)
            {
                var s = sRef.Dereference(registry);
                if (null != s)
                {
                    yield return s;
                }
            }
        }

        private static IEnumerable<TRef> MissingRef<T, TRef>(this IEnumerable<TRef> source, IRegistry registry)
            where T : class
            where TRef : IReference<T>
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (null == registry)
            {
                throw new ArgumentNullException(nameof(registry));
            }
            return source.MissingRefImpl<T, TRef>(registry);
        }

        private static IEnumerable<TRef> MissingRefImpl<T, TRef>(this IEnumerable<TRef> source, IRegistry registry)
            where T : class
            where TRef : IReference<T>
        {
            foreach (var sRef in source)
            {
                var s = sRef.Dereference(registry);
                if (null == s)
                {
                    yield return sRef;
                }
            }
        }
        #endregion // Implementation
    }
}

