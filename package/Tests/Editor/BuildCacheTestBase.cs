﻿using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tests
{
    [TestFixture]
    abstract internal class BuildCacheTestBase
    {
        protected const string kBuildCacheTestPath = "Assets/BuildCacheTestAssets";
        protected string kTestfile1 { get { return Path.Combine(kBuildCacheTestPath, "testfile1.txt"); } }
        protected string kUncachedTestFilename { get { return Path.Combine(kBuildCacheTestPath, "uncached.txt"); } }
        protected GUID Textfile1GUID { get { return new GUID(AssetDatabase.AssetPathToGUID(kTestfile1)); } }
        protected GUID UncachedGUID { get { return new GUID(AssetDatabase.AssetPathToGUID(kUncachedTestFilename)); } }
        protected BuildCache m_Cache;

        internal virtual void OneTimeSetupDerived() { }
        internal virtual void OneTimeTearDownDerived() { }
        internal virtual void SetupDerived() { }
        internal virtual void TeardownDerived() { }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Directory.CreateDirectory(kBuildCacheTestPath);
            File.WriteAllText(kTestfile1, "t1");
            File.WriteAllText(kUncachedTestFilename, "uncached");
            AssetDatabase.Refresh();
            OneTimeSetupDerived();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Directory.Delete(kBuildCacheTestPath, true);
            File.Delete(kBuildCacheTestPath + ".meta");
            AssetDatabase.Refresh();
            OneTimeTearDownDerived();
        }

        [SetUp]
        public void Setup()
        {
            BuildCache.PurgeCache(false);
            RecreateBuildCache();
            SetupDerived();
        }

        [TearDown]
        public void TearDown()
        {
            m_Cache.Dispose();
            m_Cache = null;
            TeardownDerived();
        }

        protected virtual void RecreateBuildCache()
        {
            if (m_Cache != null)
                m_Cache.Dispose();
            m_Cache = new BuildCache();
        }

        static protected CachedInfo LoadCachedInfoForGUID(BuildCache cache, GUID guid)
        {
            IList<CachedInfo> infos;
            CacheEntry entry1 = cache.GetCacheEntry(guid);
            cache.LoadCachedData(new List<CacheEntry>() { entry1 }, out infos);
            return infos[0];
        }

        static protected void StoreDataInCacheWithGUID(BuildCache cache, GUID guid, object data, GUID depGUID = new GUID())
        {
            List<CacheEntry> deps = new List<CacheEntry>();
            if (!depGUID.Empty())
                deps.Add(cache.GetCacheEntry(depGUID));

            CacheEntry entry1 = cache.GetCacheEntry(guid);
            CachedInfo info = new CachedInfo();
            info.Asset = entry1;
            info.Dependencies = deps.ToArray();
            info.Data = new object[] { data };
            cache.SaveCachedData(new List<CachedInfo>() { info });
            cache.SyncPendingSaves();
        }

        static protected GUID CreateTestTextAsset(string contents)
        {
            string filename;
            return CreateTestTextAsset(contents, out filename);
        }

        static protected GUID CreateTestTextAsset(string contents, out string filename)
        {
            int fileIndex = 0;
            while (true)
            {
                filename = Path.Combine(kBuildCacheTestPath, "testasset" + fileIndex);
                if (!File.Exists(filename))
                {
                    File.WriteAllText(filename, contents);
                    AssetDatabase.Refresh();
                    return new GUID(AssetDatabase.AssetPathToGUID(filename));
                }
                fileIndex++;
            }
        }

        static protected void ModifyTestTextAsset(GUID guid, string text)
        {
            string filename = AssetDatabase.GUIDToAssetPath(guid.ToString());
            File.WriteAllText(filename, text);
            AssetDatabase.ImportAsset(filename);
        }

        [Test]
        public void WhenLoadingCachedDataForGUIDWithModifiedDependency_CachedInfoIsNull()
        {
            GUID depGuid = CreateTestTextAsset("mytext");
            StoreDataInCacheWithGUID(m_Cache, Textfile1GUID, "data", depGuid);
            ModifyTestTextAsset(depGuid, "mytext2");
            RecreateBuildCache();
            CachedInfo info = LoadCachedInfoForGUID(m_Cache, Textfile1GUID);
            Assert.IsNull(info);
        }

        [Test]
        public void WhenLoadingCachedDataForModifiedGUID_CachedInfoIsNull()
        {
            GUID guid = CreateTestTextAsset("mytext");
            StoreDataInCacheWithGUID(m_Cache, guid, "data");
            ModifyTestTextAsset(guid, "mytext2");
            RecreateBuildCache();
            CachedInfo info = LoadCachedInfoForGUID(m_Cache, guid);
            Assert.IsNull(info);
        }

        [Test]
        public void WhenLoadingStoredCachedData_CachedInfoIsValid()
        {
            StoreDataInCacheWithGUID(m_Cache, Textfile1GUID, "data");
            RecreateBuildCache();
            CachedInfo info = LoadCachedInfoForGUID(m_Cache, Textfile1GUID);
            Assert.AreEqual("data", (string)info.Data[0]);
        }

        [Test]
        public void WhenLoadingUncachedData_CachedInfoIsNull()
        {
            CachedInfo info = LoadCachedInfoForGUID(m_Cache, UncachedGUID);
            Assert.IsNull(info);
        }

        [Test]
        public void WhenGlobalVersionChanges_OriginalCachedInfoDoesNotNeedRebuild()
        {
            m_Cache.OverrideGlobalHash(new Hash128(0, 1, 0, 0));

            StoreDataInCacheWithGUID(m_Cache, Textfile1GUID, "data");
            CachedInfo info1 = LoadCachedInfoForGUID(m_Cache, Textfile1GUID);

            RecreateBuildCache();
            m_Cache.OverrideGlobalHash(new Hash128(0, 2, 0, 0));
            CachedInfo info2 = LoadCachedInfoForGUID(m_Cache, Textfile1GUID);

            Assert.IsFalse(m_Cache.HasAssetOrDependencyChanged(info1)); // original info still doesn't need rebuild. Required for content update
            Assert.IsNull(info2); // however the data cannot be recovered
        }

        [Test]
        public void WhenLocalVersionChanges_AssetReturnsDifferentCacheEntry()
        {
            GUID guid = CreateTestTextAsset("mytext");

            var entry1 = m_Cache.GetCacheEntry(guid, 2);
            var entry2 = m_Cache.GetCacheEntry(guid, 4);

            Assert.AreEqual(entry1.Guid, entry2.Guid);
            Assert.AreEqual(entry1.File, entry2.File);
            Assert.AreEqual(entry1.Type, entry2.Type);
            Assert.AreNotEqual(entry1.Version, entry2.Version);
            Assert.AreNotEqual(entry1.Hash, entry2.Hash);
        }

        [Test]
        public void WhenLocalVersionChanges_FileReturnsDifferentCacheEntry()
        {
            string filename;
            CreateTestTextAsset("mytext", out filename);

            var entry1 = m_Cache.GetCacheEntry(filename, 2);
            var entry2 = m_Cache.GetCacheEntry(filename, 4);

            Assert.AreEqual(entry1.Guid, entry2.Guid);
            Assert.AreEqual(entry1.File, entry2.File);
            Assert.AreEqual(entry1.Type, entry2.Type);
            Assert.AreNotEqual(entry1.Version, entry2.Version);
            Assert.AreNotEqual(entry1.Hash, entry2.Hash);
        }

        [Test]
        public void GetUpdatedCacheEntry_ReturnsCacheEntryWithSameVersionAndHash_IfAssetHasNotChanged()
        {
            GUID guid = CreateTestTextAsset("mytext");

            var entry1 = m_Cache.GetCacheEntry(guid, 2);
            m_Cache.ClearCacheEntryMaps();
            var entry2 = m_Cache.GetUpdatedCacheEntry(entry1);

            Assert.AreEqual(entry1.Guid, entry2.Guid);
            Assert.AreEqual(entry1.File, entry2.File);
            Assert.AreEqual(entry1.Type, entry2.Type);
            Assert.AreEqual(entry1.Version, entry2.Version);
            Assert.AreEqual(entry1.Hash, entry2.Hash);
        }

        [Test]
        public void GetUpdatedCacheEntry_ReturnsCacheEntryWithSameVersionAndHash_IfFileHasNotChanged()
        {
            string filename;
            CreateTestTextAsset("mytext", out filename);

            var entry1 = m_Cache.GetCacheEntry(filename, 2);
            m_Cache.ClearCacheEntryMaps();
            var entry2 = m_Cache.GetUpdatedCacheEntry(entry1);

            Assert.AreEqual(entry1.Guid, entry2.Guid);
            Assert.AreEqual(entry1.File, entry2.File);
            Assert.AreEqual(entry1.Type, entry2.Type);
            Assert.AreEqual(entry1.Version, entry2.Version);
            Assert.AreEqual(entry1.Hash, entry2.Hash);
        }
    }
}