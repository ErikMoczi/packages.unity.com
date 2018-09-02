using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.WriteTypes;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class GenerateSpritePathMaps : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBundleWriteData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            
            IBuildSpriteData spriteData;
            context.TryGetContextObject(out spriteData);
            return Run(context.GetContextObject<IBundleWriteData>(), spriteData);
        }

        static int GetWrapOffsetIndex(int index, int offset, int max)
        {
            return (index + offset) % max;
        }

        static ReturnCode Run(IBundleWriteData writeData, IBuildSpriteData spriteData)
        {
            if (spriteData == null || spriteData.ImporterData.Count == 0)
                return ReturnCode.SuccessNotRun;

            Dictionary<string, IWriteOperation> fileToOperation = writeData.WriteOperations.ToDictionary(x => x.Command.internalName, x => x);
            foreach (GUID asset in spriteData.ImporterData.Keys)
            {
                var mainFile = writeData.AssetToFiles[asset][0];
                var abOp = fileToOperation[mainFile] as AssetBundleWriteOperation;

                var assetInfoIndex = abOp.Info.bundleAssets.FindIndex(x => x.asset == asset);
                var assetInfo = abOp.Info.bundleAssets[assetInfoIndex];
                for (int i = 1; i < assetInfo.includedObjects.Count; i++)
                {
                    var secondaryAssetInfo = new AssetLoadInfo();
                    secondaryAssetInfo.asset = assetInfo.asset;
                    secondaryAssetInfo.address = assetInfo.address;
                    secondaryAssetInfo.referencedObjects = assetInfo.referencedObjects;

                    secondaryAssetInfo.includedObjects = new List<ObjectIdentifier>();
                    for (int j = 0; j < assetInfo.includedObjects.Count; j++)
                    {
                        var index = GetWrapOffsetIndex(j, i, assetInfo.includedObjects.Count);
                        secondaryAssetInfo.includedObjects.Add(assetInfo.includedObjects[index]);
                    }
                    abOp.Info.bundleAssets.Insert(assetInfoIndex + i, secondaryAssetInfo);
                }
            }

            return ReturnCode.Success;
        }
    }
}
