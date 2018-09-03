namespace Unity.Properties.Serialization
{
    public static class JsonSerializer
    {
        public static object Deserialize(string json)
        {
            object result;
            return SimpleJson.TryDeserializeObject(json, out result) ? result : null;
        }

        public static string Serialize<TContainer>(TContainer container)
            where TContainer : class, IPropertyContainer
        {
            return JsonPropertyContainerWriter.Write(container);
        }
        
        public static string SerializeStruct<TContainer>(TContainer container)
            where TContainer : struct, IPropertyContainer
        {
            return JsonPropertyContainerWriter.WriteStruct(ref container);
        }
    }
}
