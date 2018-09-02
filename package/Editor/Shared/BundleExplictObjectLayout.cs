using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Optional context object used for overriding the location where specific objects will be serialized
    /// </summary>
    public class BundleExplictObjectLayout : IBundleExplictObjectLayout
    {
        /// <inheritdoc />
        public Dictionary<ObjectIdentifier, string> ExplicitObjectLocation { get; private set; }

        /// <summary>
        /// Default constructor, initializes properties to defaults
        /// </summary>
        public BundleExplictObjectLayout()
        {
            ExplicitObjectLocation = new Dictionary<ObjectIdentifier, string>();
        }
    }
}
