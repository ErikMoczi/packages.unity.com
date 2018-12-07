using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Properties;
using Unity.Properties.Serialization;
using Unity.Tiny.Attributes;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Unity.Tiny.Serialization.Json
{
    /// <summary>
    /// Writes objects as JSON to a stream
    /// </summary>
    internal static class JsonBackEnd
    {
        private static PropertyVisitor Visitor => new PropertyVisitor();

        /// <summary>
        /// Writes the given property container to a JSON string and any asset references to the given storage
        /// </summary>
        /// <param name="container">Generic property container</param>
        /// <returns>JSON stringified object</returns>
        public static string Persist(IPropertyContainer container)
        {
            return JsonPropertyContainerWriter.Write(container, Visitor);
        }

        /// <summary>
        /// Writes the given property containers to a JSON string and any asset references to the given storage
        /// @NOTE return string is in the format "[{..}, {..}]"
        /// </summary>
        /// <param name="objects">Generic property containers to write</param>
        /// <returns>JSON stringified objects</returns>
        public static string Persist(params IPropertyContainer[] objects)
        {
            var sb = new StringBuilder();
            sb.Append("[");

            var first = true;

            foreach (var obj in objects)
            {
                if (!first)
                {
                    sb.Append(",\n");
                }
                else
                {
                    first = false;
                }

                sb.Append(JsonPropertyContainerWriter.Write(obj, Visitor));
            }

            sb.Append("]");
            return sb.ToString();
        }

        public static void Persist(string path, IEnumerable<IPropertyContainer> objects)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                Persist(writer, objects);
            }
        }

        public static void Persist(Stream output, params IPropertyContainer[] objects)
        {
            Persist(output, (IEnumerable<IPropertyContainer>) objects);
        }

        public static void Persist(Stream output, IEnumerable<IPropertyContainer> objects)
        {
            using (var writer = new StreamWriter(output, Encoding.UTF8, 1024, true))
            {
                Persist(writer, objects);
            }
        }

        /// <summary>
        /// NOTE: This method is not optimal. It must type check and cast objects
        /// @TODO Write specialized methods for our use cases
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="objects">Objects to pack</param>
        private static void Persist(TextWriter writer, IEnumerable<IPropertyContainer> objects)
        {
            writer.Write("[");

            var first = true;

            foreach (var obj in objects)
            {
                if (null == obj)
                {
                    continue;
                }

                if (!first)
                {
                    writer.Write(",\n");
                }
                else
                {
                    first = false;
                }

                (obj as IRegistryObject)?.Refresh();

                switch (obj)
                {
                    case TinyProject project:
                        writer.Write(JsonPropertyContainerWriter.Write(project, Visitor));
                        break;
                    case TinyModule module:
                        writer.Write(JsonPropertyContainerWriter.Write(module, Visitor));
                        break;
                    case TinyType type:
                        writer.Write(JsonPropertyContainerWriter.Write(type, Visitor));
                        break;
                    case TinyEntityGroup entityGroup:
                        writer.Write(JsonPropertyContainerWriter.Write(entityGroup, Visitor));
                        break;
                    case TinyEntity entity:
                        writer.Write(JsonPropertyContainerWriter.Write(entity, Visitor));
                        break;
                    case TinyObject component:
                        writer.Write(JsonPropertyContainerWriter.Write(component, Visitor));
                        break;
                    case TinyPrefabInstance prefabInstance:
                        writer.Write(JsonPropertyContainerWriter.Write(prefabInstance, Visitor));
                        break;
                    default:
                        writer.Write(JsonPropertyContainerWriter.Write(obj, Visitor));
                        break;
                }
            }

            writer.Write("]");
        }

        private class PropertyVisitor : JsonPropertyVisitor,
            ICustomVisit<TinyTypeCode>,
            ICustomVisit<TinyModule.Reference>,
            ICustomVisit<TinyType.Reference>,
            ICustomVisit<TinyEntityGroup.Reference>,
            ICustomVisit<TinyEntity.Reference>,
            ICustomVisit<TinyPrefabInstance.Reference>,
            ICustomVisit<Object>,
            ICustomVisit<TextAsset>,
            ICustomVisit<Texture2D>,
            ICustomVisit<Sprite>,
            ICustomVisit<Tile>,
            ICustomVisit<Tilemap>,
            ICustomVisit<AudioClip>,
            ICustomVisit<AnimationClip>,
            ICustomVisit<Font>,
            IExcludeVisit<TinyObject>,
            IExcludeVisit<TinyObject.PropertiesContainer>,
            IExcludeVisit<TinyDocumentation>,
            IExcludeVisit<string>,
            IExcludeVisit<Object>
        {
            private void VisitReference<TReference>(TReference value)
                where TReference : IReference
            {
                if (value.Id == TinyId.Empty)
                {
                    return;
                }

                StringBuffer.Append(' ', Indent * Style.Space);

                if (IsListItem)
                {
                    StringBuffer.Append("{ \"Id\": \"");
                    StringBuffer.Append(value.Id);
                    StringBuffer.Append("\", \"Name\": \"");
                    StringBuffer.Append(value.Name);
                    StringBuffer.Append("\" },\n");
                }
                else
                {
                    StringBuffer.Append("\"");
                    StringBuffer.Append(Property.Name);
                    StringBuffer.Append("\": { \"Id\": \"");
                    StringBuffer.Append(value.Id);
                    StringBuffer.Append("\", \"Name\": \"");
                    StringBuffer.Append(value.Name);
                    StringBuffer.Append("\" },\n");
                }
            }

            private void VisitReference<TReference>(TReference value, int typeId)
                where TReference : IReference
            {
                if (value.Id == TinyId.Empty && !IsListItem)
                {
                    return;
                }

                StringBuffer.Append(' ', Indent * Style.Space);

                if (IsListItem)
                {
                    StringBuffer.Append("{ \"$TypeId\": ");
                    StringBuffer.Append(typeId);

                    if (value.Id != TinyId.Empty)
                    {
                        StringBuffer.Append(", \"Id\": \"");
                        StringBuffer.Append(value.Id);
                        StringBuffer.Append("\", \"Name\": \"");
                        StringBuffer.Append(value.Name);
                        StringBuffer.Append("\" },\n");
                    }
                    else
                    {
                        StringBuffer.Append(" },\n");
                    }
                }
                else
                {
                    StringBuffer.Append("\"");
                    StringBuffer.Append(Property.Name);
                    StringBuffer.Append("\": { \"$TypeId\": ");
                    StringBuffer.Append(typeId);

                    if (value.Id != TinyId.Empty)
                    {
                        StringBuffer.Append(", \"Id\": \"");
                        StringBuffer.Append(value.Id);
                        StringBuffer.Append("\", \"Name\": \"");
                        StringBuffer.Append(value.Name);
                        StringBuffer.Append("\" },\n");
                    }
                    else
                    {
                        StringBuffer.Append(" },\n");
                    }
                }
            }
            
            private void VisitObject(UnityEngine.Object value)
            {
                var handle = UnityObjectSerializer.ToObjectHandle(value);

                StringBuffer.Append(' ', Style.Space * Indent);

                if (false == IsListItem)
                {
                    StringBuffer.Append("\"");
                    StringBuffer.Append(Property.Name);
                    StringBuffer.Append("\": ");
                }

                if (string.IsNullOrEmpty(handle.Guid))
                {
                    StringBuffer.Append("{ \"$TypeId\": ");
                    StringBuffer.Append((int) TinyTypeId.UnityObject);
                    StringBuffer.Append(" },\n");
                }
                else
                {
                    StringBuffer.Append("{ \"$TypeId\": ");
                    StringBuffer.Append((int) TinyTypeId.UnityObject);
                    StringBuffer.Append(", \"Guid\": \"");
                    StringBuffer.Append(handle.Guid);
                    StringBuffer.Append("\", \"FileId\": ");
                    StringBuffer.Append(handle.FileId);
                    StringBuffer.Append(", \"Type\": ");
                    StringBuffer.Append(handle.Type);
                    StringBuffer.Append(" },\n");
                }
            }

            void ICustomVisit<TinyTypeCode>.CustomVisit(TinyTypeCode value)
            {
                // custom override to avoid the default Enum-to-Int32 serialization and use the name instead
                AppendPrimitive(Properties.Serialization.Json.EncodeJsonString(value.ToString()));
            }
            
            void ICustomVisit<TinyModule.Reference>.CustomVisit(TinyModule.Reference value)
            {
                VisitReference(value);
            }

            void ICustomVisit<TinyType.Reference>.CustomVisit(TinyType.Reference value)
            {
                VisitReference(value);
            }

            void ICustomVisit<TinyEntityGroup.Reference>.CustomVisit(TinyEntityGroup.Reference value)
            {
                VisitReference(value);
            }

            void ICustomVisit<TinyEntity.Reference>.CustomVisit(TinyEntity.Reference value)
            {
                VisitReference(value, (int) TinyTypeId.EntityReference);
            }

            void ICustomVisit<TinyPrefabInstance.Reference>.CustomVisit(TinyPrefabInstance.Reference value)
            {
                VisitReference(value, (int) TinyTypeId.PrefabInstanceReference);
            }

            void ICustomVisit<Object>.CustomVisit(Object value)
            {
                VisitObject(value);
            }

            void ICustomVisit<TextAsset>.CustomVisit(TextAsset value)
            {
                VisitObject(value);
            }

            void ICustomVisit<Texture2D>.CustomVisit(Texture2D value)
            {
                VisitObject(value);
            }

            void ICustomVisit<Sprite>.CustomVisit(Sprite value)
            {
                VisitObject(value);
            }

            void ICustomVisit<Tile>.CustomVisit(Tile value)
            {
                VisitObject(value);
            }

            void ICustomVisit<Tilemap>.CustomVisit(Tilemap value)
            {
                VisitObject(value);
            }

            void ICustomVisit<AudioClip>.CustomVisit(AudioClip value)
            {
                VisitObject(value);
            }
            

            void ICustomVisit<AnimationClip>.CustomVisit(AnimationClip value)
            {
                VisitObject(value);
            }

            void ICustomVisit<Font>.CustomVisit(Font value)
            {
                VisitObject(value);
            }

            private static bool IsSkipped<TValue>(TValue value, IPropertyContainer container, IProperty property)
            {
                if (SerializationContextUtility.NonSerializedInCurrentContext(value) ||
                    SerializationContextUtility.NonSerializedInCurrentContext(property)) 
                {
                    return true;
                }
                
                // special case for lists
                if (property is IListProperty listProperty)
                {
                    // skip empty lists
                    // always write list elements, we don't handle `IsOverridden` or default values for lists
                    return listProperty.Count(container) == 0;
                }
                
                // skip null containers
                // TODO: fix in property API
                if (typeof(IPropertyContainer).IsAssignableFrom(typeof(TValue)) && value == null)
                {
                    return true;
                }
                
                // skip default property values
                if (property is ITinyValueProperty valueProperty)
                {
                    return !valueProperty.IsOverridden(container);
                }
                
                // filter out fake nulls
                if (typeof(Object).IsAssignableFrom(typeof(TValue)))
                {
                    return (false == ((Object) (object) value));
                }
                
                return false;
            }

            public override bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            {
                return IsSkipped(context.Value, container, context.Property) || base.ExcludeVisit(container, context);
            }
            
            public override bool ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            {
                return IsSkipped(context.Value, container, context.Property) || base.ExcludeVisit(ref container, context);
            }

            bool IExcludeVisit<TinyObject>.ExcludeVisit(TinyObject value)
            {
                return false;
            }

            bool IExcludeVisit<TinyObject.PropertiesContainer>.ExcludeVisit(TinyObject.PropertiesContainer value)
            {
                return (false == value.IsOverridden);
            }

            bool IExcludeVisit<TinyDocumentation>.ExcludeVisit(TinyDocumentation value)
            {
                return string.IsNullOrEmpty(value.Summary);
            }

            bool IExcludeVisit<string>.ExcludeVisit(string value)
            {
                return string.IsNullOrEmpty(value);
            }

            bool IExcludeVisit<Object>.ExcludeVisit(Object value)
            {
                if (value == null || IsListItem)
                {
                    return false;
                }

                var instanceId = value.GetInstanceID();
                if (instanceId > 0)
                {
                    return false;
                }
                #if UNITY_EDITOR
                var isValid = UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instanceId, out string guid, out long localId);
                #else
                var isValid = true;
                #endif
                return false == isValid;
            }
        }
    }
}

