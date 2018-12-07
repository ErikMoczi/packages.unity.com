

using System;

using UnityEngine;


namespace Unity.Tiny
{
    internal class Vector4Converter : IConverterTo<Vector4>, IConverterFrom<Vector4>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Vector4>(TypeRefs.Math.Vector4);

        public Vector4 ConvertTo(object @object, IRegistry registry)
        {
            return ConvertTo(@object as TinyObject);
        }

        private Vector4 ConvertTo(TinyObject @object)
        {
            ValidateType(@object);
            return new Vector4(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y"),
                @object.GetProperty<float>("z"),
                @object.GetProperty<float>("w")
            );
        }

        public TinyObject ConvertFrom(TinyObject @object, Vector4 v)
        {
            ValidateType(@object);
            @object["x"] = v.x;
            @object["y"] = v.y;
            @object["z"] = v.z;
            @object["w"] = v.w;
            return @object;
        }

        public object ConvertFrom(object @object, Vector4 value, IRegistry registry)
        {
            return ConvertFrom(@object as TinyObject, value);
        }

        private void ValidateType(TinyObject @object)
        {
            if (null == @object)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            if (null == @object.Registry)
            {
                throw new ArgumentException("registry is null");
            }

            if (!@object.Type.Equals(TypeRefs.Math.Vector4))
            {
                throw new InvalidOperationException("Cannot convert value to or from Vector4");
            }
        }
    }
}

