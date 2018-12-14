

using System;

using UnityEngine;


namespace Unity.Tiny
{
    internal class Vector2Converter : IConverterTo<Vector2>, IConverterFrom<Vector2>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Vector2>(TypeRefs.Math.Vector2);

        public Vector2 ConvertTo(object @object, IRegistry registry)
        {
            return ConvertTo(@object as TinyObject);
        }

        private Vector2 ConvertTo(TinyObject @object)
        {
            ValidateType(@object);
            return new Vector2(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y")
            );
        }

        public TinyObject ConvertFrom(TinyObject @object, Vector2 vec2)
        {
            ValidateType(@object);
            @object["x"] = vec2.x;
            @object["y"] = vec2.y;
            return @object;
        }
        public object ConvertFrom(object @object, Vector2 value, IRegistry registry)
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

            if (!@object.Type.Equals(TypeRefs.Math.Vector2))
            {
                throw new InvalidOperationException("Cannot convert value to or from Vector2");
            }
        }
    }
}

