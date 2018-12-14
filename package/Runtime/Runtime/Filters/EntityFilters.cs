

using System;
using System.Collections.Generic;

namespace Unity.Tiny
{
    using Tiny;

    internal static partial class Filter
    {
        #region API
        public static IEnumerable<TinyObject> GetAllComponents(this IEnumerable<TinyEntity> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.GetAllComponentsImpl();
        }

        public static IEnumerable<TinyObject> GetComponents(this IEnumerable<TinyEntity> source, TinyType.Reference typeRef)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (TinyType.Reference.None.Equals(typeRef))
            {
                throw new ArgumentException(nameof(typeRef));
            }

            return source.GetComponentsImpl(typeRef);
        }

        public static IEnumerable<TinyObject> GetComponents(this IEnumerable<TinyEntity> source, TinyType type)
        {
            return source.GetComponents((TinyType.Reference)type);
        }

        public static IEnumerable<TinyEntity> WithComponent(this IEnumerable<TinyEntity> source, TinyType.Reference typeRef)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            if (TinyType.Reference.None.Equals(typeRef))
            {
                throw new ArgumentException(nameof(typeRef));
            }

            return source.WithComponentImpl(typeRef);
        }

        public static IEnumerable<TinyEntity> WithComponent(this IEnumerable<TinyEntity> source, TinyType type)
        {
            return source.WithComponent((TinyType.Reference)type);
        }

        public static IEnumerable<TinyEntity> WithoutComponent(this IEnumerable<TinyEntity> source, TinyType.Reference typeRef)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (TinyType.Reference.None.Equals(typeRef))
            {
                throw new ArgumentException(nameof(typeRef));
            }

            return source.WithoutComponentImpl(typeRef);
        }

        public static IEnumerable<TinyEntity> WithoutComponent(this IEnumerable<TinyEntity> source, TinyType type)
        {
            return source.WithoutComponent((TinyType.Reference)type);
        }
        #endregion // API

        #region Implementation

        private static IEnumerable<TinyObject> GetAllComponentsImpl(this IEnumerable<TinyEntity> source)
        {
            foreach(var entity in source)
            {
                foreach(var component in entity.Components)
                {
                    yield return component;
                }
            }
        }

        private static IEnumerable<TinyObject> GetComponentsImpl(this IEnumerable<TinyEntity> source, TinyType.Reference typeRef)
        {
            foreach (var entity in source)
            {
                var component = entity.GetComponent(typeRef);
                if (null != component)
                {
                    yield return component;
                }
            }
        }

        private static IEnumerable<TinyEntity> WithComponentImpl(this IEnumerable<TinyEntity> source, TinyType.Reference typeRef)
        {
            foreach (var entity in source)
            {
                var component = entity.GetComponent(typeRef);
                if (null != component)
                {
                    yield return entity;
                }
            }
        }

        private static IEnumerable<TinyEntity> WithoutComponentImpl(this IEnumerable<TinyEntity> source, TinyType.Reference typeRef)
        {
            foreach (var entity in source)
            {
                var component = entity.GetComponent(typeRef);
                if (null == component)
                {
                    yield return entity;
                }
            }
        }
        #endregion // Implementation
    }

}

