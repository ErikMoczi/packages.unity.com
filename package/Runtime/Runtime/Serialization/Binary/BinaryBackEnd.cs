using System;
using System.Collections.Generic;
using System.IO;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Unity.Tiny.Serialization.Binary
{
    internal enum TinyBinaryToken : byte
    {
        Id = 0,
        ModuleReference = 1,
        TypeReference = 2,
        SceneReference = 3,
        EntityReference = 4,
        SystemReference = 6, // DEPRECATED - DO NOT USE
        UnityObject = 7,
        PrefabInstanceReference = 8,
    }

    /// <summary>
    /// Writes objects as binary to a stream
    /// </summary>
    internal static class BinaryBackEnd
    {
        private static readonly PropertyVisitor s_PropertyVisitor = new PropertyVisitor();

        public static void Persist(Stream output, params IPropertyContainer[] objects)
        {
            Persist(output, (IEnumerable<IPropertyContainer>)objects);
        }

        public static void Persist(string path, IEnumerable<IPropertyContainer> objects)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            {
                Persist(stream, objects);
            }
        }

        public static void Persist(Stream output, IEnumerable<IPropertyContainer> objects)
        {
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory))
            {
                writer.Write(BinaryToken.BeginArray);
                writer.Write((uint)0);
                foreach (var obj in objects)
                {
                    (obj as IRegistryObject)?.Refresh();
                    BinaryPropertyContainerWriter.Write(memory, obj, s_PropertyVisitor);
                }
                writer.Write(BinaryToken.EndArray);

                const int start = 5;
                var end = memory.Position;
                var size = end - start;
                memory.Position = start - sizeof(uint);
                writer.Write((uint)size);
                output.Write(memory.GetBuffer(), 0, (int)end);
            }
        }

        private class PropertyVisitor : BinaryPropertyVisitor,
            ICustomVisit<TinyId>,
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
            ICustomVisit<TMPro.TMP_FontAsset>,
            IExcludeVisit<TinyObject>,
            IExcludeVisit<TinyObject.PropertiesContainer>,
            IExcludeVisit<Object>
        {
            private void VisitReference<TReference>(TinyBinaryToken token, TReference value)
                where TReference : IReference
            {
                WriteValuePropertyHeader(TypeCode.Object);
                Writer.Write((byte)token);
                Writer.Write(value.Id.ToGuid());
                Writer.Write(value.Name ?? string.Empty);
            }

            private void VisitObject(Object value)
            {
                var handle = UnityObjectSerializer.ToObjectHandle(value);
                WriteValuePropertyHeader(TypeCode.Object);
                Writer.Write((byte)TinyBinaryToken.UnityObject);
                Writer.Write(handle.Guid ?? string.Empty);
                Writer.Write(handle.FileId);
                Writer.Write(handle.Type);
            }

            void ICustomVisit<TinyId>.CustomVisit(TinyId value)
            {
                WriteValuePropertyHeader(TypeCode.Object);
                Writer.Write((byte)TinyBinaryToken.Id);
                Writer.Write(value.ToGuid());
            }

            void ICustomVisit<TinyModule.Reference>.CustomVisit(TinyModule.Reference value)
            {
                VisitReference(TinyBinaryToken.ModuleReference, value);
            }

            void ICustomVisit<TinyType.Reference>.CustomVisit(TinyType.Reference value)
            {
                VisitReference(TinyBinaryToken.TypeReference, value);
            }

            void ICustomVisit<TinyEntityGroup.Reference>.CustomVisit(TinyEntityGroup.Reference value)
            {
                VisitReference(TinyBinaryToken.SceneReference, value);
            }

            void ICustomVisit<TinyEntity.Reference>.CustomVisit(TinyEntity.Reference value)
            {
                VisitReference(TinyBinaryToken.EntityReference, value);
            }
            
            void ICustomVisit<TinyPrefabInstance.Reference>.CustomVisit(TinyPrefabInstance.Reference value)
            {
                VisitReference(TinyBinaryToken.PrefabInstanceReference, value);
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

            void ICustomVisit<TMPro.TMP_FontAsset>.CustomVisit(TMPro.TMP_FontAsset value)
            {
                VisitObject(value);
            }

            private static bool IsSkipped<TValue>(TValue value, IPropertyContainer container, IProperty property)
            {
                if (property.GetAttribute<NonSerializedInContext>()?.Context == SerializationContext.CurrentContext)
                {
                    return true;
                }

                // skip empty lists
                if (property is IListProperty listProperty)
                {
                    return (listProperty.Count(container) == 0);
                }

                // skip null containers
                // TODO: fix in property API
                if (typeof(IPropertyContainer).IsAssignableFrom(typeof(TValue)) && value == null)
                {
                    return true;
                }

                // skip default values
                if (property is ITinyValueProperty valueProperty)
                {
                    if (!valueProperty.IsOverridden(container))
                    {
                        // skip the visit if the property is in its default value
                        return true;
                    }
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
                //return !IsListItem && (false == value.IsOverridden);
            }

            bool IExcludeVisit<TinyObject.PropertiesContainer>.ExcludeVisit(TinyObject.PropertiesContainer value)
            {
                return (false == value.IsOverridden);
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

