using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.Tasks
{
    public class GenerateBundlePacking : IBuildTask
    {
        // TODO: Move to utility file
        public const string k_UnityDefaultResourcePath = "library/unity default resources";
        public const string k_AssetBundleNameFormat = "archive:/{0}/{0}";
        public const string k_SceneBundleNameFormat = "archive:/{0}/{1}.sharedAssets";

        const int k_Version = 1;

        public int Version
        {
            get { return k_Version; }
        }

        static readonly Type[] k_RequiredTypes = { typeof(IBundleContent), typeof(IDependencyData), typeof(IBundleWriteData), typeof(IDeterministicIdentifiers) };

        public Type[] RequiredContextTypes
        {
            get { return k_RequiredTypes; }
        }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBundleContent>(), context.GetContextObject<IDependencyData>(), context.GetContextObject<IBundleWriteData>(),
                context.GetContextObject<IDeterministicIdentifiers>());
        }

        public static ReturnCodes Run(IBundleContent content, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            Dictionary<GUID, List<GUID>> AssetToReferences = new Dictionary<GUID, List<GUID>>();

            // Pack each asset bundle
            foreach (var bundle in content.BundleLayout)
            {
                if (ValidationMethods.ValidAssetBundle(bundle.Value))
                    PackAssetBundle(bundle.Key, bundle.Value, dependencyData, writeData, packingMethod, AssetToReferences);
                else if (ValidationMethods.ValidSceneBundle(bundle.Value))
                    PackSceneBundle(bundle.Key, bundle.Value, dependencyData, writeData, packingMethod, AssetToReferences);
            }

            // Calculate Asset file load dependency list
            foreach (var bundle in content.BundleLayout)
            {
                foreach (var asset in bundle.Value)
                {
                    List<string> files = writeData.AssetToFiles[asset];
                    List<GUID> references = AssetToReferences[asset];
                    foreach (var reference in references)
                    {
                        List<string> referenceFiles = writeData.AssetToFiles[reference];
                        if (!files.Contains(referenceFiles[0]))
                            files.Add(referenceFiles[0]);
                    }
                }
            }

            return ReturnCodes.Success;
        }

        static void PackAssetBundle(string bundleName, List<GUID> includedAssets, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod, Dictionary<GUID, List<GUID>> assetToReferences)
        {
            var internalName = string.Format(k_AssetBundleNameFormat, packingMethod.GenerateInternalFileName(bundleName));

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
                var internalName = string.Format(k_SceneBundleNameFormat, firstFileName, internalSceneName);

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
                if (reference.filePath == k_UnityDefaultResourcePath)
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
