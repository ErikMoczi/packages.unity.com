

using System;
using System.Collections.Generic;

namespace Unity.Tiny
{
    using Tiny;

    internal static partial class Filter
    {
        #region API
        public static IEnumerable<TinyEntity.Reference> EntityRefs(this IEnumerable<TinyEntityGroup> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EntityRefsImpl();
        }

        public static IEnumerable<TinyEntity> Entities(this IEnumerable<TinyEntityGroup> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EntitiesImpl();
        }
        #endregion // API

        #region Implementation
        private static IEnumerable<TinyEntity.Reference> EntityRefsImpl(this IEnumerable<TinyEntityGroup> source)
        {
            foreach(var entityGroup in source)
            {
                foreach(var entityRef in entityGroup.Entities)
                {
                    if (!TinyEntity.Reference.None.Equals(entityRef))
                    {
                        yield return entityRef;
                    }
                }
            }
        }

        private static IEnumerable<TinyEntity> EntitiesImpl(this IEnumerable<TinyEntityGroup> source)
        {
            foreach (var entityGroup in source)
            {
                foreach (var entityRef in entityGroup.Entities)
                {
                    var entity = entityRef.Dereference(entityGroup.Registry);
                    if (null != entity)
                    {
                        yield return entity;
                    }
                }
            }
        }
        #endregion // Implementation
    }

}

