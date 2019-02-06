
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal delegate T ConstructDelegate<out T>(TinyObject tiny) where T : ITinyComponent;

    internal interface ICacheManager : IContextManager
    {
        void RegisterId<T>(TinyId id) where T : ITinyComponent;
        void RegisterConverter<T>(TinyType.Reference typeRef, bool generated = false);
        T Construct<T>(TinyObject tiny) where T : ITinyComponent;
        Type GetConvertedType(TinyType.Reference typeRef, bool includeGenerated);
        TinyId GetIdForType<T>() where T : ITinyComponent;
        TinyModule GetModuleOf<T>(T obj) where T:IReference;
        void SetTilemapCache(TinyEntity.Reference entity, ITilemapTileDataCache cache);
        ITilemapTileDataCache GetTilemapCache(TinyEntity.Reference entity);
    }

    internal interface ITilemapTileDataCache
    {
        void SetTileDataIndex(Vector2Int position, int index);
        int GetTileDataIndex(Vector2Int position);
        int GetTileDataSize();
    }

    internal interface ICacheManagerInternal : ICacheManager
    {
        void RegisterComponentConstructor<T>(ConstructDelegate<T> constructor) where T : ITinyComponent;
    }

    [ContextManager(ContextUsage.All)]
    [UsedImplicitly]
    internal class TinyCacheManager : ContextManager, ICacheManagerInternal
    {
        private interface IComponentCache { }

        private class TinyComponentCache<T> : IComponentCache where T : ITinyComponent
        {
            public TinyId Id { get; set; } = TinyId.Empty;
            public ConstructDelegate<T> Constructor { get; set; }
        }

        private readonly Dictionary<Type, IComponentCache> m_ComponentCachePerType = new Dictionary<Type, IComponentCache>();
        private readonly Dictionary<TinyType.Reference, Type> m_ConverterPerType = new Dictionary<TinyType.Reference, Type>();
        private readonly Dictionary<TinyType.Reference, bool> m_GeneratedConverterPerType = new Dictionary<TinyType.Reference, bool>();
        private readonly Dictionary<IReference, TinyModule> m_ObjToModule = new Dictionary<IReference, TinyModule>();
        private readonly Dictionary<TinyEntity.Reference, ITilemapTileDataCache> m_TilemapCache = new Dictionary<TinyEntity.Reference, ITilemapTileDataCache>();

        public TinyCacheManager(TinyContext context)
            :base(context)
        {
            foreach (var typeAttribute in TinyAttributeScanner.GetMethodAttributes<TinyCachableAttribute>())
            {
                typeAttribute.Method.Invoke(null, new object[] { this });
            }
        }

        void IContextManager.Load()
        {
            foreach (var module in Registry.FindAllByType<TinyModule>())
            {
                if (!module.IsRuntimeIncluded)
                {
                    continue;
                }

                foreach (var type in module.Types)
                {
                    m_ObjToModule[type] = module;
                }
                
                foreach (var group in module.EntityGroups)
                {
                    m_ObjToModule[group] = module;
                }

                foreach (var configuration in module.Configurations)
                {
                    m_ObjToModule[configuration] = module;
                }
            }
        }

        public void RegisterId<T>(TinyId id) where T : ITinyComponent
        {
            GetOrCreateComponentCache<T>().Id = id;
        }

        public void RegisterConverter<T>(TinyType.Reference typeRef, bool generated = false)
        {
            m_ConverterPerType[typeRef] = typeof(T);
            if (generated)
            {
                m_GeneratedConverterPerType[typeRef] = true;
            }
        }

        public void RegisterComponentConstructor<T>(ConstructDelegate<T> constructor) where T : ITinyComponent
        {
            GetOrCreateComponentCache<T>().Constructor = constructor;
        }

        public T Construct<T>(TinyObject tiny) where T : ITinyComponent
        {
            if (GetComponentCache<T>() is var cache)
            {
                Assert.IsNotNull(cache.Constructor);
                return cache.Constructor.Invoke(tiny);
            }

            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: Cannot construct a Tiny Component instance, since no constructor was added for type {typeof(T).Name}");
        }

        public Type GetConvertedType(TinyType.Reference typeRef, bool includeGenerated)
        {
            m_ConverterPerType.TryGetValue(typeRef, out var type);

            if (includeGenerated || !m_GeneratedConverterPerType.ContainsKey(typeRef))
            {
                return type;
            }

            return null;
        }

        public TinyId GetIdForType<T>() where T : ITinyComponent
        {
            return GetComponentCache<T>()?.Id ?? TinyId.Empty;
        }

        public TinyModule GetModuleOf<T>(T obj) where T : IReference
        {
            // Fast path
            if (m_ObjToModule.TryGetValue(obj, out var module))
            {
                return module;
            }
            
            // Slow path, look in non-runtime modules.
            foreach (var m in Context.Registry.FindAllByType<TinyModule>())
            {
                if (m.IsRuntimeIncluded)
                {
                    continue;
                }

                switch (obj)
                {
                    case TinyType.Reference typeRef when (m.Types.Contains(typeRef) || m.Configurations.Contains(typeRef)):
                        return m;
                    case TinyEntityGroup.Reference groupRef when m.EntityGroups.Contains(groupRef):
                        return m;
                }
            }

            return null;
        }

        private TinyComponentCache<T> GetComponentCache<T>() where T : ITinyComponent
        {
            return m_ComponentCachePerType.TryGetValue(typeof(T), out var cache) ? cache as TinyComponentCache<T> : null;
        }

        private TinyComponentCache<T> GetOrCreateComponentCache<T>() where T : ITinyComponent
        {
            var type = typeof(T);
            if (m_ComponentCachePerType.TryGetValue(type, out var cache))
            {
                return cache as TinyComponentCache<T>;
            }

            return (m_ComponentCachePerType[type] = new TinyComponentCache<T>()) as TinyComponentCache<T>;
        }

        public void SetTilemapCache(TinyEntity.Reference entity, ITilemapTileDataCache cache)
        {
            m_TilemapCache[entity] = cache;
        }

        public ITilemapTileDataCache GetTilemapCache(TinyEntity.Reference entity)
        {
            if (m_TilemapCache.TryGetValue(entity, out var cache))
            {
                return cache;
            }
            return null;
        }
    }
}
