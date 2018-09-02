using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build
{
    public struct LinearPackedIdentifiers : IDeterministicIdentifiers
    {
        public long Index { get; set; }

        public LinearPackedIdentifiers(long index) : this()
        {
            Index = index;
        }

        public string GenerateInternalFileName(string name)
        {
            Hash128 hash = HashingMethods.CalculateMD5Hash(name);
            return string.Format("CAB-{0}", hash);
        }

        public long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            return Index++;
        }
    }
}
