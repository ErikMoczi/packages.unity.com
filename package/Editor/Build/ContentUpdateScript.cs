﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets
{
    /// <summary>
    /// Data stored with each build that is used to generated content updates.
    /// </summary>
    [Serializable]
    public class AddressablesContentState
    {
        /// <summary>
        /// The version that the player was built with.  This is usually set to AddressableAssetSettings.PlayerBuildVersion.
        /// </summary>
        [FormerlySerializedAs("m_playerVersion")]
        [SerializeField]
        public string playerVersion;
        /// <summary>
        /// The version of the unity editor used to build the player.
        /// </summary>
        [FormerlySerializedAs("m_editorVersion")]
        [SerializeField]
        public string editorVersion;
        /// <summary>
        /// Dependency information for all assets in the build that have been marked StaticContent.
        /// </summary>
        [FormerlySerializedAs("m_cachedInfos")]
        [SerializeField]
        public CachedInfo[] cachedInfos;
    }

    /// <summary>
    /// Contains methods used for the content update workflow.
    /// </summary>
    public static class ContentUpdateScript
    {
        /// <summary>
        /// Save the content update information for a set of AddressableAssetEntry objects.
        /// </summary>
        /// <param name="path">File to write content stat info to.  If file already exists, it will be deleted before the new file is created.</param>
        /// <param name="entries">The entries to save.</param>
        /// <param name="buildCache">The cache dependency information generated from the build.</param>
        /// <param name="playerVersion">The player version to save. This is usually set to AddressableAssetSettings.PlayerBuildVersion.</param>
        /// <returns>True if the file is saved, false otherwise.</returns>
        public static bool SaveContentState(string path, List<AddressableAssetEntry> entries, IBuildCache buildCache, string playerVersion)
        {
            try
            {
                var cacheEntries = new List<CacheEntry>();
                foreach (var entry in entries)
                {
                    GUID guid;
                    if (GUID.TryParse(entry.guid, out guid))
                    {
                        var cacheEntry = buildCache.GetCacheEntry(guid);
                        if (cacheEntry.IsValid())
                            cacheEntries.Add(cacheEntry);
                    }
                }
                IList<CachedInfo> cachedInfos;
                buildCache.LoadCachedData(cacheEntries, out cachedInfos);
                var cacheData = new AddressablesContentState { cachedInfos = cachedInfos.ToArray(), playerVersion = playerVersion, editorVersion = Application.unityVersion };
                var formatter = new BinaryFormatter();
                if (File.Exists(path))
                    File.Delete(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                formatter.Serialize(stream, cacheData);
                stream.Flush();
                stream.Close();
                stream.Dispose();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>
        /// [Obsolete] Save the content update information for a set of AddressableAssetEntry objects.
        /// </summary>
        /// <param name="entries">The entries to save.</param>
        /// <param name="buildCache">The cache dependency information generated from the build.</param>
        /// <param name="playerVersion">The player version to save. This is usually set to AddressableAssetSettings.PlayerBuildVersion.</param>
        /// <returns></returns>
        [Obsolete("Use SaveContentState instead. (UnityUpgradable) -> SaveContentState")]
        public static string SaveCacheData(List<AddressableAssetEntry> entries, IBuildCache buildCache, string playerVersion)
        {
            var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/addressables_content_state.bin";
            return SaveContentState(tempPath, entries, buildCache, playerVersion) ? tempPath : "";
        }


        /// <summary>
        /// [obsolete] Gets the path of the cache data from a selected build.
        /// </summary>
        /// <param name="browse">If true, the user is allowed to browse for a specific file.</param>
        /// <returns></returns>
        [Obsolete("Use GetContentStateDataPath instead. (UnityUpgradable) -> GetContentStateDataPath")]
        public static string GetCacheDataPath(bool browse)
        {
            return GetContentStateDataPath(browse);
        }

        /// <summary>
        /// Gets the path of the cache data from a selected build.
        /// </summary>
        /// <param name="browse">If true, the user is allowed to browse for a specific file.</param>
        /// <returns></returns>
        public static string GetContentStateDataPath(bool browse)
        {
            var buildPath = EditorUserBuildSettings.GetBuildLocation(EditorUserBuildSettings.activeBuildTarget);
            if (File.Exists(buildPath))
                buildPath = Path.GetDirectoryName(buildPath);
            #if UNITY_EDITOR_OSX
                if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS || 
                   EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX)
                    buildPath = Path.GetDirectoryName(buildPath);
            #endif
            if (browse)
            {
                if (string.IsNullOrEmpty(buildPath))
                    buildPath = Application.dataPath;

                buildPath = EditorUtility.OpenFilePanel("Build Data File", Path.GetDirectoryName(buildPath), "bin");

                if (string.IsNullOrEmpty(buildPath))
                    return null;

                return buildPath;
            }

            if (string.IsNullOrEmpty(buildPath))
                buildPath = Application.streamingAssetsPath;
            var path = Path.Combine(buildPath, "addressables_content_state.bin");
            return path;
        }

        /// <summary>
        /// Loads cache data from a specific location
        /// </summary>
        /// <param name="contentStateDataPath"></param>
        /// <returns></returns>
        public static AddressablesContentState LoadCacheData(string contentStateDataPath)
        {
            return LoadContentState(contentStateDataPath);
        }

        /// <summary>
        /// Loads cache data from a specific location
        /// </summary>
        /// <param name="contentStateDataPath"></param>
        /// <returns></returns>
        public static AddressablesContentState LoadContentState(string contentStateDataPath)
        {
            if (string.IsNullOrEmpty(contentStateDataPath))
            {
                Debug.LogErrorFormat("Unable to load cache data from {0}.", contentStateDataPath);
                return null;
            }
            var stream = new FileStream(contentStateDataPath, FileMode.Open, FileAccess.Read);
            var formatter = new BinaryFormatter();
            var cacheData = formatter.Deserialize(stream) as AddressablesContentState;
            if (cacheData == null)
            {
                Addressables.LogError("Invalid hash data file.  This file is usually named addressables_content_state.bin and is built into the streaming assets path of a player build.");
                return null;
            }
            return cacheData;
        }

        static bool s_StreamingAssetsExists;
        static string kStreamingAssetsPath = "Assets/StreamingAssets";

        internal static void Cleanup(bool deleteStreamingAssetsFolderIfEmpty)
        {
            if (Directory.Exists(Addressables.BuildPath))
            {
                Directory.Delete(Addressables.BuildPath, true);
                if (File.Exists(Addressables.BuildPath + ".meta"))
                    File.Delete(Addressables.BuildPath + ".meta");
            }
            if (deleteStreamingAssetsFolderIfEmpty)
            {
                if (Directory.Exists(kStreamingAssetsPath))
                {
                    var files = Directory.GetFiles(kStreamingAssetsPath);
                    if (files.Length == 0)
                    {
                        Directory.Delete(kStreamingAssetsPath);
                        if (File.Exists(kStreamingAssetsPath + ".meta"))
                            File.Delete(kStreamingAssetsPath + ".meta");

                    }
                }
            }
        }

        /// <summary>
        /// Builds player content using the player content version from a specified cache file.
        /// </summary>
        /// <param name="settings">The settings object to use for the build.</param>
        /// <param name="contentStateDataPath">The path of the cache data to use.</param>
        /// <returns>The build operation.</returns>
        public static AddressablesPlayerBuildResult BuildContentUpdate(AddressableAssetSettings settings, string contentStateDataPath)
        {
            var cacheData = LoadCacheData(contentStateDataPath);
            if (cacheData == null)
                return null;

            s_StreamingAssetsExists = Directory.Exists("Assets/StreamingAssets");
            var context = new AddressablesBuildDataBuilderContext(settings, 
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
                EditorUserBuildSettings.activeBuildTarget, 
                false, 
                false, 
                cacheData.playerVersion);

            SceneManagerState.Record();
            var buildOp = settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);
            SceneManagerState.Restore();
            Cleanup(!s_StreamingAssetsExists);
            return buildOp;
        }

        internal static List<AddressableAssetEntry> GatherModifiedEntries(AddressableAssetSettings settings, string cacheDataPath)
        {
            var cacheData = LoadCacheData(cacheDataPath);
            if (cacheData == null)
            {
                return null;
            }

            var allEntries = new List<AddressableAssetEntry>();
            settings.GetAllAssets(allEntries, g => g.HasSchema<BundledAssetGroupSchema>() && g.GetSchema<ContentUpdateGroupSchema>().StaticContent);

            var entryToCacheInfo = new Dictionary<string, CachedInfo>();
            foreach (var cacheInfo in cacheData.cachedInfos)
                if (cacheInfo != null)
                    entryToCacheInfo[cacheInfo.Asset.Guid.ToString()] = cacheInfo;
            var modifiedEntries = new List<AddressableAssetEntry>();
            var buildCache = new BuildCache();
            foreach (var entry in allEntries)
            {
                CachedInfo info;
                if (!entryToCacheInfo.TryGetValue(entry.guid, out info) || buildCache.NeedsRebuild(info))
                    modifiedEntries.Add(entry);
            }
            return modifiedEntries;
        }

        /// <summary>
        /// Create a new AddressableAssetGroup with the items and mark it as remote.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        /// <param name="items">The items to move.</param>
        /// <param name="groupName">The name of the new group.</param>
        public static void CreateContentUpdateGroup(AddressableAssetSettings settings, List<AddressableAssetEntry> items, string groupName)
        {
            var contentGroup = settings.CreateGroup(settings.FindUniqueGroupName(groupName), false, false, true, null);
            var schema = contentGroup.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            contentGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = false;
            settings.MoveEntriesToGroup(items, contentGroup);
        }

    }

 
}