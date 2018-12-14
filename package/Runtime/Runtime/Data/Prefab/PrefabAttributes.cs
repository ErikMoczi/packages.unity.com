namespace Unity.Tiny.Attributes
{
    /// <summary>
    /// Attribute to flag the given object as being part of a prefab instance
    /// </summary>
    internal class PrefabInstanceAttribute : IPropertyAttribute { }
    
    /// <summary>
    /// Attribute to flag a field as being custom.
    /// This field ignores compression and expansion of property paths
    ///
    /// e.g. When serializing a PrefabModification we compress
    ///
    ///     "obj.Items[0]
    ///     "Properties.position.Properties.x" -> "position.x"
    ///
    /// In certain cases the field is NOT part of a UTinyObject and should not be expanded
    /// (e.g. someList.Count)
    /// 
    /// </summary>
    internal class CustomFieldAttribute : IPropertyAttribute
    {
        public string Prefix { get; set; }
    }

    internal static class PrefabAttributes
    {
        /// <summary>
        /// This attribute is used internally by the prefab system to flag an object as being part of a prefab instance hierarchy
        /// </summary>
        public static readonly PrefabInstanceAttribute PrefabInstance = new PrefabInstanceAttribute();
        
        /// <summary>
        /// Used during prefab property path serialization
        ///
        /// Custom fields are stored with a `!` prefix to ignore property path expansion during deserialization
        /// </summary>
        public static readonly CustomFieldAttribute CustomField = new CustomFieldAttribute {Prefix = "!"};
    }
}