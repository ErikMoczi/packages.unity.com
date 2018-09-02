using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Build.WriteTypes;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.Tasks
{
    public struct GenerateBundleCommands : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBundleContent), typeof(IDependencyData), typeof(IBundleWriteData), typeof(IDeterministicIdentifiers) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBundleContent>(), context.GetContextObject<IDependencyData>(), context.GetContextObject<IBundleWriteData>(), 
                context.GetContextObject<IDeterministicIdentifiers>());
        }

        public static ReturnCodes Run(IBundleContent content, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            foreach (var bundlePair in content.BundleLayout)
            {
                if (ValidationMethods.ValidAssetBundle(bundlePair.Value))
                {
                    CreateAssetBundleCommand(bundlePair.Key, writeData.AssetToFiles[bundlePair.Value[0]][0], bundlePair.Value, content, dependencyData, writeData, packingMethod);
                }
                else if (ValidationMethods.ValidSceneBundle(bundlePair.Value))
                {
                    CreateSceneBundleCommand(bundlePair.Key, writeData.AssetToFiles[bundlePair.Value[0]][0], bundlePair.Value[0], bundlePair.Value, content, dependencyData, writeData);
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
            // command.dependencies // TODO: Definitely remove this from C++

            command.serializeObjects = objects.Select(x => new SerializationInfo
            {
                serializationObject = x,
                serializationIndex = packingMethod.SerializationIndexFromObjectIdentifier(x)
            }).ToList();
            return command;
        }

        static void CreateAssetBundleCommand(string bundleName, string internalName, List<GUID> assets, IBundleContent content, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            var abOp = new AssetBundleWriteOperation();
            abOp.usageSet = new BuildUsageTagSet();
            writeData.FileToUsageSet.Add(internalName, abOp.usageSet);

            abOp.referenceMap = new BuildReferenceMap();
            writeData.FileToReferenceMap.Add(internalName, abOp.referenceMap);

            var fileObjects = writeData.FileToObjects[internalName];
            abOp.command = CreateWriteCommand(internalName, fileObjects, packingMethod);

            {
                abOp.info = new AssetBundleInfo();
                abOp.info.bundleName = bundleName;

                var dependencies = new HashSet<string>();
                var bundles = assets.SelectMany(x => writeData.AssetToFiles[x].Select(y => writeData.FileToBundle[y]));
                dependencies.UnionWith(bundles);
                dependencies.Remove(bundleName);

                abOp.info.bundleDependencies = dependencies.OrderBy(x => x).ToList();
                abOp.info.bundleAssets = assets.Select(x => dependencyData.AssetInfo[x]).ToList();
                foreach (var loadInfo in abOp.info.bundleAssets)
                    loadInfo.address = content.Addresses[loadInfo.asset];
            }

            writeData.WriteOperations.Add(abOp);
        }

        static void CreateSceneBundleCommand(string bundleName, string internalName, GUID asset, List<GUID> assets, IBundleContent content, IDependencyData dependencyData, IBundleWriteData writeData)
        {
            var sbOp = new SceneBundleWriteOperation();
            sbOp.usageSet = new BuildUsageTagSet();
            writeData.FileToUsageSet.Add(internalName, sbOp.usageSet);

            sbOp.referenceMap = new BuildReferenceMap();
            writeData.FileToReferenceMap.Add(internalName, sbOp.referenceMap);

            var fileObjects = writeData.FileToObjects[internalName];
            sbOp.command = CreateWriteCommand(internalName, fileObjects, new LinearPackedIdentifiers(3)); // Start at 3: PreloadData = 1, AssetBundle = 2

            var sceneInfo = dependencyData.SceneInfo[asset];
            sbOp.scene = sceneInfo.scene;
            sbOp.processedScene = sceneInfo.processedScene;

            {
                sbOp.preloadInfo = new PreloadInfo { preloadObjects = sceneInfo.referencedObjects.Where(x => !fileObjects.Contains(x)).ToList() };
            }

            {
                sbOp.info = new SceneBundleInfo();
                sbOp.info.bundleName = bundleName;

                var dependencies = new HashSet<string>();
                var bundles = assets.SelectMany(x => writeData.AssetToFiles[x].Select(y => writeData.FileToBundle[y]));
                dependencies.UnionWith(bundles);
                dependencies.Remove(bundleName);

                sbOp.info.bundleDependencies = dependencies.OrderBy(x => x).ToList();
                sbOp.info.bundleScenes = assets.Select(x => new SceneLoadInfo
                {
                    asset = x,
                    internalName = Path.GetFileNameWithoutExtension(writeData.AssetToFiles[x][0]),
                    address = content.Addresses[x]
                }).ToList();
            }

            writeData.WriteOperations.Add(sbOp);
        }

        static void CreateSceneDataCommand(string internalName, GUID asset, IDependencyData dependencyData, IBundleWriteData writeData)
        {
            var sdOp = new SceneDataWriteOperation();
            sdOp.usageSet = new BuildUsageTagSet();
            writeData.FileToUsageSet.Add(internalName, sdOp.usageSet);

            sdOp.referenceMap = new BuildReferenceMap();
            writeData.FileToReferenceMap.Add(internalName, sdOp.referenceMap);

            var fileObjects = writeData.FileToObjects[internalName];
            sdOp.command = CreateWriteCommand(internalName, fileObjects, new LinearPackedIdentifiers(2)); // Start at 2: PreloadData = 1

            var sceneInfo = dependencyData.SceneInfo[asset];
            sdOp.scene = sceneInfo.scene;
            sdOp.processedScene = sceneInfo.processedScene;

            {
                sdOp.preloadInfo = new PreloadInfo();
                sdOp.preloadInfo.preloadObjects = sceneInfo.referencedObjects.Where(x => !fileObjects.Contains(x)).ToList();
            }

            writeData.WriteOperations.Add(sdOp);
        }
    }
}
