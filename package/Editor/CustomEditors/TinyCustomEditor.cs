using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unity.Tiny
{
    internal abstract class TinyCustomEditor : ITinyCustomEditor
    {
        public TinyContext TinyContext { get; }
        public abstract bool Visit(ref UIVisitContext<TinyObject> context);

        protected TinyCustomEditor(TinyContext context)
        {
            TinyContext = context;
        }
        
        public static void VisitField(ref UIVisitContext<TinyObject> context, string name)
        {
            var tiny = context.Value;
            var visitor = context.Visitor;
            
            var registry = tiny.Registry;
            var field = tiny.Type.Dereference(registry).FindFieldByName(name);
            var fieldType = field.FieldType.Dereference(registry);
            var isArray = field.Array;
            
            if (isArray)
            {
                visitor.VisitList<TinyObject.PropertiesContainer, TinyList>(tiny.Properties, name);
                return;
            }

            switch (fieldType.TypeCode)
            {
                case TinyTypeCode.Unknown:
                    break;
                case TinyTypeCode.Int8:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, sbyte>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.Int16:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, short>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.Int32:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, int>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.Int64:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, long>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.UInt8:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, byte>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.UInt16:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, ushort>(
                        tiny.Properties, name);
                    break;
                case TinyTypeCode.UInt32:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, uint>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.UInt64:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, ulong>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.Float32:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, float>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.Float64:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, double>(
                        tiny.Properties, name);
                    break;
                case TinyTypeCode.Boolean:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, bool>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.String:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, string>(
                        tiny.Properties, name);
                    break;
                case TinyTypeCode.Component:
                    throw new InvalidOperationException("A field's default value cannot be of component type.");
                case TinyTypeCode.Struct:
                    visitor.VisitContainer<TinyObject.PropertiesContainer, TinyObject>(tiny.Properties,
                        name);
                    break;
                case TinyTypeCode.Enum:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, TinyEnum.Reference>(
                        tiny.Properties, name);
                    break;
                case TinyTypeCode.Configuration:
                    throw new InvalidOperationException("A field's default value cannot be of configuration type.");
                case TinyTypeCode.EntityReference:
                    visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, TinyEntity.Reference>(
                        tiny.Properties, name);
                    break;
                case TinyTypeCode.UnityObject:
                    if (fieldType == TinyType.Texture2DEntity)
                    {
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, Texture2D>(
                            tiny.Properties, name);
                    }
                    else if (fieldType == TinyType.SpriteEntity)
                    {
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, Sprite>(
                            tiny.Properties, name);
                    }
                    else if (fieldType == TinyType.FontEntity)
                    {
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, TMPro.TMP_FontAsset>(
                            tiny.Properties, name);
                    }
                    else if (fieldType == TinyType.AudioClipEntity)
                    {
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, AudioClip>(
                            tiny.Properties, name);
                    }
                    else if (fieldType == TinyType.AnimationClipEntity)
                    {
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, AnimationClip>(
                            tiny.Properties, name);
                    }
                    else if (fieldType == TinyType.TileEntity)
                    {
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, Tile>(
                            tiny.Properties, name);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}