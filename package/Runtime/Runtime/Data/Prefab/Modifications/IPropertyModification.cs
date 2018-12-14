using Unity.Properties;

namespace Unity.Tiny
{
    /// <summary>
    /// Modification to a single property of a prefab instance
    /// </summary>
    internal interface IPropertyModification : IPropertyContainer
    {
        /// <summary>
        /// Target component
        /// </summary>
        TinyType.Reference Target { get; }

        /// <summary>
        /// Short property path relative to the component
        ///
        /// @NOTE This is stripped down version of the path (e.g. position.x)
        /// </summary>
        string Path { get; }
        
        /// <summary>
        /// TypeCode of this modification for polymorphic serialization
        /// </summary>
        int TypeCode { get; }
        
        /// <summary>
        /// Modification value
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// Gets the full property path relative to the `Target` component
        ///
        /// @NOTE This is the full property path (e.g. Properties.position.Properties.x)
        /// </summary>
        /// <returns></returns>
        PropertyPath GetFullPath();
    }
}