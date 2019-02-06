
using System;
using System.Collections.Generic;

namespace Unity.Tiny
{
    internal static class TinyPropertyMetaData<T>
    {
        #region Static
        private static Dictionary<T, Dictionary<Type, IPropertyAttribute>> s_Map;
        #endregion

        #region API
        public static void RegisterAttribute<TAttribute>(T target, TAttribute attribute) where TAttribute : class, IPropertyAttribute
        {
            var dict = NonNull(target);
            dict[attribute.GetType()] = attribute;
        }

        public static void UnregisterAttribute<TAttribute>(T target, TAttribute attribute) where TAttribute : class, IPropertyAttribute
        {
            if (!MapContainsTargetAttribute(target, attribute))
            {
                return;
            }
            var dict = s_Map[target];
            dict.Remove(attribute.GetType());
            if (dict.Count == 0)
            {
                s_Map.Remove(target);
            }
        }

        public static bool HasAttribute<TAttribute>(T target) where TAttribute : class, IPropertyAttribute
        {
            if (!MapContainsTarget(target))
            {
                return false;
            }
            var dict = NonNull(target);
            return dict.ContainsKey(typeof(TAttribute));
        }

        public static TAttribute GetAttribute<TAttribute>(T target) where TAttribute : class, IPropertyAttribute
        {
            if (HasAttribute<TAttribute>(target))
            {
                return (TAttribute)NonNull(target)[typeof(TAttribute)];
            }
            return null;
        }

        public static void RemoveAllAttributes(T target)
        {
            if (!MapContainsTarget(target))
            {
                return;
            }
            s_Map.Remove(target);
        }

        public static IEnumerable<IPropertyAttribute> GetAllAttributes(T target)
        {
            if (!MapContainsTarget(target))
            {
                yield break;
            }
            foreach(var attribute in NonNull(target).Values)
            {
                yield return attribute;
            }
        }
        #endregion

        #region Helpers
        private static bool MapExists() { return null != s_Map; }
        private static bool MapContainsTarget(T target)
        {
            if (!MapExists())
            {
                return false;
            }
            return s_Map.ContainsKey(target);
        }
        private static bool MapContainsTargetAttribute<TAttribute>(T target, TAttribute attribute) where TAttribute : IPropertyAttribute
        {
            if (!MapContainsTarget(target))
            {
                return false;
            }
            return null != s_Map[target];
        }

        private static Dictionary<T, Dictionary<Type, IPropertyAttribute>> NonNull()
        {
            if (null == s_Map)
            {
                s_Map = new Dictionary<T, Dictionary<Type, IPropertyAttribute>>();
            }
            return s_Map;
        }

        private static Dictionary<Type, IPropertyAttribute> NonNull(T property)
        {
            NonNull();
            Dictionary<Type, IPropertyAttribute> dict = null;
            if (!s_Map.TryGetValue(property, out dict))
            {
                dict = s_Map[property] = new Dictionary<Type, IPropertyAttribute>();    
            }
            return dict;
        }
        #endregion
    }

}
