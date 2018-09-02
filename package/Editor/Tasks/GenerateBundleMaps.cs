using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Interfaces;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.Tasks
{
    public struct GenerateBundleMaps : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IDependencyData), typeof(IBundleWriteData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IDependencyData>(), context.GetContextObject<IBundleWriteData>());
        }

        public static ReturnCodes Run(IDependencyData dependencyData, IBundleWriteData writeData)
        {
            foreach (var assetFilesPair in writeData.AssetToFiles)
            {
                // Generate BuildReferenceMap map
                AddReferencesForFiles(assetFilesPair.Value, writeData);

                // Generate BuildUsageTagSet map
                AddUsageSetForFiles(assetFilesPair.Key, assetFilesPair.Value, dependencyData, writeData);
            }

            return ReturnCodes.Success;
        }

        static void AddReferencesForFiles(IList<string> files, IBundleWriteData writeData)
        {
            BuildReferenceMap referenceMap;
            if (!writeData.FileToReferenceMap.TryGetValue(files[0], out referenceMap))
            {
                referenceMap = new BuildReferenceMap();
                writeData.FileToReferenceMap.Add(files[0], referenceMap);
            }

            var fileToCommand = writeData.WriteOperations.ToDictionary(x => x.command.internalName, x => x.command);
            foreach (var file in files)
            {
                var command = fileToCommand[file];
                referenceMap.AddMappings(file, command.serializeObjects.ToArray());
            }
        }

        static void AddUsageSetForFiles(GUID asset, IList<string> files, IDependencyData dependencyData, IBundleWriteData writeData)
        {
            BuildUsageTagSet assetUsage;
            if (!dependencyData.AssetUsage.TryGetValue(asset, out assetUsage))
            {
                if (!dependencyData.SceneUsage.TryGetValue(asset, out assetUsage))
                    return;
            }

            foreach (var file in files)
            {
                BuildUsageTagSet fileUsage;
                if (!writeData.FileToUsageSet.TryGetValue(file, out fileUsage))
                {
                    fileUsage = new BuildUsageTagSet();
                    writeData.FileToUsageSet.Add(file, fileUsage);
                }
                fileUsage.UnionWith(assetUsage);
            }
        }
    }
}
