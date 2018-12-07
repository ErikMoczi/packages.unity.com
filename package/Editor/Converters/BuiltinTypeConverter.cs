

using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unity.Tiny
{
    internal abstract class BuiltinTypeConverter<TValue> : IConverterTo<TValue>, IConverterFrom<TValue>
    {
        public TinyObject ConvertFrom(TinyObject obj, TValue vec2)
        {
            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: Cannot convert {typeof(TValue).Name} into a TinyObject.");
        }

        public object ConvertFrom(object obj, TValue value, IRegistry registry)
        {
            obj = value;
            return obj;
        }

        public TValue ConvertTo(object obj, IRegistry registry)
        {
            if (obj == null)
            {
                return default(TValue);
            }
            
            if (!typeof(TValue).IsAssignableFrom(obj.GetType()))
            {
                throw new InvalidOperationException($"{TinyConstants.ApplicationName}: Cannot convert from {obj.GetType().Name} to {typeof(TValue).Name}.");
            }
            return (TValue)obj;
        }
    }


    internal class BoolConverter : BuiltinTypeConverter<bool>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<bool>(TinyType.Boolean.Ref);
    }

    internal class SByteConverter : BuiltinTypeConverter<sbyte>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<sbyte>(TinyType.Int8.Ref);
    }

    internal class ShortConverter : BuiltinTypeConverter<short>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<short>(TinyType.Int16.Ref);
    }

    internal class IntConverter : BuiltinTypeConverter<int>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<int>(TinyType.Int32.Ref);
    }

    internal class LongConverter : BuiltinTypeConverter<long>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<long>(TinyType.Int64.Ref);
    }

    internal class ByteConverter : BuiltinTypeConverter<byte>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<byte>(TinyType.UInt8.Ref);
    }

    internal class UShortConverter : BuiltinTypeConverter<ushort>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<ushort>(TinyType.UInt16.Ref);
    }

    internal class UIntConverter : BuiltinTypeConverter<uint>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<uint>(TinyType.UInt32.Ref);
    }

    internal class ULongConverter : BuiltinTypeConverter<ulong>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<ulong>(TinyType.UInt64.Ref);
    }

    internal class FloatConverter : BuiltinTypeConverter<float>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<float>(TinyType.Float32.Ref);
    }

    internal class DoubleConverter : BuiltinTypeConverter<double>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<double>(TinyType.Float64.Ref);
    }

    internal class StringConverter : BuiltinTypeConverter<string>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<string>(TinyType.String.Ref);
    }

    internal class Texture2DConverter : BuiltinTypeConverter<Texture2D>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Texture2D>(TinyType.Texture2DEntity.Ref);
    }

    internal class AudioClipConverter : BuiltinTypeConverter<AudioClip>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<AudioClip>(TinyType.AudioClipEntity.Ref);
    }

    internal class SpriteConverter : BuiltinTypeConverter<Sprite>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Sprite>(TinyType.SpriteEntity.Ref);
    }

    internal class TileConverter : BuiltinTypeConverter<Tile>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Tile>(TinyType.TileEntity.Ref);
    }

    internal class TilemapConverter : BuiltinTypeConverter<Tilemap>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Tilemap>(TinyType.TilemapEntity.Ref);
    }

    internal class FontConverter : BuiltinTypeConverter<Font>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Font>(TinyType.FontEntity.Ref);
    }

    internal class EntityRefConverter : BuiltinTypeConverter<TinyEntity.Reference>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyEntity.Reference>(TinyType.EntityReference.Ref);
    }

    internal class AnimationClipConverter : BuiltinTypeConverter<AnimationClip>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<AnimationClip>(TinyType.AnimationClipEntity.Ref);
    }

    internal class EnumRefConverter : BuiltinTypeConverter<TinyEnum.Reference>
    {
    }
}

