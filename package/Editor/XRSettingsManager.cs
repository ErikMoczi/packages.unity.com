using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace UnityEditor.XR.Management
{
    public class XRSettingsManager : SettingsProvider {

        private static XRSettingsManager s_SettingsManager = null;

        public XRSettingsManager(string path, SettingsScopes scopes = SettingsScopes.Any) : base(path, scopes)
        {

        }

        internal struct AssetBuildData
        {
            internal string BuildDataName;
            internal string AssetPath;
        }

        private static List<AssetBuildData> FindAllBuildDataInstances()
        {
            List<AssetBuildData> ret = new List<AssetBuildData>();

            var assetGUIDs = AssetDatabase.FindAssets("t:XRBuildData");
            foreach (var assetGUID in assetGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGUID);
                var obj = AssetDatabase.LoadAssetAtPath<XRBuildData>(path);
                if (obj != null)
                {
                    ret.Add(new AssetBuildData(){ BuildDataName = obj.Name, AssetPath = path});
                }
            }

            return ret;
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            if (s_SettingsManager == null)
            {
                s_SettingsManager = new XRSettingsManager("XR");
            }

            return s_SettingsManager;
        }

        [SettingsProviderGroup]
        public static SettingsProvider[] CreateAllChildSettingsProviders()
        {
            List<SettingsProvider> ret = new List<SettingsProvider>();
            if (s_SettingsManager != null)
            {
                foreach (var buildDataInst in FindAllBuildDataInstances())
                {
                    string settingsPath = String.Format("XR/{0}", buildDataInst.BuildDataName);
                    var resProv = new AssetSettingsProvider(settingsPath, buildDataInst.AssetPath)
                    {
                    };
                    ret.Add(resProv);
                }
            }

            return ret.ToArray();
        }
    }
}
