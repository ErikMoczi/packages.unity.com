

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal enum TinyRegistryEventType
    {
        Registered = 0,
        Unregistered = 1,
    }

    internal interface IRegistryObject : IIdentified<TinyId>, IDocumented
    {
        IRegistry Registry { get; }
        string Name { get; }
        
        void Refresh();
    }

    internal interface IRegistry
    {
        int Count { get; }
        string SourceIdentifier { get; }
        IContext Context { get; }
        ICacheManager CacheManager { get; }
        IDisposable DontTrackChanges();
        
        void Register(IRegistryObject t);
        void Unregister(IRegistryObject t);
        void Unregister(TinyId id);
        void UnregisterAllBySource(string identifier);
        void Clear();
        T Dereference<TReference, T>(TReference reference) where TReference : IReference<T> where T : class, IRegistryObject;
        TinyRegistryObjectBase Dereference(IReference reference);

        IDisposable SourceIdentifierScope(string identifier);
        TinyProject CreateProject(TinyId id, string name);
        TinyModule CreateModule(TinyId id, string name);
        TinyType CreateType(TinyId id, string name, TinyTypeCode typeCode);
        TinyEntityGroup CreateEntityGroup(TinyId id, string name);
        TinyEntity CreateEntity(TinyId id, string name);
        TinyPrefabInstance CreatePrefabInstance(TinyId id, string name);
        
        IRegistryObject FindById(TinyId id);
        T FindById<T>(TinyId id) where T : class, IRegistryObject;
        bool TryFindById<T>(TinyId id, out T t) where T : class, IRegistryObject;
        T FindByName<T>(string name) where T : class, IRegistryObject;
        bool TryFindByName<T>(string name, out T t) where T : class, IRegistryObject;
        T AnyByType<T>() where T : class, IRegistryObject;
        IEnumerable<T> FindAllByType<T>() where T : class, IRegistryObject;
        bool HasObjectFromSource(string identifier);
        IEnumerable<IRegistryObject> FindAllBySource(string identifier);
        bool TryGetSourceIdentifier(TinyId id, out string identifier);
        void ChangeAllSource(string fromIdentifier, string toIdentifier);
        void ChangeSource(TinyId id, string toIdentifier);
        IEnumerable<IRegistryObject> All();
        IEnumerable<IRegistryObject> AllUnregistered();
        void ClearUnregisteredObjects();
    }
    
    internal sealed class TinyRegistry : IRegistry
    {
        public const string BuiltInSourceIdentifier = "__builtin__";
        public const string DefaultSourceIdentifier = "__default__";
        public const string TempSourceIdentifier = "__temp__";
        
        private static int s_RegistryId;
        
        private readonly IDictionary<TinyId, IRegistryObject> m_Objects = new Dictionary<TinyId, IRegistryObject>();
        private readonly Dictionary<Type, HashSet<TinyId>> m_IdsByType = new Dictionary<Type, HashSet<TinyId>>();
        private readonly int m_Id = s_RegistryId++;

        private readonly Stack<string> m_SourceIdentifierStack = new Stack<string>();
        private readonly Dictionary<string, HashSet<TinyId>> m_SourceIdentifierMap = new Dictionary<string, HashSet<TinyId>>();

        private readonly HashSet<IRegistryObject> m_UnregisteredObjects = new HashSet<IRegistryObject>();
        
        /// <summary>
        /// Shared version storage used by registry objects
        /// </summary>
        private readonly TinyVersionStorage m_VersionStorage;

        public TinyRegistry(IContext context, TinyVersionStorage versionStorage)
        {
            m_SourceIdentifierStack.Push(DefaultSourceIdentifier);
            m_VersionStorage = versionStorage;
            
            RegisterBuiltInTypes();
            Context = context;
        }
        
        public TinyRegistry() : this(null, new TinyVersionStorage())
        {
        }

        public TinyRegistry(IContext context) : this(context, new TinyVersionStorage())
        {
        }

        public int Count => m_Objects.Count;
        
        public string SourceIdentifier => m_SourceIdentifierStack.Peek();
        private ICacheManager m_CacheManager;

        public ICacheManager CacheManager => m_CacheManager ?? (m_CacheManager = Context.GetManager<ICacheManager>());
        public IContext Context { get; internal set; }

        private class NullDisposable : IDisposable
        {
            public void Dispose()
            {
            }
            public static readonly NullDisposable Default = new NullDisposable();
        }

        public IDisposable DontTrackChanges()
        {
            return m_VersionStorage?.DontTrackChangeScopeInternal() ?? NullDisposable.Default;
        }

        public override string ToString()
        {
            return $"Registry, ID={m_Id}, Count={m_Objects.Count}";
        }

        private void RegisterBuiltInTypes()
        {
            using (new IdentificationScope(this, BuiltInSourceIdentifier))
            {
                foreach (var type in TinyType.BuiltInTypes)
                {
                    Register(type);
                }
            }
        }

        public void Register(IRegistryObject t)
        {
            Assert.IsNotNull(t);
            
            IRegistryObject oldObject;
            if (m_Objects.TryGetValue(t.Id, out oldObject))
            {
                if (oldObject == t)
                {
                    return;
                }
                Unregister(oldObject);
            }
            
            Assert.AreNotEqual(TinyId.Empty, t.Id);
            
            m_Objects[t.Id] = t;
            var type = t.GetType();
            HashSet<TinyId> typeIds;
            if (!m_IdsByType.TryGetValue(type, out typeIds))
            {
                m_IdsByType[type] = typeIds = new HashSet<TinyId>();
            }
            
            SetSourceIdentifier(t);

            if (typeIds.Add(t.Id))
            {
                TinyEventDispatcher.Dispatch(TinyRegistryEventType.Registered, t);
            }
        }
        
        public void Unregister(IRegistryObject t)
        {
            if (t == null)
            {
                return;
            }

            if (!m_Objects.Remove(t.Id))
            {
                return;
            }

            m_UnregisteredObjects.Add(t);

            var type = t.GetType();
            HashSet<TinyId> typeIds;
            if (m_IdsByType.TryGetValue(type, out typeIds))
            {
                if (typeIds.Remove(t.Id))
                {
                    TinyEventDispatcher.Dispatch(TinyRegistryEventType.Unregistered, t);
                }
                if (typeIds.Count == 0)
                {
                    m_IdsByType.Remove(type);
                }
            }
        }
        
        public void Unregister(TinyId id)
        {
            Unregister(FindById(id));
        }
        
        public void UnregisterAllBySource(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                identifier = DefaultSourceIdentifier;
            }
            
            foreach (var obj in FindAllBySource(identifier))
            {
                Unregister(obj);
            }

            m_SourceIdentifierMap.Remove(identifier);
        }

        public void ChangeAllSource(string fromIdentifier, string toIdentifier)
        {
            HashSet<TinyId> source;
            if (!m_SourceIdentifierMap.TryGetValue(fromIdentifier, out source))
            {
                return;
            }

            HashSet<TinyId> destination;
            if (!m_SourceIdentifierMap.TryGetValue(toIdentifier, out destination))
            {
                destination = new HashSet<TinyId>();
                m_SourceIdentifierMap.Add(toIdentifier, destination);
            }
            
            m_SourceIdentifierMap.Remove(fromIdentifier);
            foreach (var id in source)
            {
                destination.Add(id);
            }
        }

        public void ChangeSource(TinyId id, string identifier)
        {
            string fromIdentifier;
            if (!TryGetSourceIdentifier(id, out fromIdentifier))
            {
                throw new Exception($"Id does not exist in the registry '{id}'");
            }

            HashSet<TinyId> source;
            if (!m_SourceIdentifierMap.TryGetValue(fromIdentifier, out source))
            {
                return;
            }

            if (fromIdentifier == identifier)
            {
                return;
            }
            
            HashSet<TinyId> destination;
            if (!m_SourceIdentifierMap.TryGetValue(identifier, out destination))
            {
                destination = new HashSet<TinyId>();
                m_SourceIdentifierMap.Add(identifier, destination);
            }

            source.Remove(id);
            destination.Add(id);

            if (source.Count <= 0)
            {
                m_SourceIdentifierMap.Remove(fromIdentifier);
            }
        }

        public bool TryGetSourceIdentifier(TinyId id, out string identifier)
        {
            foreach (var kvp in m_SourceIdentifierMap)
            {
                if (!kvp.Value.Contains(id))
                {
                    continue;
                }
                
                identifier = kvp.Key;
                return true;
            }

            identifier = null;
            return false;
        }
        
        public void Clear()
        {
            m_Objects.Clear();
            m_IdsByType.Clear();
            m_SourceIdentifierMap.Clear();
            
            RegisterBuiltInTypes();
        }
        
        public T Dereference<TReference, T>(TReference reference)
            where TReference : IReference<T>
            where T : class, IRegistryObject
        {
            T obj;
            return !TryFindById(reference.Id, out obj) ? null : obj;
        }
        
        public TinyRegistryObjectBase Dereference(IReference reference)
        {
            TinyRegistryObjectBase obj;
            return !TryFindById(reference.Id, out obj) ? null : obj;
        }

        private class IdentificationScope : IDisposable
        {
            private readonly TinyRegistry m_Registry;
            
            public IdentificationScope(TinyRegistry registry, string identifier)
            {
                m_Registry = registry;
                m_Registry.m_SourceIdentifierStack.Push(identifier);
            }

            public void Dispose()
            {
                m_Registry.m_SourceIdentifierStack.Pop();
            }
        }

        /// <summary>
        /// When active, identification scopes can be used to associate resource identifiers to newly
        /// created registry objects.
        /// 
        /// These scopes can be nested.
        /// </summary>
        /// <returns>A Disposable object. Dispose</returns>
        public IDisposable SourceIdentifierScope(string identifier)
        {
            if (identifier == BuiltInSourceIdentifier)
            {
                throw new Exception($"The built-in source identifier \"{BuiltInSourceIdentifier}\" cannot be used");
            }

            if (string.IsNullOrEmpty(identifier))
            {
                identifier = DefaultSourceIdentifier;
            }
            return new IdentificationScope(this, identifier);
        }

        private void SetSourceIdentifier(IRegistryObject obj)
        {
            var identifier = SourceIdentifier;
            if (string.IsNullOrEmpty(identifier))
            {
                return;
            }
            HashSet<TinyId> ids;
            if (!m_SourceIdentifierMap.TryGetValue(identifier, out ids))
            {
                ids = m_SourceIdentifierMap[identifier] = new HashSet<TinyId>();
            }

            ids.Add(obj.Id);
        }

        public bool HasObjectFromSource(string identifier)
        {
            return m_SourceIdentifierMap.ContainsKey(identifier);
        }

        public IEnumerable<IRegistryObject> FindAllBySource(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                identifier = DefaultSourceIdentifier;
            }
            
            HashSet<TinyId> ids;
            if (!m_SourceIdentifierMap.TryGetValue(identifier, out ids))
            {
                yield break;
            }

            foreach (var id in ids)
            {
                IRegistryObject obj;
                if (m_Objects.TryGetValue(id, out obj))
                {
                    yield return obj;
                }
            }
        }

        public TinyProject CreateProject(TinyId id, string name)
        {
            var project = new TinyProject(this, m_VersionStorage)
            {
                Id = id,
                Name = name,
                SerializedVersion = TinyProject.CurrentSerializedVersion,
                LastSerializedVersion = TinyProject.CurrentSerializedVersion
            };
            
            m_VersionStorage.MarkAsChanged(project);
            
            Register(project);

            return project;
        }

        public TinyModule CreateModule(TinyId id, string name)
        {
            var module = new TinyModule(this, m_VersionStorage)
            {
                Id = id,
                Name = name,
                SerializedVersion = TinyModule.CurrentSerializedVersion
            };
            
            m_VersionStorage.MarkAsChanged(module);
            
            Register(module);

            return module;
        }
        
        public TinyType CreateType(TinyId id, string name, TinyTypeCode typeCode)
        {
            var type = new TinyType(this, m_VersionStorage)
            {
                Id = id,
                Name = name,
                TypeCode = typeCode
            };
            
            m_VersionStorage.MarkAsChanged(type);

            Register(type);

            return type;
        }
        
        public TinyEntity CreateEntity(TinyId id, string name)
        {
            var entity = new TinyEntity(this, m_VersionStorage)
            {
                Id = id,
                Name = name
            };
            
            m_VersionStorage.MarkAsChanged(entity);
            
            Register(entity);

            return entity;
        }

        public TinyEntityGroup CreateEntityGroup(TinyId id, string name)
        {
            var entityGroup = new TinyEntityGroup(this, m_VersionStorage)
            {
                Id = id,
                Name = name
            };
            
            m_VersionStorage.MarkAsChanged(entityGroup);
            
            
            Register(entityGroup);

            return entityGroup;
        }
        
        public TinyPrefabInstance CreatePrefabInstance(TinyId id, string name)
        {
            var prefabInstance = new TinyPrefabInstance(this, m_VersionStorage)
            {
                Id = id,
                Name = name
            };
            
            m_VersionStorage.MarkAsChanged(prefabInstance);
            
            Register(prefabInstance);

            return prefabInstance;
        }

        public IRegistryObject FindById(TinyId id)
        {
            IRegistryObject t;
            m_Objects.TryGetValue(id, out t);
            return t;
        }

        public T FindById<T>(TinyId id) where T : class, IRegistryObject
        {
            IRegistryObject t;
            m_Objects.TryGetValue(id, out t);
            return t as T;
        }

        public bool TryFindById<T>(TinyId id, out T t) where T : class, IRegistryObject
        {
            IRegistryObject o;
            m_Objects.TryGetValue(id, out o);
            t = o as T;
            return true;
        }
        
        public T FindByName<T>(string name) where T : class, IRegistryObject
        {
            return FindAllByType<T>().FirstOrDefault(obj => string.Equals(obj.Name, name));
        }

        public bool TryFindByName<T>(string name, out T t) where T : class, IRegistryObject
        {
            t = FindByName<T>(name);
            return null != t;
        }

        public T AnyByType<T>() where T : class, IRegistryObject
        {
            return FindAllByType<T>().FirstOrDefault();
        }

        public IEnumerable<T> FindAllByType<T>() where T : class, IRegistryObject
        {
            foreach (var typeKvp in m_IdsByType)
            {
                if (typeof(T).IsAssignableFrom(typeKvp.Key))
                {
                    foreach (var id in typeKvp.Value)
                    {
                        var obj = m_Objects[id];
                        // Debug.Assert(obj is T, $"Cannot cast from {obj.GetType()} to {typeof(T)}");
                        yield return (T) obj;
                    }
                }
            }
        }

        public IEnumerable<IRegistryObject> All()
        {
            return m_Objects.Values;
        }

        public IEnumerable<IRegistryObject> AllUnregistered()
        {
            return m_UnregisteredObjects;
        }

        public void ClearUnregisteredObjects()
        {
            m_UnregisteredObjects.Clear();
        }
    }
}

