using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.Tasks
{
    public struct StripUnusedSpriteSources : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IDependencyData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IDependencyData>());
        }

        static Hash128 CalculateInputHash(bool useCache, IDependencyData dependencyData)
        {
            if (!useCache)
                return new Hash128();

            return HashingMethods.CalculateMD5Hash(k_Version, dependencyData.AssetInfo, dependencyData.SceneInfo);
        }

        public static ReturnCodes Run(IBuildParameters parameters, IDependencyData dependencyData)
        {
            var spriteSourceRef = new Dictionary<ObjectIdentifier, int>();

            Hash128 hash = CalculateInputHash(parameters.UseCache, dependencyData);
            if (TryLoadFromCache(parameters.UseCache, hash, ref spriteSourceRef))
            {
                SetOutputInformation(spriteSourceRef, dependencyData);
                return ReturnCodes.SuccessCached;
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

            if (!TrySaveToCache(parameters.UseCache, hash, spriteSourceRef))
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

        static bool TryLoadFromCache(bool useCache, Hash128 hash, ref Dictionary<ObjectIdentifier, int> spriteSourceRef)
        {
            Dictionary<ObjectIdentifier, int> cachedSpriteSourceRef;
            if (useCache && BuildCache.TryLoadCachedResults(hash, out cachedSpriteSourceRef))
            {
                spriteSourceRef = cachedSpriteSourceRef;
                return true;
            }

            return false;
        }

        static bool TrySaveToCache(bool useCache, Hash128 hash, Dictionary<ObjectIdentifier, int> spriteSourceRef)
        {
            if (useCache && !BuildCache.SaveCachedResults(hash, spriteSourceRef))
                return false;
            return true;
        }
    }
}
