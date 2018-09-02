using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.WriteTypes;

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class GenerateSpritePathMaps : IBuildTask
    {
        public int Version { get { return 1; } }

#pragma warning disable 649
        [InjectContext]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In, true)]
        IBuildSpriteData m_SpriteData;
#pragma warning restore 649

        static int GetWrapOffsetIndex(int index, int offset, int max)
        {
            return (index + offset) % max;
        }

        public ReturnCode Run()
        {
            if (m_SpriteData == null || m_SpriteData.ImporterData.Count == 0)
                return ReturnCode.SuccessNotRun;

            Dictionary<string, IWriteOperation> fileToOperation = m_WriteData.WriteOperations.ToDictionary(x => x.Command.internalName, x => x);
            foreach (GUID asset in m_SpriteData.ImporterData.Keys)
            {
                string mainFile = m_WriteData.AssetToFiles[asset][0];
                var abOp = fileToOperation[mainFile] as AssetBundleWriteOperation;

                int assetInfoIndex = abOp.Info.bundleAssets.FindIndex(x => x.asset == asset);
                AssetLoadInfo assetInfo = abOp.Info.bundleAssets[assetInfoIndex];
                for (int i = 1; i < assetInfo.includedObjects.Count; i++)
                {
                    var secondaryAssetInfo = new AssetLoadInfo();
                    secondaryAssetInfo.asset = assetInfo.asset;
                    secondaryAssetInfo.address = assetInfo.address;
                    secondaryAssetInfo.referencedObjects = assetInfo.referencedObjects;

                    secondaryAssetInfo.includedObjects = new List<ObjectIdentifier>();
                    for (int j = 0; j < assetInfo.includedObjects.Count; j++)
                    {
                        int index = GetWrapOffsetIndex(j, i, assetInfo.includedObjects.Count);
                        secondaryAssetInfo.includedObjects.Add(assetInfo.includedObjects[index]);
                    }
                    abOp.Info.bundleAssets.Insert(assetInfoIndex + i, secondaryAssetInfo);
                }
            }

            return ReturnCode.Success;
        }
    }
}
