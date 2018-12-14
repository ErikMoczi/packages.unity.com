

using System;

namespace Unity.Tiny
{
    internal struct Range
    {
        public float start;
        public float end;

        public Range(float s, float e)
        {
            start = s;
            end = e;
        }
    }

    internal class RangeConverter : IConverterTo<Range>, IConverterFrom<Range>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Range>(TypeRefs.Math.Range);

        public Range ConvertTo(object @object, IRegistry registry)
        {
            return ConvertTo(@object as TinyObject);
        }

        private Range ConvertTo(TinyObject @object)
        {
            ValidateType(@object);
            return new Range(
                @object.GetProperty<float>("start"),
                @object.GetProperty<float>("end")
            );
        }

        public TinyObject ConvertFrom(TinyObject @object, Range value)
        {
            ValidateType(@object);
            @object["start"] = value.start;
            @object["end"] = value.end;
            return @object;
        }
        public object ConvertFrom(object @object, Range value, IRegistry registry)
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

            if (!@object.Type.Equals(TypeRefs.Math.Range))
            {
                throw new InvalidOperationException("Cannot convert value to or from Range");
            }
        }

    }
}

