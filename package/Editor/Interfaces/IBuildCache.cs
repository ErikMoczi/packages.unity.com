using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline.Interfaces
{
    /// <summary>
    /// Base interface for the Build Caching
    /// </summary>
    public interface IBuildCache : IContextObject
    {
        /// <summary>
        /// Gets a CacheEntry for an asset identified by its GUID.
        /// </summary>
        /// <param name="asset">GUID identifier for an asset from the Asset Database</param>
        /// <returns>CacheEntry representing current asset.</returns>
        CacheEntry GetCacheEntry(GUID asset);
        
        /// <summary>
        /// Gets a CacheEntry for a file identified by its relative path.
        /// </summary>
        /// <param name="path">Relative path of a file on disk</param>
        /// <returns>CacheEntry representing a file on disk.</returns>
        CacheEntry GetCacheEntry(string path);
        
        /// <summary>
        /// Gets a CacheEntry for an object identified by an Object Identifier.
        /// </summary>
        /// <param name="objectID">Object identifier for an object</param>
        /// <returns>CacheEntry representing an object identifier.</returns>
        CacheEntry GetCacheEntry(ObjectIdentifier objectID);

        /// <summary>
        /// Checks if the CachedInfo passed in needs to be rebuilt
        /// </summary>
        /// <param name="info">Cached Info to check</param>
        /// <returns><c>true</c> if the cached info needs to be rebuilt; otherwise, <c>false</c>.</returns>
        bool NeedsRebuild(CachedInfo info);

        /// <summary>
        /// Returns the path where info data can be saved in the cache
        /// </summary>
        /// <param name="entry">Cache entry to get the path</param>
        /// <returns>Path on disk where to save cached info</returns>
        string GetCachedInfoFile(CacheEntry entry);
        
        /// <summary>
        /// Returns the path where artifact data can be saved in the cache
        /// </summary>
        /// <param name="entry">Cache entry to get the path</param>
        /// <returns>Path on disk where to save cached artifacts</returns>
        string GetCachedArtifactsDirectory(CacheEntry entry);

        /// <summary>
        /// Loads a set of CachedInfos from the cache
        /// </summary>
        /// <param name="entries">List of cache entries to load</param>
        /// <param name="cachedInfos">Out list of cached infos loaded</param>
        void LoadCachedData(IList<CacheEntry> entries, out IList<CachedInfo> cachedInfos);

        /// <summary>
        /// Saves a set of CachedInfos to the cache
        /// </summary>
        /// <param name="infos">List of cached infos to save</param>
        void SaveCachedData(IList<CachedInfo> infos);
    }
}
