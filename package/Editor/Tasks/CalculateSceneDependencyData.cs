using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class CalculateSceneDependencyData : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBuildContent), typeof(IDependencyData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            IBuildParameters parameters = context.GetContextObject<IBuildParameters>();

            IProgressTracker tracker;
            context.TryGetContextObject(out tracker);
            IBuildCache cache = null;
            if (parameters.UseCache)
                context.TryGetContextObject(out cache);

            return Run(parameters, context.GetContextObject<IBuildContent>(), context.GetContextObject<IDependencyData>(), tracker, cache);
        }

        static CachedInfo GetCachedInfo(IBuildCache cache, GUID scene, IEnumerable<ObjectIdentifier> references, SceneDependencyInfo sceneInfo, BuildUsageTagSet usageTags)
        {
            var info = new CachedInfo();
            info.Asset = cache.GetCacheEntry(scene);

            var dependencies = new HashSet<CacheEntry>();
            foreach (var reference in references)
                dependencies.Add(cache.GetCacheEntry(reference));
            info.Dependencies = dependencies.ToArray();

            info.Data = new object[] { sceneInfo, usageTags };

            return info;
        }

        static ReturnCode Run(IBuildParameters parameters, IBuildContent content, IDependencyData dependencyData, IProgressTracker tracker, IBuildCache cache)
        {
            if (content.Scenes.IsNullOrEmpty())
                return ReturnCode.SuccessNotRun;


            IList<CachedInfo> cachedInfo = null;
            List<CachedInfo> uncachedInfo = null;
            if (cache != null)
            {
                IList<CacheEntry> entries = content.Scenes.Select(cache.GetCacheEntry).ToList();
                cache.LoadCachedData(entries, out cachedInfo);

                uncachedInfo = new List<CachedInfo>();
            }

            for (int i = 0; i < content.Scenes.Count; i++)
            {
                GUID scene = content.Scenes[i];
                string scenePath = AssetDatabase.GUIDToAssetPath(scene.ToString());

                SceneDependencyInfo sceneInfo;
                BuildUsageTagSet usageTags;

                if (cachedInfo != null && cachedInfo[i] != null)
                {
                    if (!tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", scenePath)))
                        return ReturnCode.Canceled;

                    sceneInfo = (SceneDependencyInfo)cachedInfo[i].Data[0];
                    usageTags = cachedInfo[i].Data[1] as BuildUsageTagSet;
                }
                else
                {
                    if (!tracker.UpdateInfoUnchecked(scenePath))
                        return ReturnCode.Canceled;

                    usageTags = new BuildUsageTagSet();
                    var outputFolder = cache != null ? cache.GetCachedArtifactsDirectory(cache.GetCacheEntry(scene))
                        : parameters.TempOutputFolder;
                    Directory.CreateDirectory(outputFolder);

                    sceneInfo = ContentBuildInterface.PrepareScene(scenePath, parameters.GetContentBuildSettings(), usageTags, outputFolder);

                    if (cache != null)
                        uncachedInfo.Add(GetCachedInfo(cache, scene, sceneInfo.referencedObjects, sceneInfo, usageTags));
                }

                SetOutputInformation(scene, sceneInfo, usageTags, dependencyData);
            }

            if (cache != null)
                cache.SaveCachedData(uncachedInfo);

            return ReturnCode.Success;
        }

        static void SetOutputInformation(GUID asset, SceneDependencyInfo sceneInfo, BuildUsageTagSet usageTags, IDependencyData dependencyData)
        {
            // Add generated scene information to BuildDependencyData
            dependencyData.SceneInfo.Add(asset, sceneInfo);
            dependencyData.SceneUsage.Add(asset, usageTags);
        }
    }
}
