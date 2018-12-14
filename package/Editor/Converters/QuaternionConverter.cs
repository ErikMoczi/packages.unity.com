

using System;

using UnityEngine;


namespace Unity.Tiny
{
    internal class QuaternionConverter : IConverterTo<Quaternion>, IConverterFrom<Quaternion>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Quaternion>(TypeRefs.Math.Quaternion);

        public Quaternion ConvertTo(object @object, IRegistry registry)
        {
            return ConvertTo(@object as TinyObject);
        }

        private Quaternion ConvertTo(TinyObject @object)
        {
            ValidateType(@object);
            return new Quaternion(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y"),
                @object.GetProperty<float>("z"),
                @object.GetProperty<float>("w")
            );
        }

        public TinyObject ConvertFrom(TinyObject @object, Quaternion q)
        {
            ValidateType(@object);
            @object["x"] = q.x;
            @object["y"] = q.y;
            @object["z"] = q.z;
            @object["w"] = q.w;
            return @object;
        }

        public object ConvertFrom(object @object, Quaternion value, IRegistry registry)
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

            if (!@object.Type.Equals(TypeRefs.Math.Quaternion))
            {
                throw new InvalidOperationException("Cannot convert value to or from Quaternion");
            }
        }
    }
}

