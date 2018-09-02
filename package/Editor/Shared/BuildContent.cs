using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Basic implementation of IBuildContent. Stores the list of Assets to feed the Scriptable Build Pipeline.
    /// <seealso cref="IBuildContent"/>
    /// </summary>
    [Serializable]
    public class BuildContent : IBuildContent
    {
        /// <inheritdoc />
        public List<GUID> Assets { get; private set; }
        
        /// <inheritdoc />
        public List<GUID> Scenes { get; private set; }

        /// <summary>
        /// Default constructor, takes a set of Assets and converts them to the appropriate properties.
        /// </summary>
        /// <param name="assets">The set of Assets identified by GUID to ensure are packaged with the build</param>
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
    
    /// <summary>
    /// Basic implementation of IBundleBuildContent. Stores the list of Assets with explicit Asset Bundle layout to feed the Scriptable Build Pipeline.
    /// <seealso cref="IBundleBuildContent"/>
    /// </summary>
    [Serializable]
    public class BundleBuildContent : IBundleBuildContent
    {
        /// <inheritdoc />
        public List<GUID> Assets { get; private set; }
        
        /// <inheritdoc />
        public List<GUID> Scenes { get; private set; }
        
        /// <inheritdoc />
        public Dictionary<GUID, string> Addresses { get; private set; }
        
        /// <inheritdoc />
        public Dictionary<string, List<GUID>> BundleLayout { get; private set; }

        /// <summary>
        /// Default constructor, takes a set of AssetBundleBuild and converts them to the appropriate properties.
        /// </summary>
        /// <param name="bundleBuilds">The set of AssetbundleBuild to be built.</param>
        public BundleBuildContent(IEnumerable<AssetBundleBuild> bundleBuilds)
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