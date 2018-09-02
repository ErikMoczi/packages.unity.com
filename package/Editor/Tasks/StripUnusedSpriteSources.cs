using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class StripUnusedSpriteSources : IBuildTask
    {
        const int k_Version = 2;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IDependencyData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            
            IBuildSpriteData spriteData;
            context.TryGetContextObject(out spriteData);
            return Run(context.GetContextObject<IDependencyData>(), spriteData);
        }

        static ReturnCode Run(IDependencyData dependencyData, IBuildSpriteData spriteData)
        {
            if (spriteData == null || spriteData.ImporterData.Count == 0)
                return ReturnCode.SuccessNotRun;

            var unusedSources = new HashSet<ObjectIdentifier>();
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
