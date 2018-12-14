

using System;

using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal interface IConverterTo<out TValue>
    {
        TValue ConvertTo(object obj, IRegistry registry);
    }

    internal interface IConverterFrom<in TValue>
    {
        object ConvertFrom(object obj, TValue value, IRegistry registry);
        TinyObject ConvertFrom(TinyObject obj, TValue vec2);
    }

    internal static class ObjectConverter<TValue>
    {
        private static IConverterTo<TValue> m_ToConverter;
        private static IConverterFrom<TValue> m_FromConverter;

        public static TValue ConvertTo(object obj, IRegistry registry)
        {
            if (null == m_ToConverter)
            {
                throw new NullReferenceException($"{TinyConstants.ApplicationName}: Cannot convert object to {typeof(TValue).Name}, no converter have been registered.");
            }
            return m_ToConverter.ConvertTo(obj, registry);
        }

        public static object ConvertFrom(object obj, TValue value, IRegistry registry)
        {
            if (null == m_FromConverter)
            {
                throw new NullReferenceException($"{TinyConstants.ApplicationName}: Cannot convert from {typeof(TValue).Name}, no converter have been registered.");
            }
            return m_FromConverter.ConvertFrom(obj, value, registry);
        }

        public static TinyObject ConvertFrom(TinyObject obj, TValue value)
        {
            if (null == m_FromConverter)
            {
                throw new NullReferenceException($"{TinyConstants.ApplicationName}: Cannot convert from {typeof(TValue).Name}, no converter have been registered.");
            }
            return m_FromConverter.ConvertFrom(obj, value);
        }

        public static void Register(IConverterTo<TValue> converter)
        {
            m_ToConverter = converter;
        }

        public static void Register(IConverterFrom<TValue> converter)
        {
            m_FromConverter = converter;
        }
    }

    internal static class TinyObjectConverters
    {
        public static TValue As<TValue>(this TinyObject obj)
        {
            return ObjectConverter<TValue>.ConvertTo(obj, obj.Registry);
        }

        public static TValue GetProperty<TValue>(this TinyObject obj, string propertyName)
        {
            Assert.IsFalse(string.IsNullOrEmpty(propertyName));
            return ObjectConverter<TValue>.ConvertTo(obj[propertyName], obj.Registry);
        }

        public static TinyObject AssignPropertyFrom<TValue>(this TinyObject obj, string propertyName, TValue value)
        {
            Assert.IsFalse(string.IsNullOrEmpty(propertyName));
            obj[propertyName] = ObjectConverter<TValue>.ConvertFrom(obj[propertyName], value, obj.Registry);
            return obj;
        }

        public static TinyObject AssignFrom<TValue>(this object obj, TValue value)
        {
            return ObjectConverter<TValue>.ConvertFrom(obj as TinyObject, value);
        }

        public static TinyObject AssignFrom<TValue>(this TinyObject obj, TValue value)
        {
            return ObjectConverter<TValue>.ConvertFrom(obj, value);
        }
    }
}

