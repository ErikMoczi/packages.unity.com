

namespace Unity.Tiny
{
    /// <summary>
    /// Attribute to opt to "opt-out" of `Undo/Redo` operations
    /// </summary>
    internal class TransientAttribute : IPropertyAttribute { }

    /// <summary>
    /// Attribute to "opt-out" of `Serialization` for specific contexts
    /// </summary>
    internal class NonSerializedInContext : IPropertyAttribute
    {
        public string Context { get; }
        
        public NonSerializedInContext(string context)
        {
            Context = context;
        }
    }
    
    internal static class SerializationAttributes
    {
        /// <summary>
        /// Use this attribute on properties to "opt-out" of `Undo/Redo` operations
        ///
        /// @NOTE The original property values are preserved during the operation
        /// </summary>
        public static readonly TransientAttribute Transient = new TransientAttribute();
        
        /// <summary>
        /// Use this attribute to "opt-out" of `Serialization` when writing to disc (e.g. json)
        ///
        /// @NOTE The property values are lost during the operation
        /// </summary>
        public static readonly NonSerializedInContext NonSerializedForPersistence = new NonSerializedInContext(Serialization.SerializationContext.Persistence);
    }
}

