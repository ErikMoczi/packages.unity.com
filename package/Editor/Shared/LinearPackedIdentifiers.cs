using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Generates identifiers linearly for built content. Only deterministic if object order and initial Index is deterministic.
    /// </summary>
    public struct LinearPackedIdentifiers : IDeterministicIdentifiers
    {
        /// <summary>
        /// The index at which to start linear id assignment.
        /// </summary>
        public long Index { get; set; }

        /// <summary>
        /// Default constructor, takes an initial index at which to start linear id assignment.
        /// </summary>
        /// <param name="index">Initial index at which to start linear id assignment.</param>
        public LinearPackedIdentifiers(long index) : this()
        {
            Index = index;
        }
        
        /// <inheritdoc />
        public string GenerateInternalFileName(string name)
        {
            Hash128 hash = HashingMethods.CalculateMD5Hash(name);
            return string.Format("CAB-{0}", hash);
        }
        
        /// <inheritdoc />
        public long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            return Index++;
        }
    }
}
