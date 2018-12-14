using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Unity.Tiny
{
    internal static class RegistryObjectRemap
    {
        /// <summary>
        /// Remaps the tiny object ids for all of the given assets
        /// </summary>
        /// <param name="assetPaths">Asset paths</param>
        public static void Remap(IEnumerable<string> assetPaths, Dictionary<string, string> guids)
        {
            foreach (var path in assetPaths)
            {
                var content = File.ReadAllText(path);

                foreach (var guid in guids)
                {
                    content = content.Replace(guid.Key, guid.Value);
                }

                File.WriteAllText(path, content);

                // Re-import this asset to have the importer update the database
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUncompressedImport);
            }
        }
    }
}