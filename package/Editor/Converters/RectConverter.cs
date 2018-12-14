

using System;

using UnityEngine;


namespace Unity.Tiny
{
    internal class RectConverter : IConverterTo<Rect>, IConverterFrom<Rect>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Rect>(TypeRefs.Math.Rect);

        public Rect ConvertTo(object @object, IRegistry registry)
        {
            return ConvertTo(@object as TinyObject);
        }

        private Rect ConvertTo(TinyObject @object)
        {
            ValidateType(@object);
            return new Rect(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y"),
                @object.GetProperty<float>("width"),
                @object.GetProperty<float>("height")
            );
        }

        public TinyObject ConvertFrom(TinyObject @object, Rect rect)
        {
            ValidateType(@object);
            @object["x"] = rect.x;
            @object["y"] = rect.y;
            @object["width"] = rect.width;
            @object["height"] = rect.height;
            return @object;
        }
        public object ConvertFrom(object @object, Rect value, IRegistry registry)
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

            if (!@object.Type.Equals(TypeRefs.Math.Rect))
            {
                throw new InvalidOperationException("Cannot convert value to or from Rect");
            }
        }

    }
}

