﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Default implementation of the Build Cache
    /// </summary>
    public class BuildCache : IBuildCache, IDisposable
    {
        const string k_CachePath = "Library/BuildCache";

        Dictionary<KeyValuePair<GUID, int>, CacheEntry> m_GuidToHash = new Dictionary<KeyValuePair<GUID, int>, CacheEntry>();
        Dictionary<KeyValuePair<string, int>, CacheEntry> m_PathToHash = new Dictionary<KeyValuePair<string, int>, CacheEntry>();

        Thread m_ActiveWriteThread;

        [NonSerialized]
        Hash128 m_GlobalHash;

        [NonSerialized]
        CacheServerUploader m_Uploader;

        [NonSerialized]
        CacheServerDownloader m_Downloader;

        public BuildCache()
        {
            m_GlobalHash = CalculateGlobalArtifactVersionHash();
        }

        public BuildCache(string host, int port = 8126)
        {
            m_GlobalHash = CalculateGlobalArtifactVersionHash();

            if (string.IsNullOrEmpty(host))
                return;

            m_Uploader = new CacheServerUploader(host, port);
            m_Downloader = new CacheServerDownloader(this, host, port);
        }

        // internal for testing purposes only
        internal void OverrideGlobalHash(Hash128 hash)
        {
            m_GlobalHash = hash;
            if (m_Uploader != null)
                m_Uploader.SetGlobalHash(m_GlobalHash);
            if (m_Downloader != null)
                m_Downloader.SetGlobalHash(m_GlobalHash);
        }

        static Hash128 CalculateGlobalArtifactVersionHash()
        {
            return HashingMethods.Calculate(PlayerSettings.scriptingRuntimeVersion, Application.unityVersion).ToHash128();
        }

        internal void ClearCacheEntryMaps()
        {
            m_GuidToHash.Clear();
            m_PathToHash.Clear();
        }

        public void Dispose()
        {
            SyncPendingSaves();
            if (m_Uploader != null)
                m_Uploader.Dispose();
            if (m_Downloader != null)
                m_Downloader.Dispose();
            m_Uploader = null;
            m_Downloader = null;
        }

        /// <inheritdoc />
        public CacheEntry GetCacheEntry(GUID asset, int version = 1)
        {
            CacheEntry entry;
            KeyValuePair<GUID, int> key = new KeyValuePair<GUID, int>(asset, version);
            if (m_GuidToHash.TryGetValue(key, out entry))
                return entry;

            entry = new CacheEntry { Guid = asset, Version = version };
            string path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            entry.Type = CacheEntry.EntryType.Asset;

            if (path.Equals(CommonStrings.UnityBuiltInExtraPath, StringComparison.OrdinalIgnoreCase) || path.Equals(CommonStrings.UnityDefaultResourcePath, StringComparison.OrdinalIgnoreCase))
                entry.Hash = HashingMethods.Calculate(Application.unityVersion, path).ToHash128();
            else
            {
                entry.Hash = AssetDatabase.GetAssetDependencyHash(path);
                if (!entry.Hash.isValid)
                    entry.Hash = HashingMethods.CalculateFile(path).ToHash128();
            }

            entry.Hash = HashingMethods.Calculate(entry.Hash, entry.Version).ToHash128();

            m_GuidToHash[key] = entry;
            return entry;
        }

        /// <inheritdoc />
        public CacheEntry GetCacheEntry(string path, int version = 1)
        {
            CacheEntry entry;
            KeyValuePair<string, int> key = new KeyValuePair<string, int>(path, version);
            if (m_PathToHash.TryGetValue(key, out entry))
                return entry;

            entry = new CacheEntry { File = path, Version = version };
            entry.Guid = HashingMethods.Calculate("FileHash", entry.File).ToGUID();
            entry.Hash = HashingMethods.Calculate(HashingMethods.CalculateFile(entry.File), entry.Version).ToHash128();
            entry.Type = CacheEntry.EntryType.File;

            m_PathToHash[key] = entry;
            return entry;
        }

        /// <inheritdoc />
        public CacheEntry GetCacheEntry(ObjectIdentifier objectID, int version = 1)
        {
            if (objectID.guid.Empty())
                return GetCacheEntry(objectID.filePath, version);
            return GetCacheEntry(objectID.guid, version);
        }

        internal CacheEntry GetUpdatedCacheEntry(CacheEntry entry)
        {
            if (entry.Type == CacheEntry.EntryType.File)
                return GetCacheEntry(entry.File, entry.Version);
            if (entry.Type == CacheEntry.EntryType.Asset)
                return GetCacheEntry(entry.Guid, entry.Version);
            return entry;
        }

        /// <inheritdoc />
        public bool HasAssetOrDependencyChanged(CachedInfo info)
        {
            if (info == null || !info.Asset.IsValid() || info.Asset != GetUpdatedCacheEntry(info.Asset))
                return true;

            foreach (var dependency in info.Dependencies)
            {
                if (!dependency.IsValid() || dependency != GetUpdatedCacheEntry(dependency))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public string GetCachedInfoFile(CacheEntry entry)
        {
            var guid = entry.Guid.ToString();
            string finalHash = HashingMethods.Calculate(m_GlobalHash, entry.Hash).ToString();
            return string.Format("{0}/{1}/{2}/{2}_{3}.info", k_CachePath, guid.Substring(0, 2), guid, finalHash);
        }

        /// <inheritdoc />
        public string GetCachedArtifactsDirectory(CacheEntry entry)
        {
            var guid = entry.Guid.ToString();
            string finalHash = HashingMethods.Calculate(m_GlobalHash, entry.Hash).ToString();
            return string.Format("{0}/{1}/{2}", k_CachePath, guid.Substring(0, 2), finalHash);
        }

        class FileOperations
        {
            public FileOperations(int size)
            {
                data = new FileOperation[size];
                waitLock = new Semaphore(0, size);
            }

            public FileOperation[] data;
            public Semaphore waitLock;
        }

        struct FileOperation
        {
            public string file;
            public MemoryStream bytes;
        }

        static void Read(object data)
        {
            var ops = (FileOperations)data;
            for (int index = 0; index < ops.data.Length; index++, ops.waitLock.Release())
            {
                try
                {
                    var op = ops.data[index];
                    if (File.Exists(op.file))
                    {
                        byte[] bytes = File.ReadAllBytes(op.file);
                        if (bytes.Length > 0)
                            op.bytes = new MemoryStream(bytes, false);
                    }
                    ops.data[index] = op;
                }
                catch (Exception e)
                {
                    BuildLogger.LogException(e);
                }
            }
        }

        static void Write(object data)
        {
            var ops = (FileOperations)data;
            for (int index = 0; index < ops.data.Length; index++)
            {
                // Basic spin lock
                ops.waitLock.WaitOne();

                var op = ops.data[index];
                if (op.bytes != null && op.bytes.Length > 0)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(op.file));
                        File.WriteAllBytes(op.file, op.bytes.GetBuffer());
                    }
                    catch (Exception e)
                    {
                        BuildLogger.LogException(e);
                    }
                }
            }
            ((IDisposable)ops.waitLock).Dispose();
        }

        /// <inheritdoc />
        public void LoadCachedData(IList<CacheEntry> entries, out IList<CachedInfo> cachedInfos)
        {
            if (entries == null)
            {
                cachedInfos = null;
                return;
            }

            if (entries.Count == 0)
            {
                cachedInfos = new List<CachedInfo>();
                return;
            }

            // Setup Operations
            var ops = new FileOperations(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                var op = ops.data[i];
                op.file = GetCachedInfoFile(entries[i]);
                ops.data[i] = op;
            }

            // Start file reading
            Thread thread = new Thread(Read);
            thread.Start(ops);

            cachedInfos = new List<CachedInfo>(entries.Count);

            // Deserialize as files finish reading
            var formatter = new BinaryFormatter();
            for (int index = 0; index < entries.Count; index++)
            {
                // Basic wait lock
                ops.waitLock.WaitOne();

                CachedInfo info = null;
                try
                {
                    var op = ops.data[index];
                    if (op.bytes != null && op.bytes.Length > 0)
                        info = formatter.Deserialize(op.bytes) as CachedInfo;
                }
                catch (Exception e)
                {
                    BuildLogger.LogException(e);
                }
                cachedInfos.Add(info);
            }
            thread.Join();
            ((IDisposable)ops.waitLock).Dispose();

            // Validate cached data is reusable
            for (int i = 0; i < cachedInfos.Count; i++)
            {
                if (HasAssetOrDependencyChanged(cachedInfos[i]))
                    cachedInfos[i] = null;
            }

            // If we have a cache server connection, download & check any missing info
            if (m_Downloader != null)
                m_Downloader.DownloadMissing(entries, cachedInfos);

            Assert.AreEqual(entries.Count, cachedInfos.Count);
        }

        /// <inheritdoc />
        public void SaveCachedData(IList<CachedInfo> infos)
        {
            if (infos == null || infos.Count == 0)
                return;

            // Setup Operations
            var ops = new FileOperations(infos.Count);
            for (int i = 0; i < infos.Count; i++)
            {
                var op = ops.data[i];
                op.file = GetCachedInfoFile(infos[i].Asset);
                ops.data[i] = op;
            }

            // Start writing thread
            SyncPendingSaves();
            m_ActiveWriteThread = new Thread(Write);
            m_ActiveWriteThread.Start(ops);

            // Serialize data as previous data is being written out
            var formatter = new BinaryFormatter();
            for (int index = 0; index < infos.Count; index++, ops.waitLock.Release())
            {
                try
                {
                    var op = ops.data[index];
                    var stream = new MemoryStream();
                    formatter.Serialize(stream, infos[index]);
                    if (stream.Length > 0)
                    {
                        op.bytes = stream;
                        ops.data[index] = op;

                        // If we have a cache server connection, upload the cached data
                        if (m_Uploader != null)
                            m_Uploader.QueueUpload(infos[index].Asset, GetCachedArtifactsDirectory(infos[index].Asset), new MemoryStream(stream.GetBuffer(), false));
                    }
                }
                catch (Exception e)
                {
                    BuildLogger.LogException(e);
                }
            }
        }

        internal void SyncPendingSaves()
        {
            if(m_ActiveWriteThread != null)
            {
                m_ActiveWriteThread.Join();
                m_ActiveWriteThread = null;
            }
        }

        [MenuItem("Window/Asset Management/Purge Build Cache", priority = 10)]
        public static void PurgeCache()
        {
            PurgeCache(false);
        }

        public static void PurgeCache(bool prompt)
        {
            if (prompt && !EditorUtility.DisplayDialog("Purge Build Cache", "Do you really want to purge your entire build cache?", "Yes", "No"))
                return;

            if (Directory.Exists(k_CachePath))
                Directory.Delete(k_CachePath, true);
        }
    }
}
