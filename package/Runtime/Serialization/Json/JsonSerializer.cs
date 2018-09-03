#if (NET_4_6 || NET_STANDARD_2_0)

using System.Collections.Generic;

namespace Unity.Properties.Serialization
{
    public static class JsonSerializer
    {
        public static string Serialize<TContainer>(TContainer container, JsonPropertyVisitor visitor = null)
            where TContainer : class, IPropertyContainer
        {
            return JsonPropertyContainerWriter.Write(container, visitor);
        }
        
        public static string Serialize<TContainer>(ref TContainer container, JsonPropertyVisitor visitor = null)
            where TContainer : struct, IPropertyContainer
        {
            return JsonPropertyContainerWriter.Write(ref container, visitor);
        }

        public static ObjectContainer Deserialize(string json)
        {
            object obj;
            Json.TryDeserializeObject(json, out obj);
            return new ObjectContainer(obj as IDictionary<string, object>);
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
