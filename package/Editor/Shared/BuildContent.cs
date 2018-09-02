using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build
{
    [Serializable]
    public class BuildContent : IBuildContent
    {
        public List<GUID> Assets { get; private set; }

        public List<GUID> Scenes { get; private set; }

        public BuildContent(IEnumerable<GUID> assets)
        {
            Assets = new List<GUID>();
            Scenes = new List<GUID>();

            foreach (var asset in assets)
            {
                if (ValidationMethods.ValidAsset(asset))
                    Assets.Add(asset);
                else if (ValidationMethods.ValidScene(asset))
                    Scenes.Add(asset);
                else
                    throw new ArgumentException(string.Format("Asset '{0}' is not a valid Asset or Scene.", asset));
            }
        }
    }

    [Serializable]
    public class BundleContent : IBundleContent
    {
        public List<GUID> Assets { get; private set; }

        public List<GUID> Scenes { get; private set; }

        public Dictionary<GUID, string> Addresses { get; private set; }

        public Dictionary<string, List<GUID>> BundleLayout { get; private set; }

        public BundleContent(BuildInput bundleInput)
        {
            Assets = new List<GUID>();
            Scenes = new List<GUID>();
            Addresses = new Dictionary<GUID, string>();
            BundleLayout = new Dictionary<string, List<GUID>>();

            foreach (BuildInput.Definition bundle in bundleInput.definitions)
            {
                List<GUID> guids;
                BundleLayout.GetOrAdd(bundle.assetBundleName, out guids);
                foreach (var assetInfo in bundle.explicitAssets)
                {
                    guids.Add(assetInfo.asset);
                    Addresses.Add(assetInfo.asset, string.IsNullOrEmpty(assetInfo.address) ? AssetDatabase.GUIDToAssetPath(assetInfo.asset.ToString()) : assetInfo.address);
                    if (ValidationMethods.ValidAsset(assetInfo.asset))
                        Assets.Add(assetInfo.asset);
                    else if (ValidationMethods.ValidScene(assetInfo.asset))
                        Scenes.Add(assetInfo.asset);
                    else
                        throw new ArgumentException(string.Format("Asset '{0}' is not a valid Asset or Scene.", assetInfo.asset));
                }
            }
        }

        public BundleContent(IEnumerable<AssetBundleBuild> bundleBuilds)
        {
            Assets = new List<GUID>();
            Scenes = new List<GUID>();
            Addresses = new Dictionary<GUID, string>();
            BundleLayout = new Dictionary<string, List<GUID>>();

            foreach (var bundleBuild in bundleBuilds)
            {
                List<GUID> guids;
                BundleLayout.GetOrAdd(bundleBuild.assetBundleName, out guids);

                for (var i = 0; i < bundleBuild.assetNames.Length; i++)
                {
                    var asset = new GUID(AssetDatabase.AssetPathToGUID(bundleBuild.assetNames[i]));
                    guids.Add(asset);
                    var address = i >= bundleBuild.addressableNames.Length || string.IsNullOrEmpty(bundleBuild.addressableNames[i]) ?
                        AssetDatabase.GUIDToAssetPath(asset.ToString()) : bundleBuild.addressableNames[i];
                    Addresses.Add(asset, address);
                    if (ValidationMethods.ValidAsset(asset))
                        Assets.Add(asset);
                    else if (ValidationMethods.ValidScene(asset))
                        Scenes.Add(asset);
                    else
                        throw new ArgumentException(string.Format("Asset '{0}' is not a valid Asset or Scene.", bundleBuild.assetNames[i]));
                }
            }
        }
    }
}