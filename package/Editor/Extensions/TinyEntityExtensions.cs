
using System;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class TinyEntityExtensions
    {
        public static TTinyComponent GetComponent<TTinyComponent>(this TinyEntity entity)
            where TTinyComponent : struct, ITinyComponent
        {
            return entity?.ComponentHelper<TTinyComponent>(entity.GetComponent) ?? default;
        }

        public static TTinyComponent AddComponent<TTinyComponent>(this TinyEntity entity)
            where TTinyComponent : struct, ITinyComponent
        {
            return entity?.ComponentHelper<TTinyComponent>(entity.AddComponent) ?? default;
        }

        public static TTinyComponent GetOrAddComponent<TTinyComponent>(this TinyEntity entity)
            where TTinyComponent : struct, ITinyComponent
        {
            return entity?.ComponentHelper<TTinyComponent>(entity.GetOrAddComponent) ?? default;
        }

        public static TTinyComponent RemoveComponent<TTinyComponent>(this TinyEntity entity)
            where TTinyComponent : struct, ITinyComponent
        {
            return entity?.ComponentHelper<TTinyComponent>(entity.RemoveComponent) ?? default;
        }

        public static bool HasComponent<TTinyComponent>(this TinyEntity entity)
            where TTinyComponent : struct, ITinyComponent
        {
            return entity?.ComponentHelper<TTinyComponent>(entity.GetComponent).IsValid ?? false;
        }

        private static TTinyComponent ComponentHelper<TTinyComponent>(this TinyEntity entity, Func<TinyType.Reference, TinyObject> helper)
            where TTinyComponent : struct, ITinyComponent
        {
            if (null == entity)
            {
                return default;
            }
            var registry = entity.Registry;
            var typeRef = new TinyType.Reference(registry.CacheManager.GetIdForType<TTinyComponent>(), string.Empty);
            var tiny = helper.Invoke(typeRef);
            return null != tiny ? registry.CacheManager.Construct<TTinyComponent>(tiny) : default;
        }

        public static TinyObject GetComponent(this TinyEntity entity, TinyId id)
        {
            var type = entity.Registry.FindById<TinyType>(id);
            return !type.IsComponent ? null : entity.GetComponent((TinyType.Reference)type);
        }

        public static TinyObject AddComponent(this TinyEntity entity, TinyId id)
        {
            var type = entity.Registry.FindById<TinyType>(id);
            return !type.IsComponent ? null : entity.AddComponent((TinyType.Reference)type);
        }

        public static TinyObject GetOrAddComponent(this TinyEntity entity, TinyId id)
        {
            var type = entity.Registry.FindById<TinyType>(id);
            return !type.IsComponent ? null : entity.GetOrAddComponent((TinyType.Reference)type);
        }

        public static TinyEntity GetRoot(this TinyEntity entity)
        {
            var parent = entity.Parent();
            if (parent.Equals(TinyEntity.Reference.None))
            {
                return entity;
            }

            return GetRoot(parent.Dereference(entity.Registry));
        }

        public static TinyEntity.Reference AsReference(this TinyEntity entity)
        {
            if (null == entity)
            {
                return TinyEntity.Reference.None;
            }
            return (TinyEntity.Reference) entity;
        }
    }
}


