

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Tiny
{
    using Tiny;

    internal static partial class Filter
    {
        #region API
        public static IEnumerable<TinyType.Reference> ConfigurationTypeRefs(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.ConfigurationTypeRefsImpl();
        }

        public static IEnumerable<TinyType> ConfigurationTypes(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.ConfigurationTypesImpl();
        }
        
        public static IEnumerable<TinyType.Reference> ComponentTypeRefs(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.ComponentTypeRefsImpl();
        }

        public static IEnumerable<TinyType> ComponentTypes(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.ComponentTypesImpl();
        }

        public static IEnumerable<TinyType.Reference> StructTypeRefs(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.StructTypeRefsImpl();
        }

        public static IEnumerable<TinyType> StructTypes(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.StructTypesImpl();
        }

        public static IEnumerable<TinyType.Reference> EnumTypeRefs(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EnumTypeRefsImpl();
        }

        public static IEnumerable<TinyType> EnumTypes(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EnumTypesImpl();
        }

        public static IEnumerable<TinyEntityGroup.Reference> SceneRefs(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.SceneRefsImpl();
        }

        public static IEnumerable<TinyEntityGroup> EntityGroups(this IEnumerable<TinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EntityGroupsImpl();
        }

        public static IEnumerable<TinyEntity.Reference> EntityRefs(this IEnumerable<TinyModule> source)
        {
            return source.EntityGroups().EntityRefs();
        }

        public static IEnumerable<TinyEntity> Entities(this IEnumerable<TinyModule> source)
        {
            return source.EntityGroups().Entities();
        }
        
        #endregion // API

        #region Implementation
        private static IEnumerable<TinyType.Reference> ConfigurationTypeRefsImpl(this IEnumerable<TinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Configurations)
                {
                    if (!TinyType.Reference.None.Equals(typeRef))
                    {
                        yield return typeRef;
                    }
                }
            }
        }

        private static IEnumerable<TinyType> ConfigurationTypesImpl(this IEnumerable<TinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Configurations)
                {
                    var type = typeRef.Dereference(module.Registry);
                    if (null != type)
                    {
                        yield return type;
                    }
                }
            }
        }
        private static IEnumerable<TinyType.Reference> ComponentTypeRefsImpl(this IEnumerable<TinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Components)
                {
                    if (!TinyType.Reference.None.Equals(typeRef))
                    {
                        yield return typeRef;
                    }
                }
            }
        }

        private static IEnumerable<TinyType> ComponentTypesImpl(this IEnumerable<TinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Components)
                {
                    var type = typeRef.Dereference(module.Registry);
                    if (null != type)
                    {
                        yield return type;
                    }
                }
            }
        }

        private static IEnumerable<TinyType.Reference> StructTypeRefsImpl(this IEnumerable<TinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Structs)
                {
                    if (!TinyType.Reference.None.Equals(typeRef))
                    {
                        yield return typeRef;
                    }
                }
            }
        }

        private static IEnumerable<TinyType> StructTypesImpl(this IEnumerable<TinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Structs)
                {
                    var type = typeRef.Dereference(module.Registry);
                    if (null != type)
                    {
                        yield return type;
                    }
                }
            }
        }

        private static IEnumerable<TinyType.Reference> EnumTypeRefsImpl(this IEnumerable<TinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Enums)
                {
                    if (!TinyType.Reference.None.Equals(typeRef))
                    {
                        yield return typeRef;
                    }
                }
            }
        }

        private static IEnumerable<TinyType> EnumTypesImpl(this IEnumerable<TinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Enums)
                {
                    var type = typeRef.Dereference(module.Registry);
                    if (null != type)
                    {
                        yield return type;
                    }
                }
            }
        }

        private static IEnumerable<TinyEntityGroup.Reference> SceneRefsImpl(this IEnumerable<TinyModule> source)
        {
            foreach(var module in source)
            {
                foreach(var entityGroupRefs in module.EntityGroups)
                {
                    if (!TinyEntityGroup.Reference.None.Equals(entityGroupRefs))
                    {
                        yield return entityGroupRefs;
                    }
                }
            }
        }

        private static IEnumerable<TinyEntityGroup> EntityGroupsImpl(this IEnumerable<TinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var entityGroupRef in module.EntityGroups)
                {
                    var entityGroup = entityGroupRef.Dereference(module.Registry);
                    if (null != entityGroup)
                    {
                        yield return entityGroup;
                    }
                }
            }
        }
        
        #endregion // Implementation
    }

}

