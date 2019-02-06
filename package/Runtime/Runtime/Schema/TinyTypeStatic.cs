

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyType
    {
        public static TinyType NewComponentType { get; }
        public static TinyType Int8 { get; }
        public static TinyType Int16 { get; }
        public static TinyType Int32 { get; }
        public static TinyType Int64 { get; }
        public static TinyType UInt8 { get; }
        public static TinyType UInt16 { get; }
        public static TinyType UInt32 { get; }
        public static TinyType UInt64 { get; }
        public static TinyType Float32 { get; }
        public static TinyType Float64 { get; }
        public static TinyType Boolean { get; }
        public static TinyType String { get; }
        public static TinyType EntityReference { get; }
        
        // Asset entity reference type
        // These types will map to an entity in the runtime `world.getByName('assets/{assetType}/{assetName}')`
        public static TinyType Texture2DEntity { get; }
        public static TinyType SpriteEntity { get; }
        public static TinyType TileEntity { get; }
        public static TinyType TilemapEntity { get; }
        public static TinyType AudioClipEntity { get; }
        public static TinyType FontEntity { get; }
        public static TinyType AnimationClipEntity { get; }
        public static IList<TinyType> BuiltInTypes { get; }

        static TinyType()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
            
            NewComponentType = new TinyType(null, null) {Id = new TinyId("696b7de3df0f4887abca1fb6aa7f2615"), Name = "NewComponentType", TypeCode = TinyTypeCode.Component};

            // @NOTE: Primitives do not belong to a single registry and are shared across all registries
            Int8 = new TinyType(null, null) {Id = new TinyId("27c155635ccb4ab2bcb79ef5aaf129ec"), Name = "Int8", TypeCode = TinyTypeCode.Int8};
            Int16 = new TinyType(null, null) {Id = new TinyId("2aa56ce081e14e8a93d276da72d813bc"), Name = "Int16", TypeCode = TinyTypeCode.Int16};
            Int32 = new TinyType(null, null) {Id = new TinyId("9633c95a0a68473682f09ed6a01194b4"), Name = "Int32", TypeCode = TinyTypeCode.Int32};
            Int64 = new TinyType(null, null) {Id = new TinyId("37695933217a49f68ce15db33f63cdf9"), Name = "Int64", TypeCode = TinyTypeCode.Int64};
            UInt8 = new TinyType(null, null) {Id = new TinyId("7112767112f747e2a340404a5ceb31b5"), Name = "UInt8", TypeCode = TinyTypeCode.UInt8};
            UInt16 = new TinyType(null, null) {Id = new TinyId("86fa32ad22614762afdacbf4dba8180f"), Name = "UInt16", TypeCode = TinyTypeCode.UInt16};
            UInt32 = new TinyType(null, null) {Id = new TinyId("1da58c8ba95a4c85a2b5920bd0663f70"), Name = "UInt32", TypeCode = TinyTypeCode.UInt32};
            UInt64 = new TinyType(null, null) {Id = new TinyId("574059163cda44b3ade6ea7b2daf67f2"), Name = "UInt64", TypeCode = TinyTypeCode.UInt64};
            Float32 = new TinyType(null, null) {Id = new TinyId("67325dccf2f047c19c7ef4a045354e67"), Name = "Float32", TypeCode = TinyTypeCode.Float32};
            Float64 = new TinyType(null, null) {Id = new TinyId("74cf32c2744342b7903871f8feb2fdd7"), Name = "Float64", TypeCode = TinyTypeCode.Float64};
            Boolean = new TinyType(null, null) {Id = new TinyId("2b477f505af74487b7092b5617d88d3f"), Name = "Boolean", TypeCode = TinyTypeCode.Boolean};
            String = new TinyType(null, null) {Id = new TinyId("1bff5adddd7c41de98d3329c7c641208"), Name = "String", TypeCode = TinyTypeCode.String};
            EntityReference = new TinyType(null, null) {Id = new TinyId("5a182d9d039d4dfd8fa96132d05f9ee7"), Name = "EntityReference", TypeCode = TinyTypeCode.EntityReference};
            
            // Asset entity reference types
            Texture2DEntity = new TinyType(null, null) {Id = new TinyId("373ed9034ede4f84829bf01ed265f6ee"), Name = "Texture2DEntity", TypeCode = TinyTypeCode.UnityObject};
            SpriteEntity = new TinyType(null, null) {Id = new TinyId("cf54a635a25248ab87f2563bb840ed5b"), Name = "SpriteEntity", TypeCode = TinyTypeCode.UnityObject};
            TileEntity = new TinyType(null, null) { Id = new TinyId("ba863be1346c460f80e3e371b0cf255b"), Name = "TileEntity", TypeCode = TinyTypeCode.UnityObject };
            TilemapEntity = new TinyType(null, null) { Id = new TinyId("fa36582cc6d14b179d858ce3744c9419"), Name = "TilemapEntity", TypeCode = TinyTypeCode.UnityObject };
            AudioClipEntity = new TinyType(null, null) {Id = new TinyId("1ae8c073dc444f4fb2d3120e5e618326"), Name = "AudioClipEntity", TypeCode = TinyTypeCode.UnityObject};
            FontEntity = new TinyType(null, null) {Id = new TinyId("4b1f918c1c564e42a04a0cb8f4ee0665"), Name = "FontEntity", TypeCode = TinyTypeCode.UnityObject};
            AnimationClipEntity = new TinyType(null, null) { Id = new TinyId("631f4285c8ca41eeb480f7fbff0756bb"), Name = "AnimationClipEntity", TypeCode = TinyTypeCode.UnityObject };

            BuiltInTypes = new List<TinyType>
            {
                Int8, Int16, Int32, Int64,
                UInt8, UInt16, UInt32, UInt64,
                Float32, Float64, 
                Boolean, String,
                EntityReference,
                Texture2DEntity, SpriteEntity, TileEntity, TilemapEntity, AudioClipEntity, AnimationClipEntity, FontEntity
            };
        }

        /// <summary>
        /// Returns the built in type based on the given typeCode
        /// </summary>
        public static TinyType GetType(TinyTypeCode typeCode)
        {
            switch (typeCode)
            {
                case TinyTypeCode.Int8:
                    return Int8;
                case TinyTypeCode.Int16:
                    return Int16;
                case TinyTypeCode.Int32:
                    return Int32;
                case TinyTypeCode.Int64:
                    return Int64;
                case TinyTypeCode.UInt8:
                    return UInt8;
                case TinyTypeCode.UInt16:
                    return UInt16;
                case TinyTypeCode.UInt32:
                    return UInt32;
                case TinyTypeCode.UInt64:
                    return UInt64;
                case TinyTypeCode.Float32:
                    return Float32;
                case TinyTypeCode.Float64:
                    return Float64;
                case TinyTypeCode.Boolean:
                    return Boolean;
                case TinyTypeCode.String:
                    return String;
                case TinyTypeCode.EntityReference:
                    return EntityReference;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Resolves a built-in Tiny type by name or known alias.
        /// </summary>
        /// <param name="typeName">Name of the Tiny type. For example: "sbyte" and "Int8" will both resolve to the Int8 Tiny type.</param>
        /// <returns>The resolved Tiny type if found, null otherwise.</returns>
        public static TinyType GetType(string typeName)
        {
            switch (typeName)
            {
                case "sbyte":
                case "Int8": return Int8;
                case "byte":
                case "UInt8": return UInt8;
                case "short":
                case "Int16": return Int16;
                case "ushort":
                case "UInt16": return UInt16;
                case "int":
                case "Int32": return Int32;
                case "uint":
                case "UInt32": return UInt32;
                case "long":
                case "Int64": return Int64;
                case "ulong":
                case "UInt64": return UInt64;
                case "float":
                case "Single":
                case "Float32": return Float32;
                case "double":
                case "Double":
                case "Float64": return Float64;
                case "bool":
                case "Boolean": return Boolean;
                case "string":
                case "String": return String;
                case "EntityReference": return EntityReference;
                case "Texture2DEntity": return Texture2DEntity;
                case "SpriteEntity": return SpriteEntity;
                case "AudioClipEntity": return AudioClipEntity;
                case "AnimationClipEntity": return AnimationClipEntity;
                case "FontEntity": return FontEntity;
                default: return null;
            }
        }

        public static string GetTypeName(Type type)
        {
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(short)) return "short";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(int)) return "int";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            return null;
        }

        public static Reference GetTypeReference(TinyTypeCode typeCode)
        {
            var type = GetType(typeCode);
            
            if (null == type)
            {
                return new Reference();
            }
            
            return (Reference) type;
        }
        
        /// <summary>
        /// Determines if the provided System.Type maps to a built-in TinyType.
        /// </summary>
        public static bool TryGetType(Type type, out TinyType tinyType)
        {
            tinyType = GetType(type);
            return null != tinyType;
        }

        /// <summary>
        /// Fast access to built in types
        /// </summary>
        public static TinyType GetType(Type type)
        {
            if (type.IsEnum)
            {
                return null;
            }
            
            var typeCode = Type.GetTypeCode(type);
            
            switch (typeCode)
            {
                case System.TypeCode.SByte:
                    return Int8;
                case System.TypeCode.Int16:
                    return Int16;
                case System.TypeCode.Int32:
                    return Int32;
                case System.TypeCode.Int64:
                    return Int64;
                case System.TypeCode.Byte:
                    return UInt8;
                case System.TypeCode.UInt16:
                    return UInt16;
                case System.TypeCode.UInt32:
                    return UInt32;
                case System.TypeCode.UInt64:
                    return UInt64;
                case System.TypeCode.Single:
                    return Float32;
                case System.TypeCode.Double:
                    return Float64;
                case System.TypeCode.Boolean:
                    return Boolean;
                case System.TypeCode.String:
                    return String;
            }

            if (typeof(UnityEngine.Texture2D).IsAssignableFrom(type))
            {
                return Texture2DEntity;
            }
            
            if (typeof(UnityEngine.Sprite).IsAssignableFrom(type))
            {
                return SpriteEntity;
            }

            if (typeof(UnityEngine.Tilemaps.TileBase).IsAssignableFrom(type))
            {
                return TileEntity;
            }

            if (typeof(UnityEngine.Tilemaps.Tilemap).IsAssignableFrom(type))
            {
                return TilemapEntity;
            }

            if (typeof(UnityEngine.AudioClip).IsAssignableFrom(type))
            {
                return AudioClipEntity;
            }

            if (typeof(UnityEngine.AnimationClip).IsAssignableFrom(type))
            {
                return AnimationClipEntity;
            }

            if (typeof(TMPro.TMP_FontAsset).IsAssignableFrom(type))
            {
                return FontEntity;
            }

            return null;
        }
        /// <summary>
        /// Creates a runtime instance of the given type
        /// 
        /// NOTE: For primitives this returns the default .NET value
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object CreateInstance(TinyType type)
        {            
            var typeCode = type.TypeCode;

            switch (typeCode)
            {
                case TinyTypeCode.Int8:
                    return default(sbyte);
                case TinyTypeCode.Int16:
                    return default(short);
                case TinyTypeCode.Int32:
                    return default(int);
                case TinyTypeCode.Int64:
                    return default(long);
                case TinyTypeCode.UInt8:
                    return default(byte);
                case TinyTypeCode.UInt16:
                    return default(ushort);
                case TinyTypeCode.UInt32:
                    return default(uint);
                case TinyTypeCode.UInt64:
                    return default(ulong);
                case TinyTypeCode.Float32:
                    return default(float);
                case TinyTypeCode.Float64:
                    return default(double);
                case TinyTypeCode.Boolean:
                    return default(bool);
                case TinyTypeCode.String:
                    return string.Empty;
                case TinyTypeCode.Enum:
                    return new TinyEnum.Reference(type, type.Fields.FirstOrDefault()?.Id ?? TinyId.Empty);
                case TinyTypeCode.EntityReference:
                    return TinyEntity.Reference.None;
                default:
                    return null;
            }
        }
        
        public static IList CreateListInstance(TinyType type)
        {            
            var typeCode = type.TypeCode;

            switch (typeCode)
            {
                case TinyTypeCode.Int8:
                    return new List<sbyte>();
                case TinyTypeCode.Int16:
                    return new List<short>();
                case TinyTypeCode.Int32:
                    return new List<int>();
                case TinyTypeCode.Int64:
                    return new List<long>();
                case TinyTypeCode.UInt8:
                    return new List<byte>();
                case TinyTypeCode.UInt16:
                    return new List<ushort>();
                case TinyTypeCode.UInt32:
                    return new List<uint>();
                case TinyTypeCode.UInt64:
                    return new List<ulong>();
                case TinyTypeCode.Float32:
                    return new List<float>();
                case TinyTypeCode.Float64:
                    return new List<double>();
                case TinyTypeCode.Boolean:
                    return new List<bool>();
                case TinyTypeCode.String:
                    return new List<string>();
                case TinyTypeCode.Configuration:
                case TinyTypeCode.Component:
                case TinyTypeCode.Struct:
                    return new List<TinyObject>();
                case TinyTypeCode.Enum:
                    return new List<TinyEnum.Reference>();
                case TinyTypeCode.EntityReference:
                    return new List<TinyEntity.Reference>();
                case TinyTypeCode.UnityObject:
                    if (type.Id == Texture2DEntity.Id)
                    {
                        return new List<UnityEngine.Texture2D>();
                    }
                    else if (type.Id == SpriteEntity.Id)
                    {
                        return new List<UnityEngine.Sprite>();
                    }
                    else if (type.Id == TileEntity.Id)
                    {
                        return new List<UnityEngine.Tilemaps.TileBase>();
                    }
                    else if (type.Id == TilemapEntity.Id)
                    {
                        return new List<UnityEngine.Tilemaps.Tilemap>();
                    }
                    else if (type.Id == AudioClipEntity.Id)
                    {
                        return new List<UnityEngine.AudioClip>();
                    }
                    else if (type.Id == AnimationClipEntity.Id)
                    {
                        return new List<UnityEngine.AnimationClip>();
                    }
                    else if (type.Id == FontEntity.Id)
                    {
                        return new List<TMPro.TMP_FontAsset>();
                    }
                    else
                    {
                        return new List<UnityEngine.Object>();
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type.TypeCode), type.TypeCode, null);
            }
        }
    }
}

