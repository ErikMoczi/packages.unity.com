namespace Unity.Tiny
{
    internal static class PrefabAttributeUtility
    {
        internal static void AddPrefabComponentAttributes(TinyObject component)
        {
            component.AddAttribute(SerializationAttributes.NonSerializedForPersistence);
        }
        
        internal static void RemovePrefabComponentAttributes(TinyObject component)
        {
            component.RemoveAttribute(SerializationAttributes.NonSerializedForPersistence);
        }
    }
}