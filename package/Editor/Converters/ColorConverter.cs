

using System;

using UnityEngine;


namespace Unity.Tiny
{
    internal class ColorConverter : IConverterTo<Color>, IConverterFrom<Color>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Color>(TypeRefs.Core2D.Color);

        public Color ConvertTo(object @object, IRegistry registry)
        {
            return ConvertTo(@object as TinyObject);
        }

        private Color ConvertTo(TinyObject @object)
        {
            ValidateType(@object);
            return new Color(
                @object.GetProperty<float>("r"),
                @object.GetProperty<float>("g"),
                @object.GetProperty<float>("b"),
                @object.GetProperty<float>("a")
            );
        }

        public TinyObject ConvertFrom(TinyObject @object, Color color)
        {
            ValidateType(@object);
            @object["r"] = color.r;
            @object["g"] = color.g;
            @object["b"] = color.b;
            @object["a"] = color.a;
            return @object;
        }
        public object ConvertFrom(object @object, Color value, IRegistry registry)
        {
            return ConvertFrom(@object as TinyObject, value);
        }

        private void ValidateType(TinyObject @object)
        {
            if (null == @object)
            {
                throw new ArgumentNullException("object");
            }

            if (null == @object.Registry)
            {
                throw new ArgumentNullException("registry");
            }

            if (!@object.Type.Equals(TypeRefs.Core2D.Color))
            {
                throw new InvalidOperationException("Cannot convert value to or from Color");
            }
        }

    }
}

