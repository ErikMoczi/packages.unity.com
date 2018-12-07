

using System;
using System.Linq;

using UnityEditor;

namespace Unity.Tiny
{
    internal static class ConverterRegistration
    {
        [TinyInitializeOnLoad]
        public static void RegisterConverters()
        {
            var assembly = typeof(ConverterRegistration).Assembly;
            var converterTo = typeof(IConverterTo<>);
            var converterFrom = typeof(IConverterFrom<>);

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.ContainsGenericParameters)
                {
                    continue;
                }

                Register(type, converterTo);
                Register(type, converterFrom);
            }
        }

        private static void Register(Type type, Type converterType)
        {
            Type converter = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == converterType);
            if (null != converter)
            {
                var TValue = converter.GetGenericArguments()[0];
                Type typedObjectConverter = typeof(ObjectConverter<>).MakeGenericType(new Type[] { TValue });
                var register = typedObjectConverter.GetMethod("Register", new Type[] { converterType.MakeGenericType(TValue) });
                register.Invoke(null, new object[] { Activator.CreateInstance(type) });
            }
        }
    }
}

