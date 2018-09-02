using System;
using System.Text;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Generates a deterministic identifier using a MD4 hash algorithm and does not require object ordering to be deterministic.
    /// This algorithm generates identical results to what is used internally in <seealso cref="BuildPipeline.BuildAssetbundles"/>.
    /// </summary>
    public class Unity5PackedIdentifiers : IDeterministicIdentifiers
    {
        /// <inheritdoc />
        public string GenerateInternalFileName(string name)
        {
            var md4 = MD4.Create();
            byte[] bytes = Encoding.ASCII.GetBytes(name);
            md4.TransformFinalBlock(bytes, 0, bytes.Length);
            return "CAB-" + BitConverter.ToString(md4.Hash, 0).ToLower().Replace("-", "");
        }
        
        /// <inheritdoc />
        public long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            byte[] bytes;
            var md4 = MD4.Create();
            if (objectID.fileType == FileType.MetaAssetType || objectID.fileType == FileType.SerializedAssetType)
            {
                // TODO: Variant info
                // NOTE: ToString() required as unity5 uses the GUID as a string to hash
                bytes = Encoding.ASCII.GetBytes(objectID.guid.ToString());
                md4.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
                bytes = BitConverter.GetBytes((int)objectID.fileType);
                md4.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
            }
            // Or path
            else
            {
                bytes = Encoding.ASCII.GetBytes(objectID.filePath);
                md4.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
            }

            bytes = BitConverter.GetBytes(objectID.localIdentifierInFile);
            md4.TransformFinalBlock(bytes, 0, bytes.Length);
            long hash = BitConverter.ToInt64(md4.Hash, 0);
            return hash;
        }
    }
}