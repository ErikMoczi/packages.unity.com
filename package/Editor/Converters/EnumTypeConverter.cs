

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

namespace Unity.Tiny
{
    internal abstract class EnumTypeConverter<TEnum> : IConverterTo<TEnum>, IConverterFrom<TEnum>
        where TEnum : struct, IConvertible
    {
        private static readonly Dictionary<TEnum, int> k_ConvertedValueFrom = new Dictionary<TEnum, int>();
        private static readonly Dictionary<int, TEnum> k_ConvertedValueTo = new Dictionary<int, TEnum>();

        public EnumTypeConverter()
        {
            PopulateConversionValues();
        }

        public TinyObject ConvertFrom(TinyObject obj, TEnum value)
        {
            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: Cannot convert an enum from a TinyObject.");
        }

        public object ConvertFrom(object obj, TEnum value, IRegistry registry)
        {
            var enumRef = (TinyEnum.Reference)obj;
            ValidateType(enumRef, registry);

            var enumType = enumRef.Type.Dereference(registry);

            var defaultValue = enumType.DefaultValue as TinyObject;
            var defaultContainer = defaultValue.Properties as IPropertyContainer;
            
            // Try to match by name.
            var prop = defaultContainer.PropertyBag.FindProperty(value.ToString());
            if (null == prop)
            {
                // Or by value.
                foreach(var property in defaultContainer.PropertyBag.Properties)
                {
                    int intValue;
                    if (k_ConvertedValueFrom.TryGetValue(value, out intValue) && (int) (property as IValueProperty)?.GetObjectValue(defaultContainer) == intValue)
                    {
                        prop = property;
                        break;
                    }
                }
            }

            if (null == prop)
            {
                throw new InvalidOperationException($"{TinyConstants.ApplicationName}: Could not convert from type '{typeof(TEnum).Name}', no mapping from value {value} found.");
            }
            obj = new TinyEnum.Reference(enumType, (int)(prop as IValueProperty)?.GetObjectValue(defaultContainer));
            return obj;
        }

        public TEnum ConvertTo(object obj, IRegistry registry)
        {
            var enumRef = (TinyEnum.Reference)obj;
            ValidateType(enumRef, registry);
            TEnum value;
            if (k_ConvertedValueTo.TryGetValue(enumRef.Value, out value))
            {
                return (TEnum)(object)value;
            }
            throw new InvalidOperationException($"{TinyConstants.ApplicationName}: Could not convert to type '{typeof(TEnum).Name}', no mapping to value {enumRef.Value} found.");
        }

        protected virtual void PopulateConversionValues()
        {
            foreach(var value in EnumUtility.EnumValues<TEnum>())
            {
                Remap(value, (int)(object)value);
            }
        }

        protected void Remap(TEnum enumValue, int intValue)
        {
            k_ConvertedValueFrom[enumValue] = intValue;
            k_ConvertedValueTo[intValue] = enumValue;
        }

        private void ValidateType(TinyEnum.Reference reference, IRegistry registry)
        {
            ValidateTypeEnumType();
            
            if (null == reference.Type.Dereference(registry))
            {
                throw new ArgumentNullException(nameof(reference));
            }
        }

        private void ValidateTypeEnumType()
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new InvalidOperationException($"{TinyConstants.ApplicationName}: TEnum must be an Enum type.");
            }
        }
    }

    [UsedImplicitly]
    internal class CameraClearFlagsConverter : EnumTypeConverter<CameraClearFlags>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<CameraClearFlags>(TypeRefs.Core2D.CameraClearFlags);

        protected override void PopulateConversionValues()
        {
            Remap(CameraClearFlags.Depth, 0);
            Remap(CameraClearFlags.Nothing, 0);
            Remap(CameraClearFlags.Skybox, 1);
            Remap(CameraClearFlags.Color, 1);
            Remap(CameraClearFlags.SolidColor, 1);
        }
    }

    [UsedImplicitly]
    internal class DrawModeConverter : EnumTypeConverter<DrawMode>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<DrawMode>(TypeRefs.Core2D.DrawMode);
    }

    [UsedImplicitly]
    internal class TextAnchorConverter : EnumTypeConverter<TextAnchor>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TextAnchor>(TypeRefs.TextJS.TextAnchor);
    }

    [UsedImplicitly]
    internal class ScaleModeConverter : EnumTypeConverter<CanvasScaler.ScaleMode>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<CanvasScaler.ScaleMode>(TypeRefs.UILayout.UIScaleMode);

        protected override void PopulateConversionValues()
        {
            Remap(CanvasScaler.ScaleMode.ConstantPhysicalSize, 0);
            Remap(CanvasScaler.ScaleMode.ConstantPixelSize, 0);
            Remap(CanvasScaler.ScaleMode.ScaleWithScreenSize, 1);
        }
    }

    [UsedImplicitly]
    internal class RenderModeConverter : EnumTypeConverter<RenderingMode>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<RenderingMode>(TypeRefs.Core2D.RenderMode);
    }

    [UsedImplicitly]
    internal class TileColliderTypeConverter : EnumTypeConverter<Tile.ColliderType>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Tile.ColliderType>(TypeRefs.Tilemap2D.TileColliderType);
    }
    
    [UsedImplicitly]
    internal class WeightedModeConverter : EnumTypeConverter<WeightedMode>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<RenderingMode>(TypeRefs.TinyEditorExtensions.WeightedMode);
    }
    
    [UsedImplicitly]
    internal class WrapModeConverter : EnumTypeConverter<WrapMode>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<RenderingMode>(TypeRefs.TinyEditorExtensions.WrapMode);
    }
    
    [UsedImplicitly]
    internal class GradientModeConverter : EnumTypeConverter<GradientMode>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<RenderingMode>(TypeRefs.TinyEditorExtensions.GradientMode);
    }
}

