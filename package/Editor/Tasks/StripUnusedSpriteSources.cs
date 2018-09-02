using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class StripUnusedSpriteSources : IBuildTask
    {
        const int k_Version = 2;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IDependencyData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            IBuildCache cache;
            context.TryGetContextObject(out cache);
            IBuildSpriteData spriteData;
            context.TryGetContextObject(out spriteData);
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IDependencyData>(), spriteData, cache);
        }

        static void CalcualteCacheEntry(IDependencyData dependencyData, ref CacheEntry cacheEntry)
        {
            cacheEntry.Hash = HashingMethods.CalculateMD5Hash(k_Version, dependencyData.AssetInfo, dependencyData.SceneInfo);
            cacheEntry.Guid = HashingMethods.CalculateMD5Guid("StripUnusedSpriteSources");
        }

        static ReturnCode Run(IBuildParameters parameters, IDependencyData dependencyData, IBuildSpriteData spriteData, IBuildCache cache = null)
        {
            if (spriteData == null || spriteData.ImporterData.Count == 0)
                return ReturnCode.SuccessNotRun;

            var unusedSources = new HashSet<ObjectIdentifier>();

            var cacheEntry = new CacheEntry();
            if (parameters.UseCache && cache != null)
            {
                CalcualteCacheEntry(dependencyData, ref cacheEntry);
                List<ObjectIdentifier> unusedSourcesList = null; // Old Mono Runtime doesn't implement HashSet serialization
                if (cache.TryLoadFromCache(cacheEntry, ref unusedSourcesList))
                {
                    unusedSources.UnionWith(unusedSourcesList); // Old Mono Runtime doesn't implement HashSet serialization
                    SetOutputInformation(unusedSources, dependencyData);
                    return ReturnCode.SuccessCached;
                }
            }

            var textures = spriteData.ImporterData.Values.Where(x => x.PackedSprite).Select(x => x.SourceTexture);
            unusedSources.UnionWith(textures);

            // Count refs from assets
            var assetRefs = dependencyData.AssetInfo.SelectMany(x => x.Value.referencedObjects);
            foreach (ObjectIdentifier reference in assetRefs)
                unusedSources.Remove(reference);

            // Count refs from scenes
            var sceneRefs = dependencyData.SceneInfo.SelectMany(x => x.Value.referencedObjects);
            foreach (ObjectIdentifier reference in sceneRefs)
                unusedSources.Remove(reference);

            SetOutputInformation(unusedSources, dependencyData);

            if (parameters.UseCache && cache != null && !cache.TrySaveToCache(cacheEntry, unusedSources.ToList())) // Old Mono Runtime doesn't implement HashSet serialization
                BuildLogger.LogWarning("Unable to cache StripUnusedSpriteSources results.");

            return ReturnCode.Success;
        }

        static void SetOutputInformation(HashSet<ObjectIdentifier> unusedSources, IDependencyData dependencyData)
        {
            foreach (var source in unusedSources)
            {
                var assetInfo = dependencyData.AssetInfo[source.guid];
                assetInfo.includedObjects.RemoveAt(0);
            }
        }
    }
}
