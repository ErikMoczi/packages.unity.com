using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public struct StripUnusedSpriteSources : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IDependencyData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            IBuildCache cache;
            context.TryGetContextObject(out cache);
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IDependencyData>(), cache);
        }

        static void CalcualteCacheEntry(IDependencyData dependencyData, ref CacheEntry cacheEntry)
        {
            cacheEntry.hash = HashingMethods.CalculateMD5Hash(k_Version, dependencyData.AssetInfo, dependencyData.SceneInfo);
            cacheEntry.guid = HashingMethods.CalculateMD5Guid("StripUnusedSpriteSources");
        }

        public static ReturnCodes Run(IBuildParameters parameters, IDependencyData dependencyData, IBuildCache cache = null)
        {
            var spriteSourceRef = new Dictionary<ObjectIdentifier, int>();

            var cacheEntry = new CacheEntry();
            if (parameters.UseCache && cache != null)
            {
                CalcualteCacheEntry(dependencyData, ref cacheEntry);
                if (cache.TryLoadFromCache(cacheEntry, ref spriteSourceRef))
                {
                    SetOutputInformation(spriteSourceRef, dependencyData);
                    return ReturnCodes.SuccessCached;
                }
            }

            // CreateBundle sprite source ref count map
            foreach (KeyValuePair<GUID, AssetLoadInfo> assetInfo in dependencyData.AssetInfo)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetInfo.Value.asset.ToString());
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType == TextureImporterType.Sprite && !string.IsNullOrEmpty(importer.spritePackingTag))
                    spriteSourceRef[assetInfo.Value.includedObjects[0]] = 0;
            }

            // Count refs from assets
            var assetRefs = dependencyData.AssetInfo.SelectMany(x => x.Value.referencedObjects);
            foreach (ObjectIdentifier reference in assetRefs)
            {
                int refCount = 0;
                if (!spriteSourceRef.TryGetValue(reference, out refCount))
                    continue;

                // Note: Because pass by value
                spriteSourceRef[reference] = ++refCount;
            }

            // Count refs from scenes
            var sceneRefs = dependencyData.SceneInfo.SelectMany(x => x.Value.referencedObjects);
            foreach (ObjectIdentifier reference in sceneRefs)
            {
                int refCount = 0;
                if (!spriteSourceRef.TryGetValue(reference, out refCount))
                    continue;

                // Note: Because pass by value
                spriteSourceRef[reference] = ++refCount;
            }

            SetOutputInformation(spriteSourceRef, dependencyData);

            if (parameters.UseCache && cache != null && !cache.TrySaveToCache(cacheEntry, spriteSourceRef))
                BuildLogger.LogWarning("Unable to cache StripUnusedSpriteSources results.");

            return ReturnCodes.Success;
        }

        static void SetOutputInformation(Dictionary<ObjectIdentifier, int> spriteSourceRef, IDependencyData dependencyData)
        {
            foreach (KeyValuePair<ObjectIdentifier, int> source in spriteSourceRef)
            {
                if (source.Value > 0)
                    continue;

                var assetInfo = dependencyData.AssetInfo[source.Key.guid];
                assetInfo.includedObjects.RemoveAt(0);
            }
        }
    }
}
