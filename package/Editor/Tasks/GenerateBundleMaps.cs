using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class GenerateBundleMaps : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IDependencyData), typeof(IBundleWriteData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return Run(context.GetContextObject<IDependencyData>(), context.GetContextObject<IBundleWriteData>());
        }

        static ReturnCode Run(IDependencyData dependencyData, IBundleWriteData writeData)
        {
            Dictionary<string, HashSet<string>> filesMapped = new Dictionary<string, HashSet<string>>();
            foreach (var assetFilesPair in writeData.AssetToFiles)
            {
                // Generate BuildReferenceMap map
                AddReferencesForFiles(assetFilesPair.Value, writeData, filesMapped);
                
                // Generate BuildUsageTagSet map
                AddUsageSetForFiles(assetFilesPair.Key, assetFilesPair.Value, dependencyData, writeData);
            }

            return ReturnCode.Success;
        }

        static void AddReferencesForFiles(IList<string> files, IBundleWriteData writeData, Dictionary<string, HashSet<string>> filesMapped)
        {
            HashSet<string> visited;
            filesMapped.GetOrAdd(files[0], out visited);

            BuildReferenceMap referenceMap;
            if (!writeData.FileToReferenceMap.TryGetValue(files[0], out referenceMap))
            {
                referenceMap = new BuildReferenceMap();
                writeData.FileToReferenceMap.Add(files[0], referenceMap);
            }
            
            var fileToCommand = writeData.WriteOperations.ToDictionary(x => x.Command.internalName, x => x.Command);
            foreach (var file in files)
            {
                if (!visited.Add(file))
                    continue;
                
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
