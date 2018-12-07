

using System;

using UnityEngine;


namespace Unity.Tiny
{
    internal class Vector3Converter : IConverterTo<Vector3>, IConverterFrom<Vector3>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Vector3>(TypeRefs.Math.Vector3);

        public Vector3 ConvertTo(object @object, IRegistry registry)
        {
            return ConvertTo(@object as TinyObject);
        }

        private Vector3 ConvertTo(TinyObject @object)
        {
            ValidateType(@object);
            return new Vector3(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y"),
                @object.GetProperty<float>("z")
            );
        }

        public TinyObject ConvertFrom(TinyObject @object, Vector3 vec3)
        {
            ValidateType(@object);
            @object["x"] = vec3.x;
            @object["y"] = vec3.y;
            @object["z"] = vec3.z;
            return @object;
        }

        public object ConvertFrom(object @object, Vector3 value, IRegistry registry)
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

            if (!@object.Type.Equals(TypeRefs.Math.Vector3))
            {
                throw new InvalidOperationException("Cannot convert value to or from Vector3");
            }
        }
    }
}

