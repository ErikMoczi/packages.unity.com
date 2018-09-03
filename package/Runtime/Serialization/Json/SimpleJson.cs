#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unity.Properties.Serialization
{
    public static class SimpleJson
    {
        private static readonly MethodInfo s_SerializeObject;
        private static readonly MethodInfo s_TryDeserializeObject;
        private static readonly MethodInfo s_ParseValue;

        static SimpleJson()
        {
            var simpleJson = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name.Equals("UnityEngine")).GetType("SimpleJson.SimpleJson");

            s_SerializeObject = simpleJson.GetMethod("SerializeObject",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] {typeof(object)},
                null);
            
            s_TryDeserializeObject = simpleJson.GetMethod("TryDeserializeObject",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] {typeof(string), typeof(object).MakeByRefType()},
                null);
        }

        public static string SerializeObject(object json)
        {
            return (string) s_SerializeObject.Invoke(null, new[] {json});
        }

        public static bool TryDeserializeObject(string json, out object @object)
        {
            var parameters = new object[] {json, null};
            var result = (bool) s_TryDeserializeObject.Invoke(null, parameters);
            @object = (result) ? parameters[1] : null;
            return result;
        }

        public static bool TryGetValue<T>(IDictionary<string, object> dict, string key, out T retVal)
        {
            object obj;
            if (dict.TryGetValue(key, out obj) && obj is T)
            {
                retVal = (T) obj;
                return true;
            }

            retVal = default(T);
            return false;
        }
    }
}
#endif // NET_4_6
