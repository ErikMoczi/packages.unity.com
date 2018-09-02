using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline.WriteTypes;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public struct GenerateBundleCommands : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBundleBuildContent), typeof(IDependencyData), typeof(IBundleWriteData), typeof(IDeterministicIdentifiers) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBundleBuildContent>(), context.GetContextObject<IDependencyData>(), context.GetContextObject<IBundleWriteData>(),
                context.GetContextObject<IDeterministicIdentifiers>());
        }

        public static ReturnCodes Run(IBundleBuildContent buildContent, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            foreach (var bundlePair in buildContent.BundleLayout)
            {
                if (ValidationMethods.ValidAssetBundle(bundlePair.Value))
                {
                    CreateAssetBundleCommand(bundlePair.Key, writeData.AssetToFiles[bundlePair.Value[0]][0], bundlePair.Value, buildContent, dependencyData, writeData, packingMethod);
                }
                else if (ValidationMethods.ValidSceneBundle(bundlePair.Value))
                {
                    CreateSceneBundleCommand(bundlePair.Key, writeData.AssetToFiles[bundlePair.Value[0]][0], bundlePair.Value[0], bundlePair.Value, buildContent, dependencyData, writeData);
                    for (int i = 1; i < bundlePair.Value.Count; ++i)
                        CreateSceneDataCommand(writeData.AssetToFiles[bundlePair.Value[i]][0], bundlePair.Value[i], dependencyData, writeData);
                }
            }
            return ReturnCodes.Success;
        }

        static WriteCommand CreateWriteCommand(string internalName, List<ObjectIdentifier> objects, IDeterministicIdentifiers packingMethod)
        {
            var command = new WriteCommand();
            command.internalName = internalName;
            command.fileName = Path.GetFileName(internalName); // TODO: Maybe remove this from C++?

            command.serializeObjects = objects.Select(x => new SerializationInfo
            {
                serializationObject = x,
                serializationIndex = packingMethod.SerializationIndexFromObjectIdentifier(x)
            }).ToList();
            return command;
        }

        static void CreateAssetBundleCommand(string bundleName, string internalName, List<GUID> assets, IBundleBuildContent buildContent, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            var abOp = new AssetBundleWriteOperation();
            abOp.UsageSet = new BuildUsageTagSet();
            writeData.FileToUsageSet.Add(internalName, abOp.UsageSet);

            abOp.ReferenceMap = new BuildReferenceMap();
            writeData.FileToReferenceMap.Add(internalName, abOp.ReferenceMap);

            var fileObjects = writeData.FileToObjects[internalName];
            abOp.Command = CreateWriteCommand(internalName, fileObjects, packingMethod);

            {
                abOp.Info = new AssetBundleInfo();
                abOp.Info.bundleName = bundleName;

                // TODO: Convert this to AssetBundleManifest data
                //var dependencies = new HashSet<string>();
                //var bundles = assets.SelectMany(x => writeData.AssetToFiles[x].Select(y => writeData.FileToBundle[y]));
                //dependencies.UnionWith(bundles);
                //dependencies.Remove(bundleName);
                //abOp.Info.bundleDependencies = dependencies.OrderBy(x => x).ToList();

                abOp.Info.bundleAssets = assets.Select(x => dependencyData.AssetInfo[x]).ToList();
                foreach (var loadInfo in abOp.Info.bundleAssets)
                    loadInfo.address = buildContent.Addresses[loadInfo.asset];
            }

            writeData.WriteOperations.Add(abOp);
        }

        static void CreateSceneBundleCommand(string bundleName, string internalName, GUID asset, List<GUID> assets, IBundleBuildContent buildContent, IDependencyData dependencyData, IBundleWriteData writeData)
        {
            var sbOp = new SceneBundleWriteOperation();
            sbOp.UsageSet = new BuildUsageTagSet();
            writeData.FileToUsageSet.Add(internalName, sbOp.UsageSet);

            sbOp.ReferenceMap = new BuildReferenceMap();
            writeData.FileToReferenceMap.Add(internalName, sbOp.ReferenceMap);

            var fileObjects = writeData.FileToObjects[internalName];
            sbOp.Command = CreateWriteCommand(internalName, fileObjects, new LinearPackedIdentifiers(3)); // Start at 3: PreloadData = 1, AssetBundle = 2

            var sceneInfo = dependencyData.SceneInfo[asset];
            sbOp.Scene = sceneInfo.scene;
            sbOp.ProcessedScene = sceneInfo.processedScene;

            {
                sbOp.PreloadInfo = new PreloadInfo { preloadObjects = sceneInfo.referencedObjects.Where(x => !fileObjects.Contains(x)).ToList() };
            }

            {
                sbOp.Info = new SceneBundleInfo();
                sbOp.Info.bundleName = bundleName;

                // TODO: Convert this to AssetBundleManifest data
                //var dependencies = new HashSet<string>();
                //var bundles = assets.SelectMany(x => writeData.AssetToFiles[x].Select(y => writeData.FileToBundle[y]));
                //dependencies.UnionWith(bundles);
                //dependencies.Remove(bundleName);
                //sbOp.Info.bundleDependencies = dependencies.OrderBy(x => x).ToList();

                sbOp.Info.bundleScenes = assets.Select(x => new SceneLoadInfo
                {
                    asset = x,
                    internalName = Path.GetFileNameWithoutExtension(writeData.AssetToFiles[x][0]),
                    address = buildContent.Addresses[x]
                }).ToList();
            }

            writeData.WriteOperations.Add(sbOp);
        }

        static void CreateSceneDataCommand(string internalName, GUID asset, IDependencyData dependencyData, IBundleWriteData writeData)
        {
            var sdOp = new SceneDataWriteOperation();
            sdOp.UsageSet = new BuildUsageTagSet();
            writeData.FileToUsageSet.Add(internalName, sdOp.UsageSet);

            sdOp.ReferenceMap = new BuildReferenceMap();
            writeData.FileToReferenceMap.Add(internalName, sdOp.ReferenceMap);

            var fileObjects = writeData.FileToObjects[internalName];
            sdOp.Command = CreateWriteCommand(internalName, fileObjects, new LinearPackedIdentifiers(2)); // Start at 2: PreloadData = 1

            var sceneInfo = dependencyData.SceneInfo[asset];
            sdOp.Scene = sceneInfo.scene;
            sdOp.ProcessedScene = sceneInfo.processedScene;

            {
                sdOp.PreloadInfo = new PreloadInfo();
                sdOp.PreloadInfo.preloadObjects = sceneInfo.referencedObjects.Where(x => !fileObjects.Contains(x)).ToList();
            }

            writeData.WriteOperations.Add(sdOp);
        }
    }
}
