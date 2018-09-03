#if NET_4_6
namespace Unity.Properties.Serialization
{
    public static class JsonSerializer
    {
        public static object Deserialize(string json)
        {
            object result;
            return Json.TryDeserializeObject(json, out result) ? result : null;
        }

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
    }
}
#endif // NET_4_6
