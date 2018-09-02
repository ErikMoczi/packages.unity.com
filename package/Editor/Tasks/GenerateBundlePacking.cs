using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class GenerateBundlePacking : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBundleBuildContent), typeof(IDependencyData), typeof(IBundleWriteData), typeof(IDeterministicIdentifiers) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return Run(context.GetContextObject<IBundleBuildContent>(), context.GetContextObject<IDependencyData>(), context.GetContextObject<IBundleWriteData>(),
                context.GetContextObject<IDeterministicIdentifiers>());
        }

        static ReturnCode Run(IBundleBuildContent buildContent, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            Dictionary<GUID, List<GUID>> assetToReferences = new Dictionary<GUID, List<GUID>>();

            // Pack each asset bundle
            foreach (var bundle in buildContent.BundleLayout)
            {
                if (ValidationMethods.ValidAssetBundle(bundle.Value))
                    PackAssetBundle(bundle.Key, bundle.Value, dependencyData, writeData, packingMethod, assetToReferences);
                else if (ValidationMethods.ValidSceneBundle(bundle.Value))
                    PackSceneBundle(bundle.Key, bundle.Value, dependencyData, writeData, packingMethod, assetToReferences);
            }

            // Calculate Asset file load dependency list
            foreach (var bundle in buildContent.BundleLayout)
            {
                foreach (var asset in bundle.Value)
                {
                    List<string> files = writeData.AssetToFiles[asset];
                    List<GUID> references = assetToReferences[asset];
                    foreach (var reference in references)
                    {
                        List<string> referenceFiles = writeData.AssetToFiles[reference];
                        if (!files.Contains(referenceFiles[0]))
                            files.Add(referenceFiles[0]);
                    }
                }
            }

            return ReturnCode.Success;
        }

        static void PackAssetBundle(string bundleName, List<GUID> includedAssets, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod, Dictionary<GUID, List<GUID>> assetToReferences)
        {
            var internalName = string.Format(CommonStrings.AssetBundleNameFormat, packingMethod.GenerateInternalFileName(bundleName));

            var allObjects = new HashSet<ObjectIdentifier>();
            foreach (var asset in includedAssets)
            {
                AssetLoadInfo assetInfo = dependencyData.AssetInfo[asset];
                allObjects.UnionWith(assetInfo.includedObjects);

                var references = new List<ObjectIdentifier>();
                references.AddRange(assetInfo.referencedObjects);
                assetToReferences[asset] = FilterReferencesForAsset(dependencyData, asset, references);

                allObjects.UnionWith(references);
                writeData.AssetToFiles[asset] = new List<string> { internalName };
            }

            writeData.FileToBundle.Add(internalName, bundleName);
            writeData.FileToObjects.Add(internalName, allObjects.ToList());
        }

        static void PackSceneBundle(string bundleName, List<GUID> includedScenes, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod, Dictionary<GUID, List<GUID>> assetToReferences)
        {
            var firstFileName = "";
            foreach (var scene in includedScenes)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(scene.ToString());
                var internalSceneName = packingMethod.GenerateInternalFileName(scenePath);
                if (string.IsNullOrEmpty(firstFileName))
                    firstFileName = internalSceneName;
                var internalName = string.Format(CommonStrings.SceneBundleNameFormat, firstFileName, internalSceneName);

                SceneDependencyInfo sceneInfo = dependencyData.SceneInfo[scene];

                var references = new List<ObjectIdentifier>();
                references.AddRange(sceneInfo.referencedObjects);
                assetToReferences[scene] = FilterReferencesForAsset(dependencyData, scene, references);

                writeData.FileToObjects.Add(internalName, references);
                writeData.FileToBundle.Add(internalName, bundleName);
                writeData.AssetToFiles[scene] = new List<string> { internalName };
            }
        }

        static List<GUID> FilterReferencesForAsset(IDependencyData dependencyData, GUID asset, List<ObjectIdentifier> references)
        {
            var referencedAssets = new HashSet<AssetLoadInfo>();

            // First pass: Remove Default Resources and Includes for Assets assigned to Bundles
            for (int i = references.Count - 1; i >= 0; --i)
            {
                var reference = references[i];
                if (reference.filePath == CommonStrings.UnityDefaultResourcePath)
                {
                    references.RemoveAt(i);
                    continue; // TODO: Fix this so we can pull these in
                }

                AssetLoadInfo referenceInfo;
                if (dependencyData.AssetInfo.TryGetValue(reference.guid, out referenceInfo))
                {
                    references.RemoveAt(i);
                    referencedAssets.Add(referenceInfo);
                    continue;
                }
            }

            // Second pass: Remove References also included by non-circular Referenced Assets
            foreach (var referencedAsset in referencedAssets)
            {
                var circularRef = referencedAsset.referencedObjects.Select(x => x.guid).Contains(asset);
                if (circularRef)
                    continue;

                references.RemoveAll(x => referencedAsset.referencedObjects.Contains(x));
            }

            // Final pass: Remove References also included by circular Referenced Assets if Asset's GUID is higher than Referenced Asset's GUID
            foreach (var referencedAsset in referencedAssets)
            {
                var circularRef = referencedAsset.referencedObjects.Select(x => x.guid).Contains(asset);
                if (!circularRef)
                    continue;

                if (asset < referencedAsset.asset)
                    continue;

                references.RemoveAll(x => referencedAsset.referencedObjects.Contains(x));
            }
            return referencedAssets.Select(x => x.asset).ToList();
        }
    }
}
