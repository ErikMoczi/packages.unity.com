using System;
using System.IO;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.AI.Planner.Utility
{
     static class AssetDatabaseUtility
    {
        public static bool IsEditable(string assetPath)
        {
            var attributes = File.GetAttributes(assetPath);

            return AssetDatabase.IsOpenForEdit(assetPath, StatusQueryOptions.ForceUpdate)
                && (attributes & FileAttributes.ReadOnly) == 0;
        }

        public static string GUIDFromObject(UnityObject @object)
        {
            var assetPath = AssetDatabase.GetAssetPath(@object);
            return AssetDatabase.AssetPathToGUID(assetPath);
        }
    }
}

