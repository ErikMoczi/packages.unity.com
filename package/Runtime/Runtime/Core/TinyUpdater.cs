using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal interface ITypeUpdater
    {
        void UpdateObjectType(MigrationContainer container);
        void UpdateReference(MigrationContainer container);
    }

    internal interface IEntityComponentUpdater
    {
        void UpdateEntityComponent(TinyEntity entity, MigrationContainer container);
    }

    /// <summary>
    /// Helper class to manage data migration and eventually versioning
    /// </summary>
    [TinyInitializeOnLoad]
    internal static class TinyUpdater
    {
        private static readonly Dictionary<TinyId, ITypeUpdater> s_TypeUpdaters =
            new Dictionary<TinyId, ITypeUpdater>();

        private static readonly Dictionary<TinyType.Reference, TinyType.Reference> s_IdUpdaters =
            new Dictionary<TinyType.Reference, TinyType.Reference>();

        private static TypeRemapVisitor s_RemapVisitor;

        private static readonly Dictionary<TinyId, IEntityComponentUpdater> s_EntityComponentUpdaters =
            new Dictionary<TinyId, IEntityComponentUpdater>();

        static TinyUpdater()
        {
            // @TODO Move registration to an external class
            s_RemapVisitor = new TypeRemapVisitor(s_IdUpdaters);
        }

        public static void RegisterTypeIdChange(string srcTypeFullName, string dstTypeFullName)
        {
            var dstName = dstTypeFullName.Substring(dstTypeFullName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            var dstType = new TinyType.Reference(TinyId.Generate(dstTypeFullName), dstName);

            var srcName = srcTypeFullName.Substring(srcTypeFullName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            var srcType = new TinyType.Reference(TinyId.Generate(srcTypeFullName), srcName);

            s_IdUpdaters.Add(srcType, dstType);
            Register(TinyId.Generate(srcTypeFullName), new TypeIdChange(dstType));
        }

        public static void RegisterEntityComponentMigration(TinyId componentId, IEntityComponentUpdater updater)
        {
            s_EntityComponentUpdaters.Add(componentId, updater);
        }

        public static void Register(TinyId id, ITypeUpdater updater)
        {
            Assert.IsFalse(s_TypeUpdaters.ContainsKey(id));
            s_TypeUpdaters.Add(id, updater);
        }

        public static void UpdateProject(TinyProject project)
        {
            project.SerializedVersion = TinyProject.CurrentSerializedVersion;
        }

        internal static void UpdateEntityComponent(TinyEntity entity, IPropertyContainer componentObject)
        {
            var migration = new MigrationContainer(componentObject);
            var componentType = migration.GetValue<TinyType.Reference>("Type");
            var componentId = componentType.Id;

            if (s_EntityComponentUpdaters.TryGetValue(componentId, out var updater))
            {
                updater.UpdateEntityComponent(entity, migration);
            }
            else
            {
                var component = entity.AddComponent(componentType);
                PropertyContainer.Transfer(componentObject, component);
            }
        }

        public static void UpdateType(TinyType type)
        {
            type.Visit(s_RemapVisitor);
        }

        public static void UpdateObjectType(IPropertyContainer @object)
        {
            var visitor = new TinyObjectMigrationVisitor();
            @object.Visit(visitor);

            do
            {
                var type = @object.GetValue<IPropertyContainer>("Type");
                var id = type.GetValue<TinyId>("Id");

                var updater = GetTypeUpdater(id);
                if (null == updater)
                {
                    return;
                }

                var migration = new MigrationContainer(@object);

                updater.UpdateObjectType(migration);
            } while (true);
        }

        private class TinyObjectMigrationVisitor : PropertyVisitor
        {
            public override bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            {
                var value = context.Value;

                // @NOTE
                // We have an issue here that we have no reliable way to determine if this container
                // is a `TinyObject`, `TinyList` or a custom object

                if (value.PropertyBag.FindProperty("Type") != null)
                {
                    // Assume if we have a `Type` property we are dealing with a `TinyObject` or `TinyList`

                    // If we have an `Items` property assume `TinyList`
                    if (value.PropertyBag.FindProperty("Items") != null)
                    {
                        // No special handling
                        return base.BeginContainer(container, context);
                    }

                    // If we have a `Properties` property assume `TinyObject`
                    // If we have no other properties assume `TinyObject`
                    if (value.PropertyBag.FindProperty("Properties") != null ||
                        value.PropertyBag.PropertyCount == 1)
                    {
                        // Skip the remaining execution and handle the type manually
                        UpdateObjectType(value);
                        return false;
                    }
                }

                // Handle all other types normally
                return base.BeginContainer(container, context);
            }

            protected override void Visit<TValue>(TValue value)
            {
                // Ignore leaf nodes
            }
        }

        private static ITypeUpdater GetTypeUpdater(TinyId id)
        {
            ITypeUpdater updater;
            return !s_TypeUpdaters.TryGetValue(id, out updater) ? null : updater;
        }
    }

    /// <summary>
    /// Simple class to handle migrating a type id
    /// </summary>
    internal class TypeIdChange : ITypeUpdater
    {
        private readonly TinyType.Reference m_Type;

        public TypeIdChange(TinyType.Reference type)
        {
            m_Type = type;
        }

        public void UpdateReference(MigrationContainer container)
        {
            var type = container.GetContainer("Type");
            type.SetValue("Id", m_Type.Id);
            type.SetValue("Name", m_Type.Name);
        }

        public void UpdateObjectType(MigrationContainer container)
        {
            UpdateReference(container);
            // no migration to perform
        }
    }
    
    internal class TypeRemapVisitor :
        PropertyVisitor,
        ICustomVisit<TinyType.Reference>
    {
        private IPropertyContainer m_Container;

        private readonly Dictionary<TinyType.Reference, TinyType.Reference> m_Remap =
            new Dictionary<TinyType.Reference, TinyType.Reference>();

        public TypeRemapVisitor(Dictionary<TinyType.Reference, TinyType.Reference> remap)
        {
            m_Remap = remap;
        }

        protected override void VisitSetup<TContainer, TValue>(ref TContainer container,
            ref VisitContext<TValue> context)
        {
            m_Container = container;
            base.VisitSetup(ref container, ref context);
        }

        protected override void Visit<TValue>(TValue value)
        {

        }

        public void CustomVisit(TinyType.Reference source)
        {
            while (m_Remap.TryGetValue(source, out var target))
            {
                (Property as IValueClassProperty)?.SetObjectValue(m_Container, target);
                source = target;
            }
        }
    }
}
