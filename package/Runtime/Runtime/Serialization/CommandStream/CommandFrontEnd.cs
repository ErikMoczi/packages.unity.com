

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEngine;

namespace Unity.Tiny.Serialization.CommandStream
{
    internal static class CommandFrontEnd
    {
        private static readonly BinaryObjectReader.PooledObjectFactory s_ObjectFactory = new BinaryObjectReader.PooledObjectFactory();
        private static readonly CommandObjectReader s_CommandObjectReader = new CommandObjectReader(s_ObjectFactory);

        /// <summary>
        /// Defered type creation
        /// </summary>
        private struct CreateTypeCommand
        {
            public string SourceIdentifier;
            public IDictionary<string, object> Data;
        }
        
        /// <summary>
        /// Deferred entity creation
        /// </summary>
        private struct CreateEntityCommand
        {
            public string SourceIdentifier;
            public IDictionary<string, object> Data;
        }
        
        public static void Accept(Stream input, IRegistry registry)
        {
            var reader = new BinaryReader(input);

            var deferredTypeCreationCommands = new List<CreateTypeCommand>();
            var deferredEntityCreationCommands = new List<CreateEntityCommand>();
            var legacyScripts = new Dictionary<string, IList<IDictionary<string, object>>>();
            var sourceIdentifierStack = new Stack<IDisposable>();
            
            var prefabInstances = ListPool<TinyPrefabInstance>.Get();
            var entityGroups = ListPool<TinyEntityGroup>.Get();
            var entities = ListPool<TinyEntity>.Get();

            try
            {
                while (input.Position != input.Length)
                {
                    var command = reader.ReadByte();
                    var size = reader.ReadUInt32();
                    var end = input.Position + size;

                    switch (command)
                    {
                        case CommandType.CreateProject:
                        {
                            Accept(new ObjectContainer(s_CommandObjectReader.ReadObject(reader)), registry, (r, id, name) => r.CreateProject(id, name), TinyProject.Migrate);
                        }
                            break;

                        case CommandType.CreateModule:
                        {
                            Accept(new ObjectContainer(s_CommandObjectReader.ReadObject(reader)), registry, (r, id, name) => r.CreateModule(id, name), TinyModule.Migrate);
                        }
                            break;

                        case CommandType.CreateType:
                        {
                            // Defer type creation until all types are received
                            deferredTypeCreationCommands.Add(new CreateTypeCommand {SourceIdentifier = registry.SourceIdentifier, Data = s_CommandObjectReader.ReadObject(reader)});
                        }
                            break;

                        case CommandType.CreateEntityGroup:
                        {
                            var group = Accept(new ObjectContainer(s_CommandObjectReader.ReadObject(reader)), registry, (r, id, name) => r.CreateEntityGroup(id, name));
                            entityGroups.Add(group);
                        }
                            break;

                        case CommandType.CreateEntity:
                        {
                            // Defer entity creation until all types are initialized
                            deferredEntityCreationCommands.Add(new CreateEntityCommand {SourceIdentifier = registry.SourceIdentifier, Data = s_CommandObjectReader.ReadObject(reader)});
                        }
                            break;

                        case CommandType.CreatePrefabInstance:
                        {
                            var instance = Accept(new ObjectContainer(s_CommandObjectReader.ReadObject(reader)), registry, (r, id, name) => r.CreatePrefabInstance(id, name));
                            prefabInstances.Add(instance);
                        }
                            break;

                        case CommandType.CreateScript:
                        {
                            // Script objects have been deprecated
                            // Defer migration until all scripts are loaded
                            IList<IDictionary<string, object>> legacyScriptsForSourceIdentifier;
                            if (!legacyScripts.TryGetValue(registry.SourceIdentifier, out legacyScriptsForSourceIdentifier))
                            {
                                legacyScriptsForSourceIdentifier = new List<IDictionary<string, object>>();
                                legacyScripts.Add(registry.SourceIdentifier, legacyScriptsForSourceIdentifier);
                            }

                            legacyScriptsForSourceIdentifier.Add(s_CommandObjectReader.ReadObject(reader));
                        }
                            break;

                        case CommandType.Unregister:
                        {
                            var bytes = new byte[16];
                            reader.Read(bytes, 0, 16);
                            var id = new TinyId(new Guid(bytes));
                            registry.Unregister(id);
                        }
                            break;

                        case CommandType.PushSourceIdentifierScope:
                        {
                            var identifier = reader.ReadString();
                            sourceIdentifierStack.Push(registry.SourceIdentifierScope(identifier));
                        }
                            break;

                        case CommandType.PopSourceIdentifierScope:
                        {
                            // @TODO error handling
                            // We are in full control of the stack so this is not an issue at the moment
                            sourceIdentifierStack.Pop().Dispose();
                        }
                            break;

                        default:
                            Debug.LogWarning($"Unhandled command type '{command}'");
                            break;
                    }

                    input.Position = end;
                }

                while (sourceIdentifierStack.Count > 0)
                {
                    sourceIdentifierStack.Pop().Dispose();
                }

                // Create types
                var types = new Dictionary<TinyType, IDictionary<string, object>>();
                foreach (var create in deferredTypeCreationCommands)
                {
                    using (registry.SourceIdentifierScope(create.SourceIdentifier))
                    {
                        var type = AcceptType(new ObjectContainer(create.Data), registry);
                        types.Add(type, create.Data);
                    }
                }

                // Initialize type default values
                // Iterate in DepthFirst order to ensure types are created properly
                foreach (var typeReference in TinyType.Iterator.DepthFirst(types.Keys))
                {
                    var type = typeReference.Dereference(registry);

                    if (!types.ContainsKey(type))
                    {
                        continue;
                    }

                    var dictionary = types[type];

                    object defaultValueObject;
                    if (!dictionary.TryGetValue("DefaultValue", out defaultValueObject))
                    {
                        continue;
                    }

                    var defaultValue = type.DefaultValue as TinyObject;
                    PropertyContainer.Transfer(new ObjectContainer(defaultValueObject as IDictionary<string, object>), defaultValue);
                }

                // Create entities
                foreach (var create in deferredEntityCreationCommands)
                {
                    using (registry.SourceIdentifierScope(create.SourceIdentifier))
                    {
                        entities.Add(AcceptEntity(new ObjectContainer(create.Data), registry));
                    }
                }

                // Setup prefab instance acceleration structure
                foreach (var group in entityGroups)
                {
                    foreach (var prefabInstance in group.PrefabInstances)
                    {
                        var instance = prefabInstance.Dereference(registry);
                        if (null == instance)
                        {
                            continue;
                        }
                        instance.EntityGroup = group.Ref;
                    }

                    foreach (var entityRef in group.Entities)
                    {
                        var entity = entityRef.Dereference(registry);
                        
                        if (null != entity && null == entity.EntityGroup)
                        {
                            entity.EntityGroup = group;
                        }
                    }
                }

                var prefabManager = registry?.Context?.GetManager<IPrefabManager>();
                foreach (var entity in entities.Where(e => e.HasEntityInstanceComponent()))
                {
                    var prefab = entity.Instance.Source.Dereference(registry);
                    prefabManager?.ApplyPrefabAttributesToInstance(prefab, entity);
                }

                ApplyPrefabModifications(registry, prefabInstances);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                ListPool<TinyEntityGroup>.Release(entityGroups);
                ListPool<TinyPrefabInstance>.Release(prefabInstances);
                ListPool<TinyEntity>.Release(entities);
            }
        }

        private static void ApplyPrefabModifications(IRegistry registry, IEnumerable<TinyPrefabInstance> instances)
        {
            // @TODO Resolve dependencies between prefab instances
            var prefabManager = registry?.Context?.GetManager<PrefabManager>();

            if (null == prefabManager)
            {
                return;
            }
            
            foreach (var instance in instances)
            {
                var references = instance.Entities;

                var entities = new List<TinyEntity>();
                var remap = new Dictionary<TinyEntity.Reference, TinyEntity.Reference>();

                foreach (var @ref in references)
                {
                    var entity = @ref.Dereference(registry);
                    
                    if (entity?.Instance == null)
                    {
                        // Debug.LogWarning($"Failed to apply prefab to instance for Entity=[{@ref.Name}]");
                        continue;
                    }
                    
                    var prefab = entity.Instance.Source.Dereference(registry);
                    
                    if (null == prefab)
                    {
                        // Debug.LogWarning($"Failed to apply prefab to instance for Entity=[{entity.Name}]");
                        continue;
                    }
                    
                    remap.Add(prefab.Ref, entity.Ref);
                    
                    prefabManager.ApplyPrefabToInstance(prefab, entity);

                    entities.Add(entity);
                    
                    entity.Instance.PrefabInstance = instance.Ref;
                }
                
                PrefabManager.RemapEntities(entities, remap);

                foreach (var @ref in references)
                {
                    var entity = @ref.Dereference(registry);
                    
                    // Keep root parents in sync
                    if (null != entity && entity.Parent().Equals(TinyEntity.Reference.None))
                    {
                        entity.SetParent(instance.Parent);
                    }
                }
            }
        }

        private static T Accept<T>(IPropertyContainer container, IRegistry registry, Func<IRegistry, TinyId, string, T> create, Func<IPropertyContainer, IRegistry, IPropertyContainer> migration = null)
            where T : class, IPropertyContainer
        {
            var id = container.GetValue<TinyId>(TinyRegistryObjectBase.IdProperty.Name);
            var name = container.GetValue<string>("Name");
            var obj = create(registry, id, name);

            if (null != migration)
            {
                container = migration.Invoke(container, registry);
                PropertyContainer.Transfer(container, obj);
            }
            else
            {
                PropertyContainer.Transfer(container, obj);
            }

            return obj;
        }

        private static TinyType AcceptType(IPropertyContainer container, IRegistry registry)
        {
            var id = container.GetValue<TinyId>(TinyRegistryObjectBase.IdProperty.Name);
            var name = container.GetValue<string>("Name");
            var typeCode = container.GetValue<TinyTypeCode>(TinyType.TypeCodeProperty.Name);
            var type = registry.CreateType(id, name, typeCode);
            PropertyContainer.Transfer(container, type);
            TinyUpdater.UpdateType(type);
            return type;
        }
        
        private static TinyEntity AcceptEntity(ObjectContainer container, IRegistry registry)
        {
            var id = container.GetValue<TinyId>(TinyRegistryObjectBase.IdProperty.Name);
            var name = container.GetValue<string>("Name");
            var entity = registry.CreateEntity(id, name);

            var migration = new MigrationContainer(container);

            // Hide the `Components` field since we want to do some specialized work
            if (migration.HasProperty("Components"))
            {
                migration.Remove("Components");
            }
            
            if (migration.HasProperty(TinyEntity.InstanceProperty.Name))
            {
                entity.Instance = new TinyEntityInstance(entity);
            }

            // Transfer everything except the components
            PropertyContainer.Transfer(migration, entity);

            // Manually unpack and deserialize the components
            // @TODO Provide a more elegant API for accessing arrays

            if (container.PropertyBag.FindProperty("Components") is IListClassProperty<ObjectContainer, ObjectContainer> components)
            {
                for (var i = 0; i < components.Count(container); i++)
                {
                    var componentContainer = components.GetAt(container, i);

                    TinyUpdater.UpdateObjectType(componentContainer);
                    TinyUpdater.UpdateEntityComponent(entity, componentContainer);
                }
            }
            
            return entity;
        }
    }
}

