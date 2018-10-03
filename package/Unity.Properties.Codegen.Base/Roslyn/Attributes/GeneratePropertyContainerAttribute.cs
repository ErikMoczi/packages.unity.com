using System;

namespace Unity.Properties.Codegen.Experimental
{
    /// <summary>
    /// Attribute needs to tag a type (class/struct) that is meant to be generated as an IPropertyContainer compatible type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class GeneratePropertyContainerAttribute : Attribute
    {
        /// <summary>
        /// Is the property container boilerplate code generated inlined in the class.
        /// If false, a proxy class will be generated.
        /// </summary>
        public bool ContainerIsInlined = true;

        /// <summary>
        /// Generate the .NET accessors?
        /// </summary>
        public bool IncludeAccessor = true;
    }
}
