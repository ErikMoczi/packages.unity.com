using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Tiny.Runtime.EditorExtensions;
using Unity.Tiny.Runtime.Tilemap2D;

namespace Unity.Tiny
{
    internal struct EntityGroupSetupOptions
    {
        /// <summary>
        /// If set to true a component with the entityGroup name will be added to each entity
        /// </summary>
        public bool IncludeEntityGroupComponent;
        
        /// <summary>
        /// If set to true a component for each layer will be added to all entities
        /// </summary>
        public bool IncludeEntityLayer;

        public static EntityGroupSetupOptions Scene { get; } = new EntityGroupSetupOptions
        {
            IncludeEntityGroupComponent = true,
            IncludeEntityLayer = true
        };
        
        public static EntityGroupSetupOptions Resources { get; } = new EntityGroupSetupOptions
        {
            IncludeEntityGroupComponent = false,
            IncludeEntityLayer = false
        };
    }
    
    internal class EntityGroupSetupVisitor : TinyProject.Visitor, IDisposable
    {
        private static readonly int k_UnityDefaultLayer = LayerMask.NameToLayer("Default");

        private static void AbortIfUnknownType(object type, TinyEntity.Reference value)
        {
            if (type == null)
            {
                throw new Exception($"Type \"{value.Name}\" has unknown Type ID {value.Id} that is not found in Tiny Type Registry.");
            }
        }

        private static void AbortIfUnknownType(object type, TinyObject value)
        {
            if (type == null)
            {
                throw new Exception($"Type \"{value.Type.Name}\" has unknown Type ID {value.Type.Id} that is not found in Tiny Type Registry, when processing TinyObject \"{value.Name}\".");
            }
        }

        private static void AbortIfUnknownType(object type, TinyEnum.Reference value, IProperty Property)
        {
            if (type == null)
            {
                throw new Exception($"Type \"{value.Type.Name}\" has unknown Type ID {value.Type.Id} that is not found in Tiny Type Registry, when processing Enum reference \"{value.Name}\" with ID {value.Id} (its value was \"{value.Value}\"), property name \"{Property.Name}\".");
            }
        }

        private static void AbortIfUnknownType(object type, TinyList value, IProperty Property)
        {
            if (type == null)
            {
                throw new Exception($"Type \"{value.Type.Name}\" has unknown Type ID {value.Type.Id} that is not found in Tiny Type Registry, when processing TinyList \"{value.Name}\", property name \"{Property.Name}\".");
            }
        }

        private static void AbortIfUnknownType(object type, TinyObject value, IProperty Property)
        {
            if (type == null)
            {
                throw new Exception($"Type \"{value.Type.Name}\" has unknown Type ID {value.Type.Id} that is not found in Tiny Type Registry, when processing TinyObject \"{value.Name}\", property name \"{Property.Name}\".");
            }
        }

        public class VisitorContext
        {
            public TinyProject Project;
            public TinyModule Module;
            public IRegistry Registry;
            public TinyCodeWriter Writer;
            public TinyEntityGroup EntityGroup;
            public TinyEntity.Reference Entity;
            public TinyObject Component;
            public EntityGroupSetupOptions Options;

            public class IndexMap<T>
            {
                private Dictionary<T, int> Map = new Dictionary<T, int>();

                public int GetOrAddValue(T item)
                {
                    int index;
                    if (Map.TryGetValue(item, out index))
                    {
                        return index;
                    }
                    index = Map.Count;
                    Map[item] = index;
                    return index;
                }

                public bool TryGetValue(T item, out int index)
                {
                    return Map.TryGetValue(item, out index);
                }
            }

            // Various type index maps while writing to entities file
            public IndexMap<TinyEntity.Reference> EntityIndexMap = new IndexMap<TinyEntity.Reference>();
            public IndexMap<TinyObject> ComponentIndexMap = new IndexMap<TinyObject>();
            public IndexMap<TinyObject> StructIndexMap = new IndexMap<TinyObject>();
            public IndexMap<TinyList> ArrayIndexMap = new IndexMap<TinyList>();

            // Each archetype will be generated once
            // In order to keep a mapping of already generated archetypes we are generating the string used to build to archetype (ordered)
            // and using that as the key for the index map (this is not the best approach and can be optimized later)
            public IndexMap<string> ArchetypeIndexMap = new IndexMap<string>();
        }

        /// <summary>
        /// If set to true enums will be exported as their underlying type
        /// e.g. c1.setEnumField(3);
        /// 
        /// If false enums are exported with their fully qualified name
        /// e.g. c1.setEnumField(MyEnumType.MyEnumValue);
        /// </summary>
        public static bool ExportEnumAsValue = true;

        public TinyCodeWriter Writer { private get; set; }
        public TinyBuildReport.TreeNode Report { private get; set; }

        /// <summary>
        /// Setup options used to control the generated output
        ///
        /// i.e. Used to export scene format vs asset format
        /// </summary>
        public EntityGroupSetupOptions Options = EntityGroupSetupOptions.Scene;

        private static string EscapeJsString(string content)
        {
            return Json.EncodeJsonString(content);
        }

        private static string GetFullyQualifiedLayerName(string layerName)
        {
            const string fullNamespace = "ut.layers.";

            layerName = layerName.Replace(" ", "");
            if (layerName.StartsWith(fullNamespace))
            {
                return layerName;
            }
            return fullNamespace + layerName;
        }

        private static bool ActiveInHierarchy(TinyEntity entity)
        {
            if (!entity.Enabled)
            {
                return false;
            }

            var registry = entity.Registry;
            TinyObject transform;
            var current = entity;

            while (null != (transform = current.GetComponent(TypeRefs.Core2D.TransformNode)))
            {
                var parent = transform.GetProperty<TinyEntity.Reference>("parent").Dereference(registry);
                if (null == parent)
                {
                    break;
                }

                if (!parent.Enabled)
                {
                    return false;
                }

                current = parent;
            }

            return true;
        }

        private static string GetDefaultJsValue(TinyType type)
        {
            switch (type.TypeCode)
            {
                case TinyTypeCode.Boolean:
                    return "false";
                case TinyTypeCode.Int8:
                case TinyTypeCode.Int16:
                case TinyTypeCode.Int32:
                case TinyTypeCode.Int64:
                case TinyTypeCode.UInt8:
                case TinyTypeCode.UInt16:
                case TinyTypeCode.UInt32:
                case TinyTypeCode.UInt64:
                case TinyTypeCode.Float32:
                case TinyTypeCode.Float64:
                case TinyTypeCode.Enum:
                    return "0";
                case TinyTypeCode.String:
                    return "\"\"";
                case TinyTypeCode.Component:
                case TinyTypeCode.Struct:
                case TinyTypeCode.Configuration:
                    return $"new {TinyScriptUtility.GetJsTypeName(type)}";
                case TinyTypeCode.EntityReference:
                case TinyTypeCode.UnityObject:
                    return "ut.Entity.NONE";
                default:
                    return "null";
            }
        }

        private static string GetArchetype(TinyEntity entity, IEnumerable<string> extraTypes)
        {
            var list = new List<string>();

            list.AddRange(extraTypes);

            foreach (var component in entity.Components)
            {
                var type = component.Type.Dereference(entity.Registry);
                AbortIfUnknownType(type, component);
                list.Add(TinyScriptUtility.GetJsTypeName(type));
            }

            list.Sort();

            return $"{string.Join(", ", list)}";
        }

        public override void VisitEntityGroup(TinyEntityGroup entityGroup)
        {
            // Export-time components
            using (entityGroup.Registry.DontTrackChanges())
            {
                var changed = Persistence.IsPersistentObjectChanged(entityGroup);
                
                foreach (var entity in entityGroup.Entities.Select(e => e.Dereference(entityGroup.Registry)))
                {
                    // Store entity layer in component
                    if (entity.Layer != k_UnityDefaultLayer)
                    {
                        var entityLayer = entity.GetOrAddComponent<TinyEntityLayer>();
                        entityLayer.layer = entity.Layer;
                    }

                    // Hack: Store camera culling mask since we can't restore from the component list
                    if (entity.HasComponent(TypeRefs.Core2D.Camera2D))
                    {
                        var cameraCullingMask = entity.GetOrAddComponent<TinyCameraCullingMask>();
                        cameraCullingMask.mask = entity.GetComponent<Runtime.Core2D.TinyCamera2D>().layerMask;
                    }

                    // If entity has Tilemap, make sure it also has TilemapRechunk
                    if (entity.HasComponent(TypeRefs.Tilemap2D.Tilemap))
                    {
                        entity.GetOrAddComponent(TypeRefs.Tilemap2D.TilemapRechunk);
                    }
                }

                if (!changed)
                {
                    Persistence.RegisterVersions(entityGroup);
                }
            }

            var begin = Writer.Length;

            Module = TinyUtility.GetModules(entityGroup).FirstOrDefault();
            Writer.Line($"{TinyHTML5Builder.k_EntityGroupNamespace}.{Module.Namespace}.{entityGroup.Name}.name = {EscapeJsString(entityGroup.Name)};");
            Writer.WriteRaw($"{TinyHTML5Builder.k_EntityGroupNamespace}.{Module.Namespace}.{entityGroup.Name}.load = ");
            WriteEntityGroupSetupFunction(Writer, Project, entityGroup, Options);

            Report.AddChild(entityGroup.Name, System.Text.Encoding.ASCII.GetBytes(Writer.Substring(begin)));

#if UNITY_EDITOR_WIN
            Writer.Length -= 2;
#else
            Writer.Length -= 1;
#endif
        }

        public static void WriteEntityGroupSetupFunction(TinyCodeWriter writer, TinyProject project, TinyEntityGroup entityGroup, EntityGroupSetupOptions options)
        {
            var context = new VisitorContext
            {
                Project = project,
                Module = project.Module.Dereference(project.Registry),
                Registry = entityGroup.Registry,
                Writer = writer,
                EntityGroup = entityGroup,
                Options = options
            };

            using (writer.Scope("function(world)"))
            {
                // Write entities setup
                foreach (var reference in entityGroup.Entities)
                {
                    context.Entity = reference;
                    WriteEntitySetup(context);
                }

                // Write entities components
                foreach (var reference in entityGroup.Entities)
                {
                    context.Entity = reference;
                    WriteEntityComponents(context);
                }

                // Write function return value
                writer.WriteIndent();
                writer.WriteRaw("return [");

                if (entityGroup.Entities.Count > 0)
                {
                    writer.WriteRaw(entityGroup.Entities.Select(e => $"e{context.EntityIndexMap.GetOrAddValue(e)}").Aggregate((c, n) => c + ", " + n));
                }

                writer.WriteRaw("];\n");
            }
            writer.Line().Line();
        }

        private static void WriteEntitySetup(VisitorContext context)
        {
            var writer = context.Writer;
            var entity = context.Entity.Dereference(context.Registry);
            AbortIfUnknownType(entity, context.Entity);
            var entityIndex = context.EntityIndexMap.GetOrAddValue(context.Entity);

            var disabled = !ActiveInHierarchy(entity);

            var extraTypes = new List<string>();

            if (entity.Static)
            {
                extraTypes.Add("ut.Core2D.TransformStatic");
            }

            if (disabled)
            {
                extraTypes.Add("ut.Disabled");
            }
            
            if (context.Options.IncludeEntityGroupComponent)
            {
                extraTypes.Add("this.Component");
            }

            if (context.Options.IncludeEntityLayer)
            {
                var layerName = LayerMask.LayerToName(entity.Layer);
                if (string.IsNullOrEmpty(layerName))
                {
                    throw new Exception(
                        $"Entity '{entity.Name}' is on Layer {entity.Layer}, which is not defined. Open the 'Tags and Layers' settings panel to define it.");
                }
                extraTypes.Add(GetFullyQualifiedLayerName(layerName));
            }
            
            var archetype = GetArchetype(entity, extraTypes);
            if (!context.ArchetypeIndexMap.TryGetValue(archetype, out var archetypeIndex))
            {
                archetypeIndex = context.ArchetypeIndexMap.GetOrAddValue(archetype);
                writer.Line($"var arch{archetypeIndex} = world.createArchetype({archetype})");
            }

            writer.Line($"var e{entityIndex} = world.createEntity(arch{archetypeIndex});");

            // TODO: make names opt-in, at least in release
            writer.Line($"world.setEntityName(e{entityIndex}, {EscapeJsString(entity.Name)});");
        }

        private static void WriteEntityComponents(VisitorContext context)
        {
            var entity = context.Entity.Dereference(context.Registry);
            AbortIfUnknownType(entity, context.Entity);
            foreach (var component in entity.Components)
            {
                context.Component = component;
                var type = component.Type.Dereference(context.Registry);

                // Skip exporting 'development' components in release configuration
                if (TinyBuildPipeline.WorkspaceBuildOptions.Configuration == TinyBuildConfiguration.Release && type.ExportFlags.HasFlag(TinyExportFlags.Development))
                {
                    continue;
                }

                if (type.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                {
                    // TODO: whenever we'll add a editor-only component, we should create and implement this method.
                    // WriteExtensionComponent(context);
                }
                else
                {
                    WriteComponent(context);
                }
            }
        }

        private static void AbortIfUnknownType(TinyType type, TinyObject component, TinyEntity.Reference entity, TinyEntityGroup entityGroup)
        {
            if (type == null)
            {
                throw new Exception($"{TinyConstants.ApplicationName}: Missing component type, ComponentType=[{component.Type.Name}] Entity=[{entity.Name}] Group=[{entityGroup.Name}]");
            }
        }

        private static void WriteComponent(VisitorContext context)
        {
            var writer = context.Writer;
            var entity = context.Entity;
            var entityGroup = context.EntityGroup;
            var component = context.Component;

            if (component.Properties.PropertyBag.PropertyCount == 0)
            {
                return;
            }

            var entityIndex = context.EntityIndexMap.GetOrAddValue(entity);
            var componentIndex = context.ComponentIndexMap.GetOrAddValue(component);

            var type = component.Type.Dereference(component.Registry);
            AbortIfUnknownType(type, component, entity, entityGroup);

            writer.Line($"var c{componentIndex} = new {TinyScriptUtility.GetJsTypeName(type)}();");
            component.Properties.Visit(new ComponentVisitor
            {
                VisitorContext = context,
                Path = $"c{componentIndex}",
                Entity = $"e{entityIndex}"
            });
            writer.Line($"world.setComponentData(e{entityIndex}, c{componentIndex});");
        }

        private void EndEntityGroup()
        {
        }

        public void Dispose()
        {
            EndEntityGroup();
        }

        public class ComponentVisitor : PropertyVisitor,
            IExcludeVisit<TinyObject>,
            ICustomVisit<TinyObject>,
            IExcludeVisit<TinyList>,
            ICustomVisit<TinyList>,
            IExcludeVisit<TinyEnum.Reference>,
            ICustomVisit<TinyEnum.Reference>,
            IExcludeVisit<TinyEntity.Reference>,
            ICustomVisit<TinyEntity.Reference>,
            IExcludeVisit<int>,
            ICustomVisit<int>,
            IExcludeVisit<float>,
            ICustomVisit<float>,
            IExcludeVisit<double>,
            ICustomVisit<double>,
            IExcludeVisit<bool>,
            ICustomVisit<bool>,
            ICustomVisit<char>,
            IExcludeVisit<string>,
            ICustomVisit<string>,
            ICustomVisit<Texture2D>,
            ICustomVisit<Sprite>,
            ICustomVisit<TileBase>,
            ICustomVisit<Tilemap>,
            ICustomVisit<AudioClip>,
            ICustomVisit<AnimationClip>,
            ICustomVisit<TMPro.TMP_FontAsset>
        {
            public VisitorContext VisitorContext { private get; set; }
            public string Path { private get; set; }
            public string Entity { private get; set; }

            private readonly StructVisitor m_StructVisitor = new StructVisitor();
            private readonly ListVisitor m_ListVisitor = new ListVisitor();

            private IPropertyContainer m_Container;

            protected override void VisitSetup<TContainer, TValue>(ref TContainer container, ref VisitContext<TValue> context)
            {
                base.VisitSetup(ref container, ref context);
                m_Container = container;
            }

            public override bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            {
                if (context.Property is ITinyValueProperty p && p.IsEditorOnly)
                {
                    return true;
                }
                return base.ExcludeVisit(container, context);
            }

            protected override bool ExcludeVisit<TValue>(TValue value)
            {
                return Equals(value, default(TValue));
            }

            protected override void Visit<TValue>(TValue value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = {value.ToString()};");
            }

            bool IExcludeVisit<TinyObject>.ExcludeVisit(TinyObject value)
            {
                return value.Properties.PropertyBag.PropertyCount == 0;
            }

            void ICustomVisit<TinyObject>.CustomVisit(TinyObject value)
            {
                var type = value.Type.Dereference(value.Registry);
                AbortIfUnknownType(type, value, Property);
                if (type.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                {
                    var exportedName = TinyEditorExtensionsGenerator.GetExportedAssetName(value);
                    VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = ut.EntityLookupCache.getByName(world, '{exportedName}');");
                }
                else
                {
                    var index = VisitorContext.StructIndexMap.GetOrAddValue(value);
                    VisitorContext.Writer.Line($"var s{index} = new {TinyScriptUtility.GetJsTypeName(type)}();");
                    m_StructVisitor.VisitorContext = VisitorContext;
                    m_StructVisitor.Path = $"s{index}";
                    value.Properties.Visit(m_StructVisitor);
                    VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = s{index};");
                }
            }

            bool IExcludeVisit<TinyList>.ExcludeVisit(TinyList value)
            {
                return value.Count == 0;
            }

            void ICustomVisit<TinyList>.CustomVisit(TinyList value)
            {
                var index = VisitorContext.ArrayIndexMap.GetOrAddValue(value);
                var type = value.Type.Dereference(VisitorContext.Registry);
                AbortIfUnknownType(type, value, Property);
                var defaultJsValue = GetDefaultJsValue(type);
                VisitorContext.Writer.Line($"var a{index} = [{string.Join(", ", Enumerable.Range(0, value.Count).Select(x => defaultJsValue).ToArray())}];");
                m_ListVisitor.VisitorContext = VisitorContext;
                m_ListVisitor.Path = $"a{index}";
                value.Visit(m_ListVisitor);
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = a{index};");
            }

            public bool ExcludeVisit(TinyEnum.Reference value)
            {
                return value.Type.Equals(TinyType.Reference.None)
                       || value.Equals(TinyEnum.Reference.None)
                       || value.Id == default;
            }

            void ICustomVisit<TinyEnum.Reference>.CustomVisit(TinyEnum.Reference value)
            {
                var type = value.Type.Dereference(VisitorContext.Registry);
                AbortIfUnknownType(type, value, Property);
                var normalized = ExportEnumAsValue ? (type.DefaultValue as TinyObject)?[value.Name] : $"{TinyScriptUtility.GetJsTypeName(type)}.{value.Name}";
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = {normalized};");
            }

            bool IExcludeVisit<TinyEntity.Reference>.ExcludeVisit(TinyEntity.Reference value)
            {
                return value.Equals(TinyEntity.Reference.None);
            }

            void ICustomVisit<TinyEntity.Reference>.CustomVisit(TinyEntity.Reference value)
            {
                if (!VisitorContext.EntityIndexMap.TryGetValue(value, out var index))
                {
                    return;
                }

                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = e{index};");
            }

            bool IExcludeVisit<int>.ExcludeVisit(int value)
            {
                if (Property.Name == "layerMask")
                {
                    return false;
                }
                return value == 0;
            }

            void ICustomVisit<int>.CustomVisit(int value)
            {
                if (Property.Name == "layerMask")
                {
                    // HACK: Create custom component exporter
                    // In this case, we have a double hack (TM)
                    //   1- We need to convert Unity layer to a component-based layering system.
                    //   2- We need to set the cullingMode to be Any
                    var layer = value;
                    var exportedLayers = new List<string>();
                    for (var i = 0; i < 32; ++i)
                    {
                        // if bit is set
                        if ((layer & 1 << i) == 1 << i)
                        {
                            var layerName = LayerMask.LayerToName(i);
                            if (string.IsNullOrEmpty(layerName))
                            {
                                continue;
                            }
                            exportedLayers.Add($"{GetFullyQualifiedLayerName(layerName)}.cid");
                        }
                    }
                    VisitorContext.Writer.Line($"{Path}.cullingMask = [{string.Join(", ", exportedLayers.ToArray())}];");
                    VisitorContext.Writer.Line($"{Path}.cullingMode = 2;");
                    return;
                }

                if (Property.Name == "layer")
                {
                    // HACK: Unity uses a unique identifier for sorting layers to support renaming/reordering.
                    // At export time, we need to convert the sorting layer ID to an actual sorting value.
                    var obj = (m_Container as TinyObject.PropertiesContainer)?.ParentObject ?? null;
                    if (obj != null && obj.Type.Equals(TypeRefs.Core2D.LayerSorting))
                    {
                        var layer = SortingLayer.GetLayerValueFromID(value);
                        VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = {layer};");
                        return;
                    }
                }
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = {value};");
            }

            bool IExcludeVisit<float>.ExcludeVisit(float value)
            {
                return Math.Abs(value) <= float.Epsilon;
            }

            void ICustomVisit<float>.CustomVisit(float value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            bool IExcludeVisit<double>.ExcludeVisit(double value)
            {
                return Math.Abs(value) <= double.Epsilon;
            }

            void ICustomVisit<double>.CustomVisit(double value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            bool IExcludeVisit<bool>.ExcludeVisit(bool value)
            {
                return !value;
            }

            void ICustomVisit<bool>.CustomVisit(bool value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = {(value ? "true" : "false")};");
            }

            void ICustomVisit<char>.CustomVisit(char value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = {EscapeJsString(string.Empty + value)};");
            }

            bool IExcludeVisit<string>.ExcludeVisit(string value)
            {
                return string.IsNullOrEmpty(value);
            }

            void ICustomVisit<string>.CustomVisit(string value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = {EscapeJsString(value)};");
            }

            private string PropertySetter(string name)
            {
                return $"{Path}.{name}";
            }

            void ICustomVisit<Texture2D>.CustomVisit(Texture2D value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<Sprite>.CustomVisit(Sprite value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<TileBase>.CustomVisit(TileBase value)
            {
                if (value is Tile)
                {
                    VisitObjectEntity(value);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            void ICustomVisit<Tilemap>.CustomVisit(Tilemap value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<AudioClip>.CustomVisit(AudioClip value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<AnimationClip>.CustomVisit(AnimationClip value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<TMPro.TMP_FontAsset>.CustomVisit(TMPro.TMP_FontAsset value)
            {
                VisitObjectEntity(value);
            }

            private void VisitObjectEntity<TValue>(TValue value) where TValue : UnityEngine.Object
            {
                var assetName = TinyAssetExporter.GetAssetName(VisitorContext.Module, value);

                // HACK
                if (TinyAssetEntityGroupGenerator.ObjectToEntityMap.TryGetValue(value, out var assetEntity) && VisitorContext.EntityIndexMap.TryGetValue(assetEntity.Ref, out var assetEntityIndex))
                {
                    VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = e{assetEntityIndex};");
                }
                else
                {
                    VisitorContext.Writer.Line($"{PropertySetter(Property.Name)} = ut.EntityLookupCache.getByName(world, '{TinyAssetEntityGroupGenerator.GetAssetEntityPath(value)}{assetName}');");
                }
            }
        }

        private class StructVisitor : PropertyVisitor,
            IExcludeVisit<TinyObject>,
            ICustomVisit<TinyObject>,
            IExcludeVisit<TinyList>,
            ICustomVisit<TinyList>,
            IExcludeVisit<TinyEnum.Reference>,
            ICustomVisit<TinyEnum.Reference>,
            IExcludeVisit<TinyEntity.Reference>,
            ICustomVisit<TinyEntity.Reference>,
            ICustomVisit<int>,
            ICustomVisit<float>,
            ICustomVisit<double>,
            ICustomVisit<bool>,
            ICustomVisit<char>,
            IExcludeVisit<string>,
            ICustomVisit<string>,
            ICustomVisit<Texture2D>,
            ICustomVisit<Sprite>,
            ICustomVisit<TileBase>,
            ICustomVisit<Tilemap>,
            ICustomVisit<AudioClip>,
            ICustomVisit<AnimationClip>,
            ICustomVisit<TMPro.TMP_FontAsset>
        {
            public VisitorContext VisitorContext { private get; set; }
            public string Path { private get; set; }
            public TinyObject Struct { private get; set; }

            public override bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            {
                if (context.Property is ITinyValueProperty p && p.IsEditorOnly)
                {
                    return true;
                }
                return base.ExcludeVisit(container, context);
            }

            protected override bool ExcludeVisit<TValue>(TValue value)
            {
                return false;
            }

            protected override void Visit<TValue>(TValue value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {value.ToString()};");
            }

            bool IExcludeVisit<TinyObject>.ExcludeVisit(TinyObject value)
            {
                return value.Properties.PropertyBag.PropertyCount == 0;
            }

            void ICustomVisit<TinyObject>.CustomVisit(TinyObject value)
            {
                value.Properties.Visit(new StructVisitor { VisitorContext = VisitorContext, Path = $"{Path}.{Property.Name}", Struct = value });
            }

            bool IExcludeVisit<TinyList>.ExcludeVisit(TinyList value)
            {
                return value.Count == 0;
            }

            void ICustomVisit<TinyList>.CustomVisit(TinyList value)
            {
                var index = VisitorContext.ArrayIndexMap.GetOrAddValue(value);
                var type = value.Type.Dereference(VisitorContext.Registry);
                AbortIfUnknownType(type, value, Property);
                var defaultJsValue = GetDefaultJsValue(type);
                VisitorContext.Writer.Line($"var a{index} = [{string.Join(", ", Enumerable.Range(0, value.Count).Select(x => defaultJsValue).ToArray())}];");
                value.Visit(new ListVisitor { VisitorContext = VisitorContext, Path = $"a{index}" });
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = a{index};");
            }

            public bool ExcludeVisit(TinyEnum.Reference value)
            {
                return value.Type.Equals(TinyType.Reference.None)
                       || value.Equals(TinyEnum.Reference.None)
                       || value.Id == default;
            }

            void ICustomVisit<TinyEnum.Reference>.CustomVisit(TinyEnum.Reference value)
            {
                var type = value.Type.Dereference(VisitorContext.Registry);
                AbortIfUnknownType(type, value, Property);
                var normalized = ExportEnumAsValue ? (type.DefaultValue as TinyObject)?[value.Name] : $"{TinyScriptUtility.GetJsTypeName(type)}.{value.Name}";
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {normalized};");
            }

            bool IExcludeVisit<TinyEntity.Reference>.ExcludeVisit(TinyEntity.Reference value)
            {
                return value.Equals(TinyEntity.Reference.None);
            }

            void ICustomVisit<TinyEntity.Reference>.CustomVisit(TinyEntity.Reference value)
            {
                if (!VisitorContext.EntityIndexMap.TryGetValue(value, out var index))
                {
                    return;
                }
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = e{index};");
            }

            void ICustomVisit<int>.CustomVisit(int value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {value};");
            }

            void ICustomVisit<float>.CustomVisit(float value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            void ICustomVisit<double>.CustomVisit(double value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            void ICustomVisit<bool>.CustomVisit(bool value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {(value ? "true" : "false")};");
            }

            void ICustomVisit<char>.CustomVisit(char value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {EscapeJsString(string.Empty + value)};");
            }

            bool IExcludeVisit<string>.ExcludeVisit(string value)
            {
                return string.IsNullOrEmpty(value);
            }

            void ICustomVisit<string>.CustomVisit(string value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {EscapeJsString(value)};");
            }

            void ICustomVisit<Texture2D>.CustomVisit(Texture2D value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<Sprite>.CustomVisit(Sprite value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<TileBase>.CustomVisit(TileBase value)
            {
                if (value is Tile)
                {
                    VisitObjectEntity(value);
                }
                else
                {
                    var tinyTileData = new TinyTileData(Struct);
                    if (TinyAssetEntityGroupGenerator.Context.TileDataToEntityMap.TryGetValue(tinyTileData, out var entity))
                    {
                        if (VisitorContext.EntityIndexMap.TryGetValue(entity.Ref, out var entityIndex))
                        {
                            VisitorContext.Writer.Line($"{Path}.{Property.Name} = e{entityIndex};");
                        }
                        else
                        {
                            VisitorContext.Writer.Line($"{Path}.{Property.Name} = ut.EntityLookupCache.getByName(world, '{entity.Name}');");
                        }
                    }
                }
            }

            void ICustomVisit<Tilemap>.CustomVisit(Tilemap value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<AudioClip>.CustomVisit(AudioClip value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<AnimationClip>.CustomVisit(AnimationClip value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<TMPro.TMP_FontAsset>.CustomVisit(TMPro.TMP_FontAsset value)
            {
                VisitObjectEntity(value);
            }

            private void VisitObjectEntity<TValue>(TValue value) where TValue : UnityEngine.Object
            {
                var assetName = TinyAssetExporter.GetAssetName(VisitorContext.Module, value);

                // HACK
                if (TinyAssetEntityGroupGenerator.ObjectToEntityMap.TryGetValue(value, out var assetEntity) && VisitorContext.EntityIndexMap.TryGetValue(assetEntity.Ref, out var assetEntityIndex))
                {
                    VisitorContext.Writer.Line($"{Path}.{Property.Name} = e{assetEntityIndex};");
                }
                else
                {
                    VisitorContext.Writer.Line($"{Path}.{Property.Name} = ut.EntityLookupCache.getByName(world, '{TinyAssetEntityGroupGenerator.GetAssetEntityPath(value)}{assetName}');");
                }
            }
        }

        private class ListVisitor : PropertyVisitor,
            IExcludeVisit<TinyObject>,
            ICustomVisit<TinyObject>,
            IExcludeVisit<TinyList>,
            ICustomVisit<TinyList>,
            IExcludeVisit<TinyEnum.Reference>,
            ICustomVisit<TinyEnum.Reference>,
            IExcludeVisit<TinyEntity.Reference>,
            ICustomVisit<TinyEntity.Reference>,
            ICustomVisit<int>,
            IExcludeVisit<float>,
            ICustomVisit<float>,
            IExcludeVisit<double>,
            ICustomVisit<double>,
            ICustomVisit<bool>,
            IExcludeVisit<char>,
            ICustomVisit<char>,
            IExcludeVisit<string>,
            ICustomVisit<string>,
            ICustomVisit<Texture2D>,
            ICustomVisit<Sprite>,
            ICustomVisit<TileBase>,
            ICustomVisit<Tilemap>,
            ICustomVisit<AudioClip>,
            ICustomVisit<AnimationClip>,
            ICustomVisit<TMPro.TMP_FontAsset>
        {
            public VisitorContext VisitorContext { private get; set; }
            public string Path { private get; set; }

            public override bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            {
                if (context.Property is ITinyValueProperty p && p.IsEditorOnly)
                {
                    return true;
                }
                return base.ExcludeVisit(container, context);
            }

            protected override bool ExcludeVisit<TValue>(TValue value)
            {
                return Equals(value, default(TValue));
            }

            protected override void Visit<TValue>(TValue value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {value.ToString()};");
            }

            bool IExcludeVisit<TinyObject>.ExcludeVisit(TinyObject value)
            {
                return value.Properties.PropertyBag.PropertyCount == 0;
            }

            void ICustomVisit<TinyObject>.CustomVisit(TinyObject value)
            {
                if (!IsListItem)
                {
                    return;
                }

                var type = value.Type.Dereference(value.Registry);
                AbortIfUnknownType(type, value, Property);
                if (type.ExportFlags.HasFlag(TinyExportFlags.EditorExtension))
                {
                    var exportedName = TinyEditorExtensionsGenerator.GetExportedAssetName(value);
                    VisitorContext.Writer.Line(
                        $"{Path}[{ListIndex}] = ut.EntityLookupCache.getByName(world, '{exportedName}');");
                }
                else
                {
                    var index = VisitorContext.StructIndexMap.GetOrAddValue(value);
                    VisitorContext.Writer.Line($"var s{index} = new {TinyScriptUtility.GetJsTypeName(type)}();");
                    value.Properties.Visit(new StructVisitor { VisitorContext = VisitorContext, Path = $"s{index}", Struct = value });
                    VisitorContext.Writer.Line($"{Path}[{ListIndex}] = s{index};");
                }
            }

            bool IExcludeVisit<TinyList>.ExcludeVisit(TinyList value)
            {
                return !IsListItem;
            }

            void ICustomVisit<TinyList>.CustomVisit(TinyList value)
            {
                throw new NotImplementedException();
            }

            public bool ExcludeVisit(TinyEnum.Reference value)
            {
                return value.Type.Equals(TinyType.Reference.None)
                       || value.Equals(TinyEnum.Reference.None)
                       || value.Id == default;
            }

            void ICustomVisit<TinyEnum.Reference>.CustomVisit(TinyEnum.Reference value)
            {
                if (!IsListItem)
                {
                    return;
                }

                var type = value.Type.Dereference(VisitorContext.Registry);
                AbortIfUnknownType(type, value, Property);
                var normalized = ExportEnumAsValue
                    ? (type.DefaultValue as TinyObject)?[value.Name]
                    : $"{TinyScriptUtility.GetJsTypeName(type)}.{value.Name}";
                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {normalized};");
            }

            bool IExcludeVisit<TinyEntity.Reference>.ExcludeVisit(TinyEntity.Reference value)
            {
                return value.Equals(TinyEntity.Reference.None);
            }

            void ICustomVisit<TinyEntity.Reference>.CustomVisit(TinyEntity.Reference value)
            {
                if (!IsListItem)
                {
                    return;
                }

                int index;
                if (!VisitorContext.EntityIndexMap.TryGetValue(value, out index))
                {
                    return;
                }
                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = e{index};");
            }

            void ICustomVisit<int>.CustomVisit(int value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {value};");
            }

            bool IExcludeVisit<float>.ExcludeVisit(float value)
            {
                return Math.Abs(value) <= float.Epsilon;
            }

            void ICustomVisit<float>.CustomVisit(float value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            bool IExcludeVisit<double>.ExcludeVisit(double value)
            {
                return Math.Abs(value) <= double.Epsilon;
            }

            void ICustomVisit<double>.CustomVisit(double value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            void ICustomVisit<bool>.CustomVisit(bool value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {(value ? "true" : "false")};");
            }

            bool IExcludeVisit<char>.ExcludeVisit(char value)
            {
                return false;
            }

            void ICustomVisit<char>.CustomVisit(char value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {EscapeJsString(string.Empty + value)};");
            }

            bool IExcludeVisit<string>.ExcludeVisit(string value)
            {
                return string.IsNullOrEmpty(value);
            }

            void ICustomVisit<string>.CustomVisit(string value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {EscapeJsString(value)};");
            }

            void ICustomVisit<Texture2D>.CustomVisit(Texture2D value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<Sprite>.CustomVisit(Sprite value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<TileBase>.CustomVisit(TileBase value)
            {
                if (value is Tile)
                {
                    VisitObjectEntity(value);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            void ICustomVisit<Tilemap>.CustomVisit(Tilemap value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<AudioClip>.CustomVisit(AudioClip value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<AnimationClip>.CustomVisit(AnimationClip value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<TMPro.TMP_FontAsset>.CustomVisit(TMPro.TMP_FontAsset value)
            {
                VisitObjectEntity(value);
            }

            private void VisitObjectEntity<TValue>(TValue value) where TValue : UnityEngine.Object
            {
                var assetName = TinyAssetExporter.GetAssetName(VisitorContext.Module, value);

                // HACK
                if (TinyAssetEntityGroupGenerator.ObjectToEntityMap.TryGetValue(value, out var assetEntity) && VisitorContext.EntityIndexMap.TryGetValue(assetEntity.Ref, out var assetEntityIndex))
                {
                    VisitorContext.Writer.Line($"{Path}[{ListIndex}] = e{assetEntityIndex};");
                }
                else
                {
                    VisitorContext.Writer.Line($"{Path}[{ListIndex}] = ut.EntityLookupCache.getByName(world, '{TinyAssetEntityGroupGenerator.GetAssetEntityPath(value)}{assetName}');");
                }
            }
        }
    }
}

