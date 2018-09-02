using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.Build.Pipeline
{
    public class LegacyAssetBundleManifest
    {
        Dictionary<string, BundleDetails> m_Details;

        internal LegacyAssetBundleManifest(IBundleBuildResults results)
        {
            m_Details = new Dictionary<string, BundleDetails>(results.BundleInfos);
        }

        public string[] GetAllAssetBundles()
        {
            string[] bundles = m_Details.Keys.ToArray();
            Array.Sort(bundles);
            return bundles;
        }

        public string[] GetAllAssetBundlesWithVariant()
        {
            return new string[0];
        }

        public Hash128 GetAssetBundleHash(string assetBundleName)
        {
            BundleDetails details;
            if (m_Details.TryGetValue(assetBundleName, out details))
                return details.Hash;
            return new Hash128();
        }

        public string[] GetDirectDependencies(string assetBundleName)
        {
            return GetAllDependencies(assetBundleName);
        }

        public string[] GetAllDependencies(string assetBundleName)
        {
            BundleDetails details;
            if (m_Details.TryGetValue(assetBundleName, out details))
                return details.Dependencies.ToArray();
            return new string[0];
        }
    }
}