
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Tiny.Runtime.UILayout;

namespace Unity.Tiny
{
    internal static class EntityTemplateMenuItems
    {
        public const string k_Prefix = "GameObject/Tiny/";
        private const string k_2DObject = "2D Object/";
        private const string k_Audio = "Audio/";
        private const string k_UI = "UI/";

        private static bool ValidForMenuItems => null != TinyEditorApplication.Project && GroupManager.LoadedEntityGroupCount > 0;

        private static TinyEditorContext Context => TinyEditorApplication.EditorContext;
        private static IRegistry Registry => Context.Registry;
        private static EntityTemplateManager Templates => Context.Context.GetManager<EntityTemplateManager>();
        private static IEntityGroupManagerInternal GroupManager => Context.Context.GetManager<IEntityGroupManagerInternal>();
        private static TinyEntityGroup.Reference ActiveGroupRef => GroupManager.ActiveEntityGroup;
        private static TinyEntityGroup ActiveGroup => ActiveGroupRef.Dereference(Registry);
        private static TinyModule MainModule => Context.Project.Module.Dereference(Registry);


        private static List<TinyEntity> CreateWithContext(string name, EntityTemplate template, bool alwaysAsRoot, params IRegistryObject[] context)
        {
            return CreateWithContext(name, template, alwaysAsRoot, Filter(context)).ToList();
        }

        private static IEnumerable<TinyEntity> CreateWithContext(string name, EntityTemplate template, bool alwaysAsRoot, IEnumerable<IRegistryObject> context)
        {
            var objects = context.ToList();
            // No context, always create as a root in the active scene.
            if (objects.Count == 0)
            {
                yield return CreateAsRoot(name, template);
            }

            var entities = objects.OfType<TinyEntity>();

            // Dealing with entities, if we want to only create as a root, do it once for each entity group.
            if (alwaysAsRoot)
            {
                foreach (var entityGroup in entities.Select(e => e.EntityGroup).Distinct())
                {
                    yield return CreateAsRoot(name, entityGroup, template);
                }
            }
            // Add as a child for each entity.
            else
            {
                foreach (var entity in entities)
                {
                    yield return CreateAsChild(name, entity, template);
                }
            }

            // Add to entity groups, always as a root.
            var entityGroups = objects.OfType<TinyEntityGroup>();
            foreach (var entityGroup in entityGroups)
            {
                yield return CreateAsRoot(name, entityGroup, template);
            }
        }

        private static TinyEntity CreateAsRoot(string name, EntityTemplate template = null)
        {
            var group = ActiveGroup;
            var uniqueName = TinyUtility.GetUniqueName(group.Entities.Deref(Registry), name);
            return Templates.CreateFromTemplate(uniqueName, group, template ?? EntityTemplates.Empty);
        }

        private static TinyEntity CreateAsRoot(string name, TinyEntityGroup group, EntityTemplate template = null)
        {
            group = group ?? ActiveGroup;
            var uniqueName = TinyUtility.GetUniqueName(group.Entities.Deref(Registry), name);
            return Templates.CreateFromTemplate(uniqueName, group, template ?? EntityTemplates.Empty);
        }

        private static TinyEntity CreateAsChild(string name, TinyEntity parent, EntityTemplate template = null)
        {
            var group = parent.EntityGroup;
            var graph = GroupManager.GetSceneGraph((TinyEntityGroup.Reference)group);
            var parentNode = graph.FindNode(parent);
            var uniqueName = TinyUtility.GetUniqueName(parentNode.Children.OfType<EntityNode>().Select(node => node.EntityRef).Deref(Registry), name);
            return Templates.CreateFromTemplate(uniqueName, parent, template ?? EntityTemplates.Empty);
        }

        public static bool ValidateMenuItems()
        {
            return ValidForMenuItems;
        }

        [TinyEntityTemplateMenuItem(k_Prefix, nameof(ValidateMenuItems), 49)]
        internal static void Create_Empty(params IRegistryObject[] context)
        {
            var entities = CreateWithContext("Entity", EntityTemplates.Empty, true, context);
            PushChanges(entities);
        }

        [TinyEntityTemplateMenuItem(k_Prefix, nameof(ValidateMenuItems), 50)]
        internal static void Create_Empty_Child(params IRegistryObject[] context)
        {
            var entities = CreateWithContext("Entity", EntityTemplates.Empty, false, context);
            PushChanges(entities);
        }

        [TinyEntityTemplateMenuItem(k_Prefix + k_2DObject, nameof(ValidateMenuItems), 51)]
        internal static void Sprite(params IRegistryObject[] context)
        {
            MainModule.AddExplicitModuleDependency((TinyModule.Reference)Registry.FindByName<TinyModule>("UTiny.Core2D"));
            var sprites = CreateWithContext("Sprite", EntityTemplates.Sprite, false, context);
            PushChanges(sprites);
        }

        [TinyEntityTemplateMenuItem(k_Prefix + k_Audio, nameof(ValidateMenuItems), 52)]
        internal static void Audio_Source(params IRegistryObject[] context)
        {
            MainModule.AddExplicitModuleDependency((TinyModule.Reference)Registry.FindByName<TinyModule>("UTiny.Audio"));
            var sources = CreateWithContext("Audio Source", EntityTemplates.AudioSource, false, context);
            PushChanges(sources);
        }

        [TinyEntityTemplateMenuItem(k_Prefix + k_UI, nameof(ValidateMenuItems), 53)]
        internal static void UI_Canvas(params IRegistryObject[] context)
        {
            MainModule.AddExplicitModuleDependency((TinyModule.Reference)Registry.FindByName<TinyModule>("UTiny.UILayout"));
            var canvases = CreateWithContext("Canvas", EntityTemplates.Canvas, true, context);
            var cameras =CreateWithContext("CanvasCamera", EntityTemplates.Camera, true, context);

            Assert.IsTrue(canvases.Count == cameras.Count);
            for (var i = 0; i < canvases.Count; ++i)
            {
                var canvas = canvases[i].GetComponent(TypeRefs.UILayout.UICanvas);
                canvas.AssignPropertyFrom("camera", (TinyEntity.Reference) cameras[i]);
            }
            PushChanges(canvases);
        }

        [TinyEntityTemplateMenuItem(k_Prefix + k_UI, nameof(ValidateMenuItems), 54)]
        internal static void Image(params IRegistryObject[] context)
        {
            const string name = "Image";
            MainModule.AddExplicitModuleDependency((TinyModule.Reference)Registry.FindByName<TinyModule>("UTiny.UILayout"));
            var images = CreateWithContext(name, EntityTemplates.Image, false, context);
            ReparentUIEntities(images, name);
            foreach (var image in images)
            {
                var rt = image.GetComponent(TypeRefs.UILayout.RectTransform);
                var tiny = new TinyRectTransform(rt);
                tiny.anchorMax = tiny.anchorMin = tiny.pivot = new Vector2(0.5f, 0.5f);
                tiny.sizeDelta = new Vector2(100, 100);

                var options = image.GetComponent(TypeRefs.Core2D.Sprite2DRendererOptions);
                options.AssignIfDifferent("drawMode", DrawMode.Stretch);
            }
            PushChanges(images);
        }

        [TinyEntityTemplateMenuItem(k_Prefix + k_UI, nameof(ValidateMenuItems), 55)]
        internal static void Panel(params IRegistryObject[] context)
        {
            const string name = "Panel";
            MainModule.AddExplicitModuleDependency((TinyModule.Reference)Registry.FindByName<TinyModule>("UTiny.UILayout"));
            var panels = CreateWithContext(name, EntityTemplates.Image, false, context);
            ReparentUIEntities(panels, name);
            foreach (var panel in panels)
            {
                var options = panel.GetComponent(TypeRefs.Core2D.Sprite2DRendererOptions);
                options.AssignIfDifferent("drawMode", DrawMode.Stretch);
            }
            PushChanges(panels);
        }

        [TinyEntityTemplateMenuItem(k_Prefix, nameof(ValidateMenuItems), 65)]
        internal static void Camera(params IRegistryObject[] context)
        {
            var cameras = CreateWithContext("Camera", EntityTemplates.Camera, false, context);
            PushChanges(cameras);
        }

        private static TinyEntity GetOrCreateCanvas(TinyEntityGroup group, bool forceCreate = false)
        {
            if (!forceCreate)
            {
                // Try to find canvas..
                var graph = GroupManager.GetSceneGraph((TinyEntityGroup.Reference)group);
                foreach (var root in graph.Roots.OfType<EntityNode>())
                {
                    var entity = root.EntityRef.Dereference(Registry);
                    
                    if (null != entity.GetComponent(TypeRefs.UILayout.UICanvas))
                    {
                        return entity;
                    }
                }
            }

            MainModule.AddExplicitModuleDependency((TinyModule.Reference)Registry.FindByName<TinyModule>("UTiny.UILayout"));
            var uiCanvasEntity = CreateAsRoot("Canvas", group, EntityTemplates.Canvas);
            var cameraEntity = CreateAsRoot("CanvasCamera", group, EntityTemplates.Camera);

            var canvas = uiCanvasEntity.GetComponent(TypeRefs.UILayout.UICanvas);
            canvas.AssignPropertyFrom("camera", (TinyEntity.Reference) cameraEntity);
            return uiCanvasEntity;
        }

        private static void ReparentUIEntities(List<TinyEntity> entities, string baseName)
        {
            var pooled = ListPool<TinyEntity>.Get();
            var names = ListPool<TinyEntity>.Get();
            try
            {
                foreach (var image in entities)
                {
                    var root = image.GetRoot();
                    if (null == root.GetComponent(TypeRefs.UILayout.UICanvas))
                    {
                        pooled.Add(image);
                    }
                }

                foreach (var byGroup in pooled.GroupBy(e => e.EntityGroup))
                {
                    names.Clear();
                    var canvas = GetOrCreateCanvas(byGroup.Key);
                    var graph = GroupManager.GetSceneGraph((TinyEntityGroup.Reference)canvas.EntityGroup);
                    var parentNode = graph.FindNode(canvas);
                    // Canvas already existed.
                    if (null != parentNode)
                    {
                        names.AddRange(parentNode.Children.OfType<EntityNode>().Select(node => node.EntityRef).Deref(Registry));
                    }

                    foreach (var entity in byGroup)
                    {
                        var entityRef = entity.AsReference();
                        entity.EntityGroup.RemoveEntityReference(entityRef);
                        entity.EntityGroup = canvas.EntityGroup;
                        entity.EntityGroup.AddEntityReference(entityRef);

                        var uniqueName = TinyUtility.GetUniqueName(names, baseName);
                        entity.Name = uniqueName;
                        names.Add(entity);
                        var transform = entity.GetComponent(TypeRefs.Core2D.TransformNode);
                        transform.AssignIfDifferent("parent", (TinyEntity.Reference)canvas);
                    }
                }
            }
            finally
            {
                ListPool<TinyEntity>.Release(names);
                ListPool<TinyEntity>.Release(pooled);
            }
        }

        private static void PushChanges(List<TinyEntity> entities)
        {
            TinyHierarchyWindow.SelectOnNextPaint(entities);
            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
        }

        private static IEnumerable<IRegistryObject> Filter(IRegistryObject[] context)
        {
            var hasEntity = false;
            foreach (var c in context)
            {
                if (c is TinyEntity)
                {
                    hasEntity = true;
                    yield return c;
                }
            }

            if (hasEntity)
            {
                yield break;
            }

            foreach (var c in context)
            {
                yield return c;
            }
        }
    }
}
