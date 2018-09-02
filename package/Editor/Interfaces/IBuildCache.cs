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
        /// Returns the relative directory path where dependency data can be cached for a specified cacheEntry.
        /// </summary>
        /// <param name="cacheEntry">Valid CacheEntry to get directory path.</param>
        /// <returns>Relative directory path.</returns>
        string GetDependencyCacheDirectory(CacheEntry cacheEntry);
        
        /// <summary>
        /// Returns the relative directory path where artifact data can be cached for a specified cacheEntry.
        /// </summary>
        /// <param name="cacheEntry">Valid CacheEntry to get directory path.</param>
        /// <returns>Relative directory path.</returns>
        string GetArtifactCacheDirectory(CacheEntry cacheEntry);

        /// <summary>
        /// Gets a CacheEntry for an asset identified by its GUID.
        /// </summary>
        /// <param name="asset">GUID identifier for an asset from the Asset Database</param>
        /// <returns>CacheEntry representing current asset.</returns>
        CacheEntry GetCacheEntry(GUID asset);

        /// <summary>
        /// Validates a cacheEntry and its dependencies.
        /// </summary>
        /// <param name="cacheEntry">The entry in the cache to validate.</param>
        /// <returns><c>true</c> if the cacheEntry is valid; otherwise, <c>false</c>.</returns>
        bool IsCacheEntryValid(CacheEntry cacheEntry);

        /// <summary>
        /// Tries to load from the cache generated dependency and usage data for a specified Assets's cacheEntry.
        /// </summary>
        /// <param name="cacheEntry">The entry to load dependency data.</param>
        /// <param name="info">The Asset's generated dependency data to load. Parameter will not be chanced if data was unable to be loaded.</param>
        /// <param name="usage">The Asset's generated usage data to load. Parameter will not be chanced if data was unable to be loaded.</param>
        /// <returns><c>true</c> if the cache was able to load the specified data; otherwise, <c>false</c>.</returns>
        bool TryLoadFromCache(CacheEntry cacheEntry, ref AssetLoadInfo info, ref BuildUsageTagSet usage);

        /// <summary>
        /// Tries to load from the cache generated dependency and usage data for a specified Scene's cacheEntry.
        /// </summary>
        /// <param name="cacheEntry">The entry to load dependency data.</param>
        /// <param name="info">The Scene's generated dependency data to load.</param>
        /// <param name="usage">The Scene's generated usage data to load.</param>
        /// <returns><c>true</c> if the cache was able to load the specified data; otherwise, <c>false</c>.</returns>
        bool TryLoadFromCache(CacheEntry cacheEntry, ref SceneDependencyInfo info, ref BuildUsageTagSet usage);

        /// <summary>
        /// Tries to load from the cache generated data for a specified cacheEntry.
        /// </summary>
        /// <typeparam name="T">The type of results data to load.</typeparam>
        /// <param name="cacheEntry">The entry to load data.</param>
        /// <param name="results">The generated data to load.</param>
        /// <returns><c>true</c> if the cache was able to load the specified data; otherwise, <c>false</c>.</returns>
        bool TryLoadFromCache<T>(CacheEntry cacheEntry, ref T results);

        /// <summary>
        /// Tries to load from the cache generated data for a specified cacheEntry.
        /// </summary>
        /// <typeparam name="T1">The first type of results data to load.</typeparam>
        /// <typeparam name="T2">The second type of results data to cache.</typeparam>
        /// <param name="cacheEntry">The entry to load data.</param>
        /// <param name="results1">The first generated data to load.</param>
        /// <param name="results2">The second generated data to load.</param>
        /// <returns><c>true</c> if the cache was able to load the specified data; otherwise, <c>false</c>.</returns>
        bool TryLoadFromCache<T1, T2>(CacheEntry cacheEntry, ref T1 results1, ref T2 results2);
        
        /// <summary>
        /// Tries to cache an Asset's cacheEntry and its generated dependency and usage data.
        /// </summary>
        /// <param name="cacheEntry">The entry for caching dependency data.</param>
        /// <param name="info">The Asset's generated dependency data to cache.</param>
        /// <param name="usage">The Asset's generated usage data to cache.</param>
        /// <returns><c>true</c> if the cache was able to save the specified data; otherwise, <c>false</c>.</returns>
        bool TrySaveToCache(CacheEntry cacheEntry, AssetLoadInfo info, BuildUsageTagSet usage);
        
        /// <summary>
        /// Tries to cache a Scene's cacheEntry and its generated dependency and usage data.
        /// </summary>
        /// <param name="cacheEntry">The entry for caching dependency data.</param>
        /// <param name="info">The Scene's generated dependency data to cache.</param>
        /// <param name="usage">The Scene's generated usage data to cache.</param>
        /// <returns><c>true</c> if the cache was able to save the specified data; otherwise, <c>false</c>.</returns>
        bool TrySaveToCache(CacheEntry cacheEntry, SceneDependencyInfo info, BuildUsageTagSet usage);
        
        /// <summary>
        /// Tries to cache generated data for the specified cacheEntry.
        /// </summary>
        /// <typeparam name="T">The type of results data to cache.</typeparam>
        /// <param name="cacheEntry">The entry for caching data.</param>
        /// <param name="results">The generated data to cache.</param>
        /// <returns><c>true</c> if the cache was able to save the specified data; otherwise, <c>false</c>.</returns>
        bool TrySaveToCache<T>(CacheEntry cacheEntry, T results);
        
        /// <summary>
        /// Tries to cache generated data for the specified cacheEntry.
        /// </summary>
        /// <typeparam name="T1">The first type of results data to cache.</typeparam>
        /// <typeparam name="T2">The second type of results data to cache.</typeparam>
        /// <param name="cacheEntry">The entry for caching data.</param>
        /// <param name="results1">The first generated data to cache.</param>
        /// <param name="results2">The second generated data to cache.</param>
        /// <returns><c>true</c> if the cache was able to save the specified data; otherwise, <c>false</c>.</returns>
        bool TrySaveToCache<T1, T2>(CacheEntry cacheEntry, T1 results1, T2 results2);
    }
}
