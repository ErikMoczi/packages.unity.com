using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;
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

            // Move into Math module
            RegisterTypeIdChange("UTiny.Core.Vector2f", "UTiny.Math.Vector2");
            RegisterTypeIdChange("UTiny.Core.Vector3f", "UTiny.Math.Vector3");
            RegisterTypeIdChange("UTiny.Core.Vector4f", "UTiny.Math.Vector4");
            RegisterTypeIdChange("UTiny.Core.Matrix3x3f", "UTiny.Math.Matrix3x3");
            RegisterTypeIdChange("UTiny.Core.Matrix4x4f", "UTiny.Math.Matrix4x4");
            RegisterTypeIdChange("UTiny.Core.Quaternionf", "UTiny.Math.Quaternion");
            RegisterTypeIdChange("UTiny.Core.Rectf", "UTiny.Math.Rect");
            RegisterTypeIdChange("UTiny.Core.RectInt", "UTiny.Math.RectInt");

            // moves into Core2D module
            RegisterTypeIdChange("UTiny.Core.DisplayOrientation", "UTiny.Core2D.DisplayOrientation");
            RegisterTypeIdChange("UTiny.Core.DisplayInfo", "UTiny.Core2D.DisplayInfo");
            RegisterTypeIdChange("UTiny.Core.MouseState", "UTiny.Core2D.MouseState");
            RegisterTypeIdChange("UTiny.Core.Camera2D", "UTiny.Core2D.Camera2D");
            RegisterTypeIdChange("UTiny.Core.Image2D", "UTiny.Core2D.Image2D");
            RegisterTypeIdChange("UTiny.Core.Sprite2D", "UTiny.Core2D.Sprite2D");
            RegisterTypeIdChange("UTiny.Core.Sprite2DRenderer", "UTiny.Core2D.Sprite2DRenderer");
            RegisterTypeIdChange("UTiny.Core.Transform", "UTiny.Core2D.Transform");

            // Type renames
            RegisterTypeIdChange("UTiny.Core2D.Sprite2DRendererTiling", "UTiny.Core2D.Sprite2DRendererOptions");

            // Modularization
            RegisterTypeIdChange("UTiny.Core2D.Color", "UTiny.Core2DTypes.Color");
            RegisterTypeIdChange("UTiny.Core2D.BlendOp", "UTiny.Core2DTypes.BlendOp");
            RegisterTypeIdChange("UTiny.Core2D.LoopMode", "UTiny.Core2DTypes.LoopMode");

            RegisterTypeIdChange("UTiny.Core2D.Image2D", "UTiny.Image2D.Image2D");
            RegisterTypeIdChange("UTiny.Core2D.ImageStatus", "UTiny.Image2D.ImageStatus");
            RegisterTypeIdChange("UTiny.Core2D.Image2DLoadFromFile", "UTiny.Image2D.Image2DLoadFromFile");
            RegisterTypeIdChange("UTiny.Core2D.Image2DAlphaMask", "UTiny.Image2D.Image2DAlphaMask");

            RegisterTypeIdChange("UTiny.Core2D.DrawMode", "UTiny.Sprite2D.DrawMode");
            RegisterTypeIdChange("UTiny.Core2D.Sprite2D", "UTiny.Sprite2D.Sprite2D");
            RegisterTypeIdChange("UTiny.Core2D.Sprite2DBorder", "UTiny.Sprite2D.Sprite2DBorder");
            RegisterTypeIdChange("UTiny.Core2D.Sprite2DRenderer", "UTiny.Sprite2D.Sprite2DRenderer");
            RegisterTypeIdChange("UTiny.Core2D.Sprite2DRendererOptions", "UTiny.Sprite2D.Sprite2DRendererOptions");
            RegisterTypeIdChange("UTiny.Core2D.Sprite2DSequence", "UTiny.Sprite2D.Sprite2DSequence");
            RegisterTypeIdChange("UTiny.Core2D.Sprite2DSequencePlayer", "UTiny.Sprite2D.Sprite2DSequencePlayer");
            RegisterTypeIdChange("UTiny.Core2D.SpriteAtlas", "UTiny.Sprite2D.SpriteAtlas");

            // Custom updates
            RegisterEntityComponentMigration(new TinyId("10b758a472c3be3e0885538510d60803"), new TransformSplitUpdater());
            RegisterEntityComponentMigration(CoreIds.Core2D.Camera2D, new Camera2DDefaultValueUpdater());
            RegisterEntityComponentMigration(CoreIds.Core2D.Sprite2DRenderer, new Sprite2DRendererUpdater());
            RegisterEntityComponentMigration(CoreIds.Core2D.Sprite2DSequencePlayer, new Sprite2DSequencePlayerUpdater());
            RegisterEntityComponentMigration(CoreIds.Core2D.Sprite2DRendererOptions, new Sprite2DRendererOptionsUpdater());
            RegisterEntityComponentMigration(CoreIds.Animation.AnimationClipPlayer, new AnimationClipPlayerUpdater());
            RegisterEntityComponentMigration(CoreIds.TextJS.TextRenderer, new Text2DRendererUpdater());
            RegisterEntityComponentMigration(TinyId.Generate("UTiny.Physics2D.RectCollider"), new RectColliderUpdater());

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
            var registry = project.Registry;
            if (project.SerializedVersion < 1)
            {
                foreach (var entity in project.Module.Dereference(registry).EntityGroups.Deref(registry).Entities())
                {
                    entity.Enabled = true;
                }
            }

            var module = project.Module.Dereference(registry);
            if (project.LastSerializedVersion < 5)
            {
                // With the module split,
                var initialModules = new[]
                {
                    "UTiny.Core2D",
                    "UTiny.EntityGroup",
                    "UTiny.Core2DTypes",
                    "UTiny.HTML",
                    "UTiny.Image2D",
                    "UTiny.ImageLoadingHTML",
                    "UTiny.Sprite2D",
                };

                foreach (var initialModule in initialModules)
                {
                    module.AddExplicitModuleDependency(registry.FindByName<TinyModule>(initialModule).Ref);
                }

                var dependencies = new HashSet<TinyModule>(module.EnumerateDependencies());

                foreach (var dependency in dependencies)
                {
                    foreach (var transitiveDependency in dependency.EnumerateDependencies())
                    {
                        module.AddExplicitModuleDependency(transitiveDependency.Ref);
                    }
                }
            }

            if (project.LastSerializedVersion < 6)
            {
                if (module.ContainsExplicitModuleDependency(new TinyModule.Reference(new TinyId(CoreGuids.TextJS.Id), "UTiny.TextJS")))
                {
                    module.AddExplicitModuleDependency(registry.FindByName<TinyModule>("UTiny.Text").Ref);
                    module.AddExplicitModuleDependency(registry.FindByName<TinyModule>("UTiny.TextHTML").Ref);
                }
            }

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
                component.Refresh(null, true);
                PropertyContainer.Transfer(componentObject, component);
                component.Refresh(null, true);
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

    internal class Camera2DDefaultValueUpdater : IEntityComponentUpdater
    {
        public void UpdateEntityComponent(TinyEntity entity, MigrationContainer migration)
        {
            const string halfVerticalSize = "halfVerticalSize";
            var componentType = migration.GetValue<TinyType.Reference>("Type");

            var project = entity.Registry.FindAllByType<TinyProject>().FirstOrDefault();
            if (null == project)
            {
                return;
            }

            var component = entity.AddComponent(componentType);
            component.Refresh(null, true);
            PropertyContainer.Transfer(migration, component);
            component.Refresh(null, true);

            if (project.LastSerializedVersion > 3)
            {
                return;
            }

            var isOverridden =
                (component.Properties.PropertyBag.FindProperty(halfVerticalSize) as ITinyValueProperty)?.IsOverridden(
                    component.Properties) ?? true;
            if (isOverridden)
            {
                return;
            }

            // Put the old vertical size
            component[halfVerticalSize] = 200.0f;
        }
    }

    internal class Sprite2DRendererOptionsUpdater : IEntityComponentUpdater
    {
        public void UpdateEntityComponent(TinyEntity entity, MigrationContainer migration)
        {
            var componentType = migration.GetValue<TinyType.Reference>("Type");
            MigrationContainer properties = null;
            if (migration.HasProperty("Properties"))
            {
                properties = migration.GetContainer("Properties");
            }

            if (properties?.HasProperty("drawMode") ?? false)
            {
                var drawMode = properties.GetContainer("drawMode");
                var drawModeType = drawMode.GetContainer("Type");
                drawModeType.SetValue("Id", CoreGuids.Core2D.DrawMode);
                var id = drawMode.GetValueOrDefault("Id", TinyId.Empty);
                var wasId = true;
                if (id == TinyId.Empty)
                {
                    id = new TinyId(drawMode.GetValueOrDefault("Id", string.Empty));
                    wasId = false;
                }

                var newId = id.ToString();
                if (id == TinyId.Generate("UTiny.Core2D.DrawMode.ContinuousTiling"))
                {
                    newId = TinyId.Generate("UTiny.Sprite2D.DrawMode.ContinuousTiling").ToString();
                }

                if (id == TinyId.Generate("UTiny.Core2D.DrawMode.AdaptiveTiling"))
                {
                    newId = TinyId.Generate("UTiny.Sprite2D.DrawMode.AdaptiveTiling").ToString();
                }

                if (id == TinyId.Generate("UTiny.Core2D.DrawMode.Stretch"))
                {
                    newId = TinyId.Generate("UTiny.Sprite2D.DrawMode.Stretch").ToString();
                }

                if (wasId)
                {
                    drawMode.SetValue("Id", new TinyId(newId));
                }
                else
                {
                    drawMode.SetValue("Id", newId);
                }
            }

            var component = entity.AddComponent(componentType);
            component.Refresh(null, true);
            PropertyContainer.Transfer(migration, component);
            component.Refresh(null, true);

            var project = entity.Registry.FindAllByType<TinyProject>().FirstOrDefault();
            if (null == project)
            {
                return;
            }

            if (project.LastSerializedVersion > 5)
            {
                return;
            }

            const string drawModeName = "drawMode";
            var isDrawModeOverriden = (component.Properties.PropertyBag.FindProperty(drawModeName) as ITinyValueProperty)?.IsOverridden(component.Properties) ?? true;
            if (!isDrawModeOverriden)
            {
                var oldDefault = new TinyEnum.Reference(entity.Registry.FindByName<TinyType>("DrawMode"), 0);
                if (properties?.HasProperty("drawMode") ?? false)
                {
                    var drawMode = properties.GetContainer("drawMode");
                    if (drawMode.GetValueOrDefault<int>("Value") != 2)
                    {
                        component[drawModeName] = oldDefault;
                    }
                }
                else
                {
                    component[drawModeName] = oldDefault;
                }
            }
        }
    }

    internal class Sprite2DRendererUpdater : IEntityComponentUpdater
    {
        public void UpdateEntityComponent(TinyEntity entity, MigrationContainer migration)
        {
            var componentType = migration.GetValue<TinyType.Reference>("Type");
            MigrationContainer properties = null;
            if (migration.HasProperty("Properties"))
            {
                properties = migration.GetContainer("Properties");
            }

            if (properties?.HasProperty("blending") ?? false)
            {
                var loop = properties.GetContainer("blending");
                var loopType = loop.GetContainer("Type");
                loopType.SetValue("Id", CoreGuids.Core2D.BlendOp);
                var id = loop.GetValueOrDefault("Id", TinyId.Empty);
                var wasId = true;
                if (id == TinyId.Empty)
                {
                    id = new TinyId(loop.GetValueOrDefault("Id", string.Empty));
                    wasId = false;
                }

                var newId = id.ToString();
                if (id == TinyId.Generate("UTiny.Core2D.BlendOp.Alpha"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.BlendOp.Alpha").ToString();
                }

                if (id == TinyId.Generate("UTiny.Core2D.BlendOp.Add"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.BlendOp.Add").ToString();
                }

                if (id == TinyId.Generate("UTiny.Core2D.BlendOp.Multiply"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.BlendOp.Multiply").ToString();
                }

                if (wasId)
                {
                    loop.SetValue("Id", new TinyId(newId));
                }
                else
                {
                    loop.SetValue("Id", newId);
                }
            }

            var component = entity.AddComponent(componentType);
            component.Refresh(null, true);
            PropertyContainer.Transfer(migration, component);
            component.Refresh(null, true);
        }
    }

    internal class Sprite2DSequencePlayerUpdater : IEntityComponentUpdater
    {
        public void UpdateEntityComponent(TinyEntity entity, MigrationContainer migration)
        {
            var componentType = migration.GetValue<TinyType.Reference>("Type");
            MigrationContainer properties = null;
            if (migration.HasProperty("Properties"))
            {
                properties = migration.GetContainer("Properties");
            }

            if (properties?.HasProperty("loop") ?? false)
            {
                var loop = properties.GetContainer("loop");
                var loopType = loop.GetContainer("Type");
                loopType.SetValue("Id", CoreGuids.Core2D.LoopMode);
                var id = loop.GetValueOrDefault("Id", TinyId.Empty);
                var wasId = true;
                if (id == TinyId.Empty)
                {
                    id = new TinyId(loop.GetValueOrDefault("Id", string.Empty));
                    wasId = false;
                }

                var newId = id.ToString();
                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.Loop"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.Loop").ToString();
                }

                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.Once"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.Once").ToString();
                }

                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.PingPong"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.PingPong").ToString();
                }

                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.PingPongOnce"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.PingPongOnce").ToString();
                }

                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.ClampForever"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.ClampForever").ToString();
                }

                if (wasId)
                {
                    loop.SetValue("Id", new TinyId(newId));
                }
                else
                {
                    loop.SetValue("Id", newId);
                }
            }

            var component = entity.AddComponent(componentType);
            component.Refresh(null, true);
            PropertyContainer.Transfer(migration, component);
            component.Refresh(null, true);
        }
    }

    internal class AnimationClipPlayerUpdater : IEntityComponentUpdater
    {
        public void UpdateEntityComponent(TinyEntity entity, MigrationContainer migration)
        {
            var componentType = migration.GetValue<TinyType.Reference>("Type");
            MigrationContainer properties = null;
            if (migration.HasProperty("Properties"))
            {
                properties = migration.GetContainer("Properties");
            }

            if (properties?.HasProperty("loopMode") ?? false)
            {
                var loop = properties.GetContainer("loopMode");
                var loopType = loop.GetContainer("Type");
                loopType.SetValue("Id", CoreGuids.Core2D.LoopMode);
                var id = loop.GetValueOrDefault("Id", TinyId.Empty);
                var wasId = true;
                if (id == TinyId.Empty)
                {
                    id = new TinyId(loop.GetValueOrDefault("Id", string.Empty));
                    wasId = false;
                }
                var newId = id.ToString();
                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.Loop"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.Loop").ToString();
                }
                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.Once"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.Once").ToString();
                }
                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.PingPong"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.PingPong").ToString();
                }
                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.PingPongOnce"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.PingPongOnce").ToString();
                }
                if (id == TinyId.Generate("UTiny.Core2D.LoopMode.ClampForever"))
                {
                    newId = TinyId.Generate("UTiny.Core2DTypes.LoopMode.ClampForever").ToString();
                }

                if (wasId)
                {
                    loop.SetValue("Id", new TinyId(newId));
                }
                else
                {
                    loop.SetValue("Id", newId);
                }
            }

            var component = entity.AddComponent(componentType);
            component.Refresh(null, true);
            PropertyContainer.Transfer(migration, component);
            component.Refresh(null, true);
        }
    }

    internal class TransformSplitUpdater : IEntityComponentUpdater
    {
        public static readonly TinyId TinyTransformId = new TinyId("10b758a472c3be3e0885538510d60803");

        public void UpdateEntityComponent(TinyEntity entity, MigrationContainer migration)
        {
            MigrationContainer properties = null;
            if (migration.HasProperty("Properties"))
            {
                properties = migration.GetContainer("Properties");
            }

            var node = entity.AddComponent(TypeRefs.Core2D.TransformNode);
            node.Refresh(null, true);

            if (properties?.HasProperty("parent") ?? false)
            {
                var container = properties.GetValue<ObjectContainer>("parent");
                var parent = new TinyEntity.Reference(new TinyId(container.GetValue<string>("Id")),
                    container.GetValue<string>("Name"));
                node["parent"] = parent;
            }

            node.Refresh(null, true);

            var position = entity.AddComponent(TypeRefs.Core2D.TransformLocalPosition);
            position.Refresh(null, true);
            if (properties?.HasProperty("localPosition") ?? false)
            {
                var container = properties.GetValue<ObjectContainer>("localPosition");
                var obj = position["position"] as TinyObject;
                var subProperties = new MigrationContainer(container.GetValue<ObjectContainer>("Properties"));
                if (subProperties.HasProperty("x"))
                {
                    obj["x"] = subProperties.GetValue<float>("x");
                }

                if (subProperties.HasProperty("y"))
                {
                    obj["y"] = subProperties.GetValue<float>("y");
                }

                if (subProperties.HasProperty("z"))
                {
                    obj["z"] = subProperties.GetValue<float>("z");
                }
            }

            position.Refresh(null, true);

            var rotation = entity.AddComponent(TypeRefs.Core2D.TransformLocalRotation);
            rotation.Refresh(null, true);
            if (properties?.HasProperty("localRotation") ?? false)
            {
                var container = properties.GetValue<ObjectContainer>("localRotation");
                var obj = rotation["rotation"] as TinyObject;
                var subProperties = new MigrationContainer(container.GetValue<ObjectContainer>("Properties"));
                if (subProperties.HasProperty("x"))
                {
                    obj["x"] = subProperties.GetValue<float>("x");
                }

                if (subProperties.HasProperty("y"))
                {
                    obj["y"] = subProperties.GetValue<float>("y");
                }

                if (subProperties.HasProperty("z"))
                {
                    obj["z"] = subProperties.GetValue<float>("z");
                }

                if (subProperties.HasProperty("w"))
                {
                    obj["w"] = subProperties.GetValue<float>("w");
                }
            }

            rotation.Refresh(null, true);

            var scale = entity.AddComponent(TypeRefs.Core2D.TransformLocalScale);
            scale.Refresh(null, true);
            if (properties?.HasProperty("localScale") ?? false)
            {
                var container = properties.GetValue<ObjectContainer>("localScale");
                var obj = scale["scale"] as TinyObject;
                var subProperties = new MigrationContainer(container.GetValue<ObjectContainer>("Properties"));
                if (subProperties.HasProperty("x"))
                {
                    obj["x"] = subProperties.GetValue<float>("x");
                }

                if (subProperties.HasProperty("y"))
                {
                    obj["y"] = subProperties.GetValue<float>("y");
                }

                if (subProperties.HasProperty("z"))
                {
                    obj["z"] = subProperties.GetValue<float>("z");
                }
            }

            scale.Refresh(null, true);
        }
    }

    internal class Text2DRendererUpdater : IEntityComponentUpdater
    {
        public void UpdateEntityComponent(TinyEntity entity, MigrationContainer migration)
        {
            MigrationContainer properties = null;
            if (migration.HasProperty("Properties"))
            {
                properties = migration.GetContainer("Properties");
            }
            var text2DRenderer = entity.GetOrAddComponent(TypeRefs.Text.Text2DRenderer);

            text2DRenderer["text"] = properties?.GetValueOrDefault("text", string.Empty);
            text2DRenderer["style"] = entity.Ref;
            if (properties?.HasProperty("anchor") ?? false)
            {
                var anchorContainer = properties.GetContainer("anchor");
                var anchor = (TextAnchor)anchorContainer.GetValueOrDefault("Value", 0);
                var pivot = GetPivotFromTextAnchor(anchor);
                var pivotObject = (TinyObject)text2DRenderer["pivot"];
                pivotObject["x"] = pivot.x;
                pivotObject["y"] = pivot.y;
            }

            var text2DStyleNativeFont = entity.GetOrAddComponent(TypeRefs.Text.Text2DStyleNativeFont);
            text2DStyleNativeFont["font"] = entity.Ref;
            text2DStyleNativeFont["italic"] = properties?.GetValueOrDefault("italic", false);
            text2DStyleNativeFont["weight"] = (properties?.GetValueOrDefault<bool>("bool") ?? false) ? 700 : 400;

            var text2DStyle = entity.GetOrAddComponent(TypeRefs.Text.Text2DStyle);
            if (properties?.HasProperty("color") ?? false)
            {
                var color = properties.GetContainer("color");
                if (color?.HasProperty("Properties") ?? false)
                {
                    var colorContainer = color.GetContainer("Properties");
                    if (null != colorContainer)
                    {
                        text2DStyle["color"] = new TinyObject(entity.Registry, TypeRefs.Core2D.Color)
                        {
                            ["r"] = colorContainer.GetValueOrDefault("r", 1.0f),
                            ["g"] = colorContainer.GetValueOrDefault("g", 1.0f),
                            ["b"] = colorContainer.GetValueOrDefault("b", 1.0f),
                            ["a"] = colorContainer.GetValueOrDefault("a", 1.0f),
                        };
                    }
                }
            }


            text2DStyle["size"] = properties?.GetValueOrDefault("fontSize", 2.0f);

            entity.GetOrAddComponent(TypeRefs.Text.NativeFont);
        }

        public static Vector2 GetPivotFromTextAnchor(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return new Vector2(0.0f, 1.0f);
                case TextAnchor.UpperCenter: return new Vector2(0.5f, 1.0f);
                case TextAnchor.UpperRight: return new Vector2(1.0f, 1.0f);
                case TextAnchor.MiddleLeft: return new Vector2(0.0f, 0.5f);
                case TextAnchor.MiddleCenter: return new Vector2(0.5f, 0.5f);
                case TextAnchor.MiddleRight: return new Vector2(1.0f, 0.5f);
                case TextAnchor.LowerLeft: return new Vector2(0.0f, 0.0f);
                case TextAnchor.LowerCenter: return new Vector2(0.5f, 0.0f);
                case TextAnchor.LowerRight: return new Vector2(1.0f, 0.0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null);
            }
        }
    }

    internal class RectColliderUpdater : IEntityComponentUpdater
    {
        public void UpdateEntityComponent(TinyEntity entity, MigrationContainer migration)
        {
            MigrationContainer properties = null;
            if (migration.HasProperty("Properties"))
            {
                properties = migration.GetContainer("Properties");
            }

            var boxCollider = entity.GetOrAddComponent(TypeRefs.Physics2D.BoxCollider2D);
            boxCollider.Refresh(null, true);
            if ((properties?.HasProperty("width") ?? false) || (properties?.HasProperty("height") ?? false))
            {
                var size = boxCollider["size"] as TinyObject;
                if (properties?.HasProperty("width") ?? false)
                {
                    size["x"] = properties.GetValueOrDefault<float>("width");
                }
                if (properties?.HasProperty("height") ?? false)
                {
                    size["y"] = properties.GetValueOrDefault<float>("height");
                }
                boxCollider.Refresh(null, true);
            }

            if (properties?.HasProperty("pivot") ?? false)
            {
                var srcPivot = properties.GetContainer("pivot")?.GetContainer("Properties");
                var dstPivot = boxCollider["pivot"] as TinyObject;
                if (srcPivot?.HasProperty("x") ?? false)
                {
                    dstPivot["x"] = srcPivot?.GetValueOrDefault<float>("x");
                }
                if (srcPivot?.HasProperty("y") ?? false)
                {
                    dstPivot["y"] = srcPivot?.GetValueOrDefault<float>("y");
                }
                boxCollider.Refresh(null, true);
            }

            var rigidBody = entity.GetOrAddComponent(TypeRefs.Physics2D.RigidBody2D);
            rigidBody.Refresh(null, true);
            if (properties?.HasProperty("bodyType") ?? false)
            {
                var bodyType = properties.GetContainer("bodyType");
                var value = bodyType.GetValueOrDefault<int>("Value");
                var enumRef = new TinyEnum.Reference(TypeRefs.Physics2D.BodyType.Dereference(entity.Registry), value);
                rigidBody["bodyType"] = enumRef;
                rigidBody.Refresh(null, true);
            }

            if (properties?.HasProperty("fixedRotation") ?? false)
            {
                rigidBody["freezeRotation"] = properties.GetValueOrDefault<bool>("fixedRotation");
                rigidBody.Refresh(null, true);
            }

            if (properties?.HasProperty("friction") ?? false)
            {
                rigidBody["friction"] = properties.GetValueOrDefault<float>("friction");
                rigidBody.Refresh(null, true);
            }

            if (properties?.HasProperty("restitution") ?? false)
            {
                rigidBody["restitution"] = properties.GetValueOrDefault<float>("restitution");
                rigidBody.Refresh(null, true);
            }

            if (properties?.HasProperty("density") ?? false)
            {
                rigidBody["density"] = properties.GetValueOrDefault<float>("density");
                rigidBody.Refresh(null, true);
            }
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
