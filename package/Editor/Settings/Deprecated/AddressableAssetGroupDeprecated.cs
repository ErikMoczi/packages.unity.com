using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
namespace UnityEditor.AddressableAssets
{
    //TODO: deprecate and remove once most users have transitioned to newer external data files
    [Serializable]
    internal class AddressableAssetGroupDeprecated
    {
        [SerializeField]
        public string m_name;
        [SerializeField]
        public KeyDataStore m_data = new KeyDataStore();
        [SerializeField]
        public string m_guid;
        [SerializeField]
        public string m_processorAssembly;
        [SerializeField]
        public string m_processorClass;
        [SerializeField]
        public List<AddressableAssetEntry> m_serializeEntries = new List<AddressableAssetEntry>();
        [SerializeField]
        public bool m_readOnly;
    }

    internal static class AddressableAssetGroupDeprecationExtensions
    {
        public static void ConvertDeprecatedGroupData(this AddressableAssetSettings settings, AddressableAssetGroupDeprecated old, bool staticContent)
        {
            string validName = settings.FindUniqueGroupName(old.m_name);
            var group = ScriptableObject.CreateInstance<AddressableAssetGroup>();
            group.Initialize(settings, validName, old);
            group.StaticContent = staticContent;
            if (!Directory.Exists(settings.GroupFolder))
                Directory.CreateDirectory(settings.GroupFolder);
            var groupAssetPath = settings.GroupFolder + "/" + validName + ".asset";
            AssetDatabase.CreateAsset(group, groupAssetPath);
            settings.groups.Add(AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(groupAssetPath));
        }
    }

}
