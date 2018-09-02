using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

#if UNITY_2018_3_OR_NEWER
using BuildCompression = UnityEngine.BuildCompression;
#else
using BuildCompression = UnityEditor.Build.Content.BuildCompression;
#endif

namespace UnityEditor.Build.Pipeline.Tasks
{
    public class ArchiveAndCompressBundles : IBuildTask
    {
        public int Version { get { return 1; } }
        
#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In)]
        IBundleWriteData m_WriteData;

        [InjectContext]
        IBundleBuildResults m_Results;

        [InjectContext(ContextUsage.In, true)]
        IProgressTracker m_Tracker;

        [InjectContext(ContextUsage.In, true)]
        IBuildCache m_Cache;
#pragma warning restore 649

        CacheEntry GetCacheEntry(string bundleName, IEnumerable<ResourceFile> resources, BuildCompression compression)
        {
            var entry = new CacheEntry();
            entry.Type = CacheEntry.EntryType.Data;
            entry.Guid = HashingMethods.Calculate("ArchiveAndCompressBundles", bundleName).ToGUID();
            entry.Hash = HashingMethods.Calculate(Version, resources, compression).ToHash128();
            return entry;
        }

        static CachedInfo GetCachedInfo(IBuildCache cache, CacheEntry entry, IEnumerable<ResourceFile> resources, BundleDetails details)
        {
            var info = new CachedInfo();
            info.Asset = entry;

            var dependencies = new HashSet<CacheEntry>();
            foreach (var resource in resources)
                dependencies.Add(cache.GetCacheEntry(resource.fileName));
            info.Dependencies = dependencies.ToArray();

            info.Data = new object[] { details };

            return info;
        }

        static Hash128 CalculateHashVersion(Dictionary<string, ulong> fileOffsets, ResourceFile[] resourceFiles)
        {
            List<RawHash> hashes = new List<RawHash>();
            foreach (ResourceFile file in resourceFiles)
            {
                if (file.serializedFile)
                {
                    // For serialized files, we ignore the header for the hash value. 
                    // This leaves us with a hash value of just the written object data.
                    using (var stream = new FileStream(file.fileName, FileMode.Open))
                    {
                        stream.Position = (long)fileOffsets[file.fileName];
                        hashes.Add(HashingMethods.CalculateStream(stream));
                    }
                }
                else
                    hashes.Add(HashingMethods.CalculateFile(file.fileName));
            }
            return HashingMethods.Calculate(hashes).ToHash128();
        }

        public ReturnCode Run()
        {
            Dictionary<string, ulong> fileOffsets = new Dictionary<string, ulong>();
            List<KeyValuePair<string, List<ResourceFile>>> bundleResources;
            { 
                Dictionary<string, List<ResourceFile>> bundleToResources = new Dictionary<string, List<ResourceFile>>();
                foreach (var pair in m_Results.WriteResults)
                {
                    string bundle = m_WriteData.FileToBundle[pair.Key];
                    List<ResourceFile> resourceFiles;
                    bundleToResources.GetOrAdd(bundle, out resourceFiles);
                    resourceFiles.AddRange(pair.Value.resourceFiles);

                    foreach (ResourceFile serializedFile in pair.Value.resourceFiles)
                    {
                        if (!serializedFile.serializedFile)
                            continue;

                        ObjectSerializedInfo firstObject = pair.Value.serializedObjects.First(x => x.header.fileName == serializedFile.fileAlias);
                        fileOffsets[serializedFile.fileName] = firstObject.header.offset;
                    }
                }
                bundleResources = bundleToResources.ToList();
            }

            IList<CacheEntry> entries = bundleResources.Select(x => GetCacheEntry(x.Key, x.Value, m_Parameters.GetCompressionForIdentifier(x.Key))).ToList();
            IList<CachedInfo> cachedInfo = null;
            IList<CachedInfo> uncachedInfo = null;
            if (m_Parameters.UseCache && m_Cache != null)
            {
                m_Cache.LoadCachedData(entries, out cachedInfo);

                uncachedInfo = new List<CachedInfo>();
            }

            for (int i = 0; i < bundleResources.Count; i++)
            {
                string bundleName = bundleResources[i].Key;
                ResourceFile[] resourceFiles = bundleResources[i].Value.ToArray();
                BuildCompression compression = m_Parameters.GetCompressionForIdentifier(bundleName);

                string writePath;
                BundleDetails details;
                if (cachedInfo != null && cachedInfo[i] != null)
                {
                    if (!m_Tracker.UpdateInfoUnchecked(string.Format("{0} (Cached)", bundleName)))
                        return ReturnCode.Canceled;

                    details = (BundleDetails)cachedInfo[i].Data[0];
                    writePath = string.Format("{0}/{1}", m_Cache.GetCachedArtifactsDirectory(entries[i]), bundleName);
                }
                else
                {
                    if (!m_Tracker.UpdateInfoUnchecked(bundleName))
                        return ReturnCode.Canceled;

                    details = new BundleDetails();
                    writePath = string.Format("{0}/{1}", m_Parameters.TempOutputFolder, bundleName);
                    if (m_Parameters.UseCache && m_Cache != null)
                        writePath = string.Format("{0}/{1}", m_Cache.GetCachedArtifactsDirectory(entries[i]), bundleName);
                    Directory.CreateDirectory(Path.GetDirectoryName(writePath));

                    details.FileName = string.Format("{0}/{1}", m_Parameters.OutputFolder, bundleName);
                    details.Crc = ContentBuildInterface.ArchiveAndCompress(resourceFiles, writePath, compression);
                    details.Hash = CalculateHashVersion(fileOffsets, resourceFiles);

                    if (uncachedInfo != null)
                        uncachedInfo.Add(GetCachedInfo(m_Cache, entries[i], resourceFiles, details));
                }
                
                SetOutputInformation(writePath, details.FileName, bundleName, details);
            }
            
            if (m_Parameters.UseCache && m_Cache != null)
                m_Cache.SaveCachedData(uncachedInfo);

            return ReturnCode.Success;
        }

        void SetOutputInformation(string writePath, string finalPath, string bundleName, BundleDetails details)
        {
            if (finalPath != writePath)
            {
                var directory = Path.GetDirectoryName(finalPath);
                Directory.CreateDirectory(directory);
                File.Copy(writePath, finalPath, true);
            }
            m_Results.BundleInfos.Add(bundleName, details);
        }
    }
}
