using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.WriteTypes;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.Tasks
{
    public struct GenerateCommands : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IDependencyData), typeof(IWriteData), typeof(IDeterministicIdentifiers) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IDependencyData>(), context.GetContextObject<IWriteData>(), context.GetContextObject<IDeterministicIdentifiers>());
        }

        public static ReturnCodes Run(IDependencyData dependencyData, IWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            var usageSet = new BuildUsageTagSet();
            foreach (var assetUsage in dependencyData.AssetUsage.Values)
                usageSet.UnionWith(assetUsage);
            foreach (var sceneUsage in dependencyData.SceneUsage.Values)
                usageSet.UnionWith(sceneUsage);

            var referenceMap = new BuildReferenceMap();

            foreach (var filePair in writeData.FileToObjects)
                CreateRawCommand(filePair.Key, filePair.Value, referenceMap, usageSet, writeData, packingMethod);

            foreach (var sceneInfo in dependencyData.SceneInfo.Values)
                CreateSceneRawCommand(sceneInfo, referenceMap, usageSet, writeData, packingMethod);

            foreach (var op in writeData.WriteOperations)
                referenceMap.AddMappings(op.command.internalName, op.command.serializeObjects.ToArray()); // TODO: Add Overload that takes List<>

            return ReturnCodes.Success;
        }

        static void CreateRawCommand(string internalName, List<ObjectIdentifier> objects, BuildReferenceMap referenceMap, BuildUsageTagSet usageSet, IWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            var rOp = new RawWriteOperation();
            rOp.command = new WriteCommand();
            rOp.command.internalName = internalName;
            rOp.command.fileName = Path.GetFileName(internalName); // TODO: Maybe remove this from C++?
            // rOp.command.dependencies // TODO: Definitely remove this from C++

            rOp.command.serializeObjects = objects.Select(x => new SerializationInfo
            {
                serializationObject = x,
                serializationIndex = packingMethod.SerializationIndexFromObjectIdentifier(x)
            }).ToList();

            rOp.referenceMap = referenceMap;
            rOp.usageSet = usageSet;

            writeData.WriteOperations.Add(rOp);
        }

        static void CreateSceneRawCommand(SceneDependencyInfo sceneInfo, BuildReferenceMap referenceMap, BuildUsageTagSet usageSet, IWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            var srOp = new SceneRawWriteOperation();
            srOp.command = new WriteCommand();
            srOp.command.internalName = packingMethod.GenerateInternalFileName(sceneInfo.scene) + ".sharedAssets"; // TODO: This is a bit awkward that we require this extension 
            srOp.command.fileName = Path.GetFileName(srOp.command.internalName); // TODO: Maybe remove this from C++?
            // srOp.command.dependencies // TODO: Definitely remove this from C++
            srOp.command.serializeObjects = new List<SerializationInfo>(); // TODO: Currently unused in this case, possible use in the future

            srOp.referenceMap = referenceMap;
            srOp.usageSet = usageSet;
            srOp.scene = sceneInfo.scene;
            srOp.processedScene = sceneInfo.processedScene;

            writeData.WriteOperations.Add(srOp);
        }
    }
}
