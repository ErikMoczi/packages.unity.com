using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using Unity.Properties.Serialization;
using Unity.Tiny.Serialization;
using Unity.Tiny.Runtime.EditorExtensions;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unity.Tiny
{
    internal class WorldStateReader
    {
        private class Context
        {
            public readonly Dictionary<RuntimeEntity, IPropertyContainer> EntityContainerMap = new Dictionary<RuntimeEntity, IPropertyContainer>();
            public readonly Dictionary<RuntimeEntity, TinyEntity> EntityMap = new Dictionary<RuntimeEntity, TinyEntity>();
            public readonly Dictionary<string, TinyType> TypeNameMap = new Dictionary<string, TinyType>();

            public WorldState WorldState { get; set; }
            public ObjectContainer WorldContainer { get; set; }
            public PropertyList<ObjectContainer, ObjectContainer> Entities { get; set; }
        }

        private static Context s_Context;

        private struct RuntimeEntity : IEquatable<RuntimeEntity>
        {
            public int Index { get; internal set; }
            public int Version { get; internal set; }
            public string Name => $"Entity {Index} (v{Version})";

            public override bool Equals(object obj) => obj is RuntimeEntity o && Equals(o);
            public bool Equals(RuntimeEntity other) => other.Index == Index && other.Version == Version;
            public override int GetHashCode() => (Index << 15) + Version;

            public static ObjectContainer ComponentsContainer(IPropertyContainer entityContainer)
            {
                return entityContainer.GetValue<ObjectContainer>("components");
            }

            public static IEnumerable<IValueProperty> ComponentsProperties(ObjectContainer componentsContainer)
            {
                return componentsContainer.PropertyBag.Properties.Select(p => p as IValueProperty).NotNull();
            }

            public static ObjectContainer GetComponentContainer(IPropertyContainer entityContainer, string name)
            {
                var componentsContainer = ComponentsContainer(entityContainer);
                var componentsProperties = ComponentsProperties(componentsContainer);
                var componentProperty = componentsProperties.FirstOrDefault(p => p.Name == name);
                return componentProperty?.GetObjectValue(componentsContainer) as ObjectContainer ?? null;
            }
        }

        [TinyInitializeOnLoad]
        private static void Initialize()
        {
            TypeConversion.Register<IPropertyContainer, RuntimeEntity>(container =>
                new RuntimeEntity { Index = container.GetValue<int>("index"), Version = container.GetValue<int>("version") });
            TypeConversion.Register<RuntimeEntity, TinyEntity.Reference>(runtimeEntity =>
                s_Context.EntityMap.TryGetValue(runtimeEntity, out var entity) ? entity.Ref : TinyEntity.Reference.None);
        }

        public static bool Deserialize(WorldState worldState)
        {
            // Deserialize json world state into properties
            var worldContainer = JsonSerializer.Deserialize(worldState.Data);
            if (worldContainer.PropertyBag.PropertyCount == 0)
            {
                Debug.LogError("Failed to deserialize world state data.");
                return false;
            }
            var entities = worldContainer.GetList<ObjectContainer, ObjectContainer>("entities");

            // Allocate new context
            s_Context = new Context
            {
                WorldState = worldState,
                WorldContainer = worldContainer,
                Entities = entities
            };

            // Read world state into entity group
            CreateEntitiesAndComponents(s_Context);
            TransferEntitiesComponentData(s_Context);

            // Free up context
            s_Context = null;
            return true;
        }

        private static void CreateEntitiesAndComponents(Context context)
        {
            var registry = context.WorldState.EntityGroup.Registry;
            foreach (var entityContainer in context.Entities)
            {
                // Get runtime entity
                var runtimeEntity = TypeConversion.Convert<RuntimeEntity>(entityContainer);
                context.EntityContainerMap.Add(runtimeEntity, entityContainer);

                // Create entity
                var entity = context.WorldState.EntityGroup.Registry.CreateEntity(TinyId.New(), runtimeEntity.Name);
                context.EntityMap.Add(runtimeEntity, entity);

                // Add components
                var componentsContainer = RuntimeEntity.ComponentsContainer(entityContainer);
                foreach (var componentProperty in RuntimeEntity.ComponentsProperties(componentsContainer))
                {
                    var componentTypeName = componentProperty.Name;

                    // Get the component type
                    if (!context.TypeNameMap.TryGetValue(componentTypeName, out var type))
                    {
                        // Find TinyType corresponding to that fully qualified component name
                        type = registry.FindTypeByQualifiedName<TinyType>(componentTypeName);
                        if (type == null)
                        {
                            //Debug.LogWarning($"Could not find type '{componentTypeName}' in registry, skipping adding component.");
                            continue;
                        }
                        context.TypeNameMap.Add(componentTypeName, type);
                    }

                    // Add component
                    entity.AddComponent(type.Ref);
                }

                // Add entity to entity group
                context.WorldState.EntityGroup.AddEntityReference(entity.Ref);
            }
        }

        private static void TransferEntitiesComponentData(Context context)
        {
            var registry = context.WorldState.EntityGroup.Registry;
            foreach (var entityContainer in context.Entities)
            {
                // Get entity we created earlier
                var runtimeEntity = TypeConversion.Convert<RuntimeEntity>(entityContainer);
                if (!context.EntityMap.TryGetValue(runtimeEntity, out var entity))
                {
                    continue;
                }

                // Transfer components data
                var componentsContainer = RuntimeEntity.ComponentsContainer(entityContainer);
                foreach (var componentProperty in RuntimeEntity.ComponentsProperties(componentsContainer))
                {
                    // Find type
                    var componentTypeName = componentProperty.Name;
                    if (!context.TypeNameMap.TryGetValue(componentTypeName, out var type))
                    {
                        continue;
                    }

                    // Find component
                    var component = entity.GetComponent(type.Ref);
                    if (component == null)
                    {
                        continue;
                    }

                    // Transfer component data
                    var componentContainer = componentProperty.GetObjectValue(componentsContainer) as ObjectContainer;
                    component.Properties.Visit(new WorldStateTransferVisitor(registry, componentContainer));

                    // Additional transfer steps
                    if (type.Id == TypeRefs.ut.EntityInformation.Id)
                    {
                        entity.Name = componentContainer.GetValue<string>("name");
                    }
                    else if (type.Id == TypeRefs.EditorExtensions.EntityLayer.Id)
                    {
                        entity.Layer = componentContainer.GetValue<int>("layer");
                    }
                    else if (type.Id == TypeRefs.EditorExtensions.CameraCullingMask.Id)
                    {
                        var camera = entity.GetComponent<Runtime.Core2D.TinyCamera2D>();
                        if (camera.IsValid)
                        {
                            camera.layerMask = componentContainer.GetValue<int>("mask");
                        }
                    }
                    else if (type.Id == TypeRefs.Core2D.DisplayInfo.Id)
                    {
                        context.WorldState.FrameWidth = componentContainer.GetValue<int>("frameWidth");
                        context.WorldState.FrameHeight = componentContainer.GetValue<int>("frameHeight");
                    }
                }
            }
        }

        interface IWorldStateCustomTransfer<TValue>
        {
            void TransferPropertyValue(IPropertyContainer container, VisitContext<TValue> context, object value);
        }

        private class WorldStateTransferVisitor :
            IPropertyVisitor,
            IWorldStateCustomTransfer<TinyEntity.Reference>,
            IWorldStateCustomTransfer<TinyEnum.Reference>,
            IWorldStateCustomTransfer<Texture2D>,
            IWorldStateCustomTransfer<Sprite>,
            IWorldStateCustomTransfer<Tile>,
            IWorldStateCustomTransfer<AudioClip>,
            IWorldStateCustomTransfer<Font>,
            IWorldStateCustomTransfer<AnimationClip>,
            IWorldStateCustomTransfer<int>
        {
            private IRegistry Registry { get; set; }

            private class SrcVisitContext
            {
                private readonly Stack<IPropertyContainer> Containers = new Stack<IPropertyContainer>();
                public IProperty Property { get; set; }

                public IPropertyContainer Peek()
                {
                    return Containers.Peek();
                }

                public void Push(IPropertyContainer container)
                {
                    Containers.Push(container);
                }

                public IPropertyContainer Pop()
                {
                    return Containers.Pop();
                }

                public bool FindProperty(string name)
                {
                    var container = Peek();
                    if (container == null)
                    {
                        throw new NullReferenceException("PropertyContainer is null");
                    }
                    Property = container.PropertyBag.FindProperty(name);
                    return Property != null;
                }

                public object GetObjectValue()
                {
                    var container = Peek();
                    if (container == null)
                    {
                        throw new NullReferenceException("PropertyContainer is null");
                    }
                    if (Property is IValueProperty property)
                    {
                        return property.GetObjectValue(container);
                    }
                    throw new NullReferenceException("Property is not a value");
                }

                public object GetObjectAt(int index)
                {
                    var container = Peek();
                    if (container == null)
                    {
                        throw new NullReferenceException("PropertyContainer is null");
                    }
                    if (Property is IListClassProperty list)
                    {
                        return list.GetObjectAt(container, index);
                    }
                    else if (Property is IListStructProperty)
                    {
                        throw new NotImplementedException();
                    }
                    throw new NullReferenceException("Property is not a list");
                }

                public int GetObjectCount()
                {
                    var container = Peek();
                    if (container == null)
                    {
                        throw new NullReferenceException("PropertyContainer is null");
                    }
                    if (Property is IListClassProperty list)
                    {
                        return list.Count(container);
                    }
                    else if (Property is IListStructProperty)
                    {
                        throw new NotImplementedException();
                    }
                    throw new NullReferenceException("Property is not a list");
                }
            }

            private readonly SrcVisitContext SrcContext = new SrcVisitContext();

            public WorldStateTransferVisitor(IRegistry registry, IPropertyContainer container)
            {
                Registry = registry;
                SrcContext.Push(container);
            }

            public bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
                where TContainer : class, IPropertyContainer
            {
                return false;
            }

            public bool ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
                where TContainer : struct, IPropertyContainer
            {
                return false;
            }

            public bool CustomVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
                where TContainer : class, IPropertyContainer
            {
                return CustomVisitTyped(container, context);
            }

            public bool CustomVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
                where TContainer : struct, IPropertyContainer
            {
                return CustomVisitTyped(container, context);
            }

            public void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
                where TContainer : class, IPropertyContainer
            {
                TransferPropertyValue(container, context);
            }

            public void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
                where TContainer : struct, IPropertyContainer
            {
                TransferPropertyValue(container, context);
            }

            public bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
                where TContainer : class, IPropertyContainer where TValue : IPropertyContainer
            {
                return PushContainer(container, context);
            }

            public bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
                where TContainer : struct, IPropertyContainer where TValue : IPropertyContainer
            {
                return PushContainer(container, context);
            }

            public void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
                where TContainer : class, IPropertyContainer where TValue : IPropertyContainer
            {
                PopContainer(container, context);
            }

            public void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
                where TContainer : struct, IPropertyContainer where TValue : IPropertyContainer
            {
                PopContainer(container, context);
            }

            public bool BeginCollection<TContainer, TValue>(TContainer container, VisitContext<IList<TValue>> context)
                where TContainer : class, IPropertyContainer
            {
                return BeginList(container, context);
            }

            public bool BeginCollection<TContainer, TValue>(ref TContainer container, VisitContext<IList<TValue>> context)
                where TContainer : struct, IPropertyContainer
            {
                return BeginList(container, context);
            }

            public void EndCollection<TContainer, TValue>(TContainer container, VisitContext<IList<TValue>> context)
                where TContainer : class, IPropertyContainer
            {
            }

            public void EndCollection<TContainer, TValue>(ref TContainer container, VisitContext<IList<TValue>> context)
                where TContainer : struct, IPropertyContainer
            {
            }

            private bool CustomVisitTyped<TValue>(IPropertyContainer container, VisitContext<TValue> context)
            {
                if (this is IWorldStateCustomTransfer<TValue> typed)
                {
                    object value = null;
                    if (context.Index < 0)
                    {
                        if (SrcContext.Peek() == null || !SrcContext.FindProperty(context.Property.Name))
                        {
                            return false;
                        }
                        value = SrcContext.GetObjectValue();
                    }
                    else if (container is TinyList list)
                    {
                        if (SrcContext.Peek() == null || !SrcContext.FindProperty(list.Name))
                        {
                            return false;
                        }
                        value = SrcContext.GetObjectAt(context.Index);
                    }
                    typed.TransferPropertyValue(container, context, value);
                    return true;
                }
                return false;
            }

            private bool BeginList<TValue>(IPropertyContainer container, VisitContext<TValue> context)
            {
                if (container is TinyList list)
                {
                    if (SrcContext.Peek() == null || !SrcContext.FindProperty(list.Name))
                    {
                        return false;
                    }
                    if (context.Property is IListClassProperty listClassProperty)
                    {
                        var count = SrcContext.GetObjectCount();
                        for (var i = 0; i < count; i++)
                        {
                            listClassProperty.AddNew(container);
                        }
                    }
                    else if (context.Property is IListStructProperty)
                    {
                        throw new NotImplementedException();
                    }
                    return true;
                }
                return false;
            }

            private bool PushContainer<TValue>(IPropertyContainer container, VisitContext<TValue> context)
            {
                if (typeof(TValue) == typeof(TinyObject.PropertiesContainer))
                {
                    return true;
                }

                if (typeof(TValue) == typeof(TinyList))
                {
                    return true;
                }

                if (typeof(TValue) == typeof(TinyType.Reference))
                {
                    return false;
                }

                Type type = null;
                IPropertyContainer value = null;

                if (context.Index < 0)
                {
                    if (SrcContext.Peek() == null || !SrcContext.FindProperty(context.Property.Name))
                    {
                        SrcContext.Push(null);
                        return false;
                    }
                    type = (SrcContext.Property as IValueProperty)?.ValueType;
                    value = SrcContext.GetObjectValue() as IPropertyContainer;
                }
                else if (container is TinyList list)
                {
                    if (SrcContext.Peek() == null || !SrcContext.FindProperty(list.Name))
                    {
                        SrcContext.Push(null);
                        return false;
                    }
                    type = (SrcContext.Property as IListProperty)?.ItemType;
                    if (SrcContext.Property is IListClassProperty listClassProperty)
                    {
                        value = SrcContext.GetObjectAt(context.Index) as IPropertyContainer;
                    }
                    else if (SrcContext.Property is IListStructProperty)
                    {
                        throw new NotImplementedException();
                    }
                }

                SrcContext.Push(value);
                return typeof(IPropertyContainer).IsAssignableFrom(type);
            }

            private void PopContainer<TValue>(IPropertyContainer container, VisitContext<TValue> context)
            {
                if (typeof(TValue) == typeof(TinyObject.PropertiesContainer))
                {
                    return;
                }

                if (typeof(TValue) == typeof(TinyType.Reference))
                {
                    return;
                }

                if (typeof(TValue) == typeof(TinyList))
                {
                    return;
                }

                if (context.Index < 0)
                {
                    if (SrcContext.Peek() == null || !SrcContext.FindProperty(context.Property.Name))
                    {
                        SrcContext.Pop();
                        return;
                    }
                }
                else if (container is TinyList list)
                {
                    SrcContext.Pop();
                    return;
                }

                SrcContext.Pop();
                TransferPropertyValue(container, context);
            }

            private void TransferPropertyValue<TValue>(IPropertyContainer container, VisitContext<TValue> context)
            {
                if (context.Index < 0)
                {
                    if (SrcContext.Peek() == null || !SrcContext.FindProperty(context.Property.Name))
                    {
                        //Debug.LogWarning($"Property '{context.Property.Name}' not found, skipping assignment.");
                        return;
                    }
                    SetPropertyValue(container, context, SrcContext.GetObjectValue());
                }
                else if (container is TinyList list)
                {
                    if (SrcContext.Peek() == null || !SrcContext.FindProperty(list.Name))
                    {
                        //Debug.LogWarning($"Property '{context.Property.Name}' not found, skipping assignment.");
                        return;
                    }
                    if (SrcContext.Property is IListClassProperty listClassProperty)
                    {
                        SetPropertyValue(container, context, SrcContext.GetObjectAt(context.Index));
                    }
                    else if (SrcContext.Property is IListStructProperty)
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            public void TransferPropertyValue(IPropertyContainer container, VisitContext<TinyEntity.Reference> context, object value)
            {
                SetPropertyValue(container, context, TypeConversion.Convert<RuntimeEntity>(value));
            }

            public void TransferPropertyValue(IPropertyContainer container, VisitContext<TinyEnum.Reference> context, object value)
            {
                SetPropertyValue(container, context, new TinyEnum.Reference(context.Value.Type.Dereference(Registry), Convert.ToInt32(value)));
            }

            public void TransferPropertyValue(IPropertyContainer container, VisitContext<Texture2D> context, object value)
            {
                TransferRuntimeEntityToUnityEngineObject(container, context, value);
            }

            public void TransferPropertyValue(IPropertyContainer container, VisitContext<Sprite> context, object value)
            {
                TransferRuntimeEntityToUnityEngineObject(container, context, value);
            }

            public void TransferPropertyValue(IPropertyContainer container, VisitContext<Tile> context, object value)
            {
                TransferRuntimeEntityToUnityEngineObject(container, context, value);
            }

            public void TransferPropertyValue(IPropertyContainer container, VisitContext<AudioClip> context, object value)
            {
                TransferRuntimeEntityToUnityEngineObject(container, context, value);
            }

            public void TransferPropertyValue(IPropertyContainer container, VisitContext<Font> context, object value)
            {
                TransferRuntimeEntityToUnityEngineObject(container, context, value);
            }

            public void TransferPropertyValue(IPropertyContainer container, VisitContext<AnimationClip> context, object value)
            {
                TransferRuntimeEntityToUnityEngineObject(container, context, value);
            }

            public void TransferPropertyValue(IPropertyContainer container, VisitContext<int> context, object value)
            {
                // HACK: Unity uses a unique identifier for sorting layers to support renaming/reordering.
                // At import time, we need to convert the sorting value to an actual sorting layer ID.
                var parentObject = (container as TinyObject.PropertiesContainer)?.ParentObject ?? null;
                if (parentObject != null && parentObject.Type.Equals(TypeRefs.Core2D.LayerSorting) && context.Property.Name == "layer")
                {
                    int layerValue = Convert.ToInt32(value);
                    foreach (var sortingLayer in SortingLayer.layers)
                    {
                        if (SortingLayer.GetLayerValueFromID(sortingLayer.id) == layerValue)
                        {
                            SetPropertyValue(container, context, sortingLayer.id);
                            return;
                        }
                    }
                    throw new Exception("Could not find sorting layer id");
                }
                SetPropertyValue(container, context, value);
            }

            private void SetPropertyValue<TValue>(IPropertyContainer container, VisitContext<TValue> context, object value)
            {
                var property = context.Property;
                if (property is IClassProperty)
                {
                    (property as IValueClassProperty)?.SetObjectValue(container, value);
                    (property as IListClassProperty)?.SetObjectAt(container, context.Index, value);
                }
                else if (property is IStructProperty)
                {
                    (property as IValueStructProperty)?.SetObjectValue(ref container, value);
                    (property as IListStructProperty)?.SetObjectAt(ref container, context.Index, value);
                }
            }

            private void TransferRuntimeEntityToUnityEngineObject<TValue>(IPropertyContainer container, VisitContext<TValue> context, object value)
                where TValue : UnityEngine.Object
            {
                var runtimeEntity = TypeConversion.Convert<RuntimeEntity>(value);
                var @object = RuntimeEntityToUnityEngineObject<TValue>(runtimeEntity);
                SetPropertyValue(container, context, @object);
            }

            private UnityEngine.Object RuntimeEntityToUnityEngineObject<TValue>(RuntimeEntity runtimeEntity)
                where TValue : UnityEngine.Object
            {
                if (!s_Context.EntityContainerMap.TryGetValue(runtimeEntity, out var entityContainer))
                {
                    return null;
                }

                var assetReferenceType = TinyAssetReference.GetType<TValue>().Dereference(Registry);
                var componentContainer = RuntimeEntity.GetComponentContainer(entityContainer, $"{TinyScriptUtility.GetJsTypeName(assetReferenceType)}");
                if (componentContainer == null)
                {
                    return null;
                }

                var guid = componentContainer.GetValue<string>("guid");
                var fileId = componentContainer.GetValue<long>("fileId");
                var type = componentContainer.GetValue<int>("type");

                return UnityObjectSerializer.FromObjectHandle(new UnityObjectHandle { Guid = guid, FileId = fileId, Type = type });
            }
        }
    }
}
