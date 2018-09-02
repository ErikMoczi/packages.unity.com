using System;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build
{
    public struct PrefabPackedIdentifiers : IDeterministicIdentifiers
    {
        public string GenerateInternalFileName(string name)
        {
            Hash128 hash = HashingMethods.CalculateMD5Hash(name);
            return string.Format("CAB-{0}", hash);
        }

        public long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            byte[] assetHash = HashingMethods.CalculateMD5(objectID.guid, objectID.filePath);
            byte[] objectHash = HashingMethods.CalculateMD5(objectID);

            var assetVal = BitConverter.ToUInt64(assetHash, 0);
            var objectVal = BitConverter.ToUInt64(objectHash, 0);
            return (long)((0xFFFFFFFF00000000 & assetVal) | (0x00000000FFFFFFFF & (objectVal ^ assetVal)));
        }
    }
}
