using System;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class UpdateBundleObjectLayout : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBundleBuildContent), typeof(IDependencyData), typeof(IBundleWriteData), typeof(IDeterministicIdentifiers) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            IBundleExplictObjectLayout layout;
            if (!context.TryGetContextObject(out layout))
                return ReturnCode.SuccessNotRun;

            return Run(layout, context.GetContextObject<IBundleBuildContent>(), context.GetContextObject<IDependencyData>(), 
                context.GetContextObject<IBundleWriteData>(), context.GetContextObject<IDeterministicIdentifiers>());
        }


        static ReturnCode Run(IBundleExplictObjectLayout layout, IBundleBuildContent content, IDependencyData dependencyData, IBundleWriteData writeData, IDeterministicIdentifiers packingMethod)
        {
            if (layout.ExplicitObjectLocation.IsNullOrEmpty())
                return ReturnCode.SuccessNotRun;

            // Go object by object
            foreach (var pair in layout.ExplicitObjectLocation)
            {
                var objectID = pair.Key;
                var bundleName = pair.Value;
                var internalName = string.Format(CommonStrings.AssetBundleNameFormat, packingMethod.GenerateInternalFileName(bundleName));

                // Add dependency on possible new file if asset depends on object
                foreach (var dependencyPair in dependencyData.AssetInfo)
                {
                    var asset = dependencyPair.Key;
                    var assetInfo = dependencyPair.Value;
                    AddFileDependencyIfFound(objectID, internalName, writeData.AssetToFiles[asset], assetInfo.includedObjects, assetInfo.referencedObjects);
                }

                // Add dependency on possible new file if scene depends on object
                foreach (var dependencyPair in dependencyData.SceneInfo)
                {
                    var asset = dependencyPair.Key;
                    var assetInfo = dependencyPair.Value;
                    AddFileDependencyIfFound(objectID, internalName, writeData.AssetToFiles[asset], assetInfo.referencedObjects, null);
                }

                // Remove object from existing FileToObjects
                foreach (var fileObjects in writeData.FileToObjects.Values)
                {
                    if (fileObjects.Contains(objectID))
                        fileObjects.Remove(objectID);
                }

                if (!writeData.FileToBundle.ContainsKey(internalName))
                {
                    writeData.FileToBundle.Add(internalName, bundleName);
                    content.BundleLayout.Add(bundleName, new List<GUID>());
                }

                List<ObjectIdentifier> objectIDs;
                writeData.FileToObjects.GetOrAdd(internalName, out objectIDs);
                if (!objectIDs.Contains(objectID))
                    objectIDs.Add(objectID);
            }
            return ReturnCode.Success;
        }

        static void AddFileDependencyIfFound(ObjectIdentifier objectID, string fileName, List<string> assetFiles, ICollection<ObjectIdentifier> collection1, ICollection<ObjectIdentifier> collection2 = null)
        {
            if (collection1.Contains(objectID) || collection2 != null && collection2.Contains(objectID))
            {
                if (!assetFiles.Contains(fileName))
                    assetFiles.Add(fileName);
            }
        }
    }
}
