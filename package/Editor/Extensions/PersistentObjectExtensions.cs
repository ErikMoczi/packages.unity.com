using System.IO;
using UnityEditor;

namespace Unity.Tiny
{
    internal static class PersistentObjectExtensions
    {
        /// <summary>
        /// Returns the directory path the persistent object is in
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetDirectoryPath(this IPersistentObject obj)
        {
            var assetGuid = Persistence.GetAssetGuidFromTinyGuid(obj.Id);
            var path = AssetDatabase.GUIDToAssetPath(assetGuid);
            return string.IsNullOrEmpty(path) ? null : new FileInfo(path).Directory?.FullName;
        }

        /// <summary>
        /// Returns the directory guid the persistent object is in
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetDirectoryGuid(this IPersistentObject obj)
        {
            var path = obj.GetDirectoryPath();
            var assetPath = Persistence.GetPathRelativeToProjectPath(path);
            return AssetDatabase.AssetPathToGUID(assetPath);
        }
    }
}