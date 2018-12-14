

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Unity.Tiny
{
    /// <summary>
    /// Helper class to detect on disc changes to assets
    ///
    /// This class works very closely with the Persistence.PostProcessor (these can potentially be merged)
    ///
    /// The change detection for this class is based on asset paths and asset guids
    /// </summary>
    internal static class TinyAssetWatcher
    {
        public struct Changes
        {
            public bool changesDetected;
            public HashSet<string> createdSources;
            public HashSet<string> changedSources;
            public HashSet<string> deletedSources;
            public HashSet<string> movedSources;
        }
        
        private static bool s_DeletionDetected;
        private static readonly HashSet<string> s_CreatedAssetPaths = new HashSet<string>();
        private static readonly HashSet<string> s_ChangedAssetPaths = new HashSet<string>();
        private static readonly HashSet<string> s_MovedAssetPaths = new HashSet<string>();

        /// <summary>
        /// Invoked by the Persistence.PostProcessor
        ///
        /// This is used to flag the asset as being changed on disc to allow the user to respond/trigger reload
        /// </summary>
        /// <param name="assetPath"></param>
        public static void MarkChanged(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }
            
            if (File.Exists(assetPath))
            {
                s_ChangedAssetPaths.Add(assetPath);
            }
            else
            {
                s_DeletionDetected = true;
            }
        }

        /// <summary>
        /// Invoked by the Persistence.PostProcessor
        /// </summary>
        /// <param name="assetPath"></param>
        public static void MarkMoved(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            s_MovedAssetPaths.Add(assetPath);
        }

        /// <summary>
        /// Call this method as necessary to gather persistence changes in the given registry sources.
        /// </summary>
        /// <param name="registry">The registry to test the last changes against.</param>
        /// <param name="clearChanges">Set to false if you want to test the last state against multiple registries. Set to true when testing the last registry.</param>
        /// <returns>A RefreshResult struct. changesDetected will be true if any of the detected changes affect the given registry.</returns>
        public static Changes DetectChanges(IRegistry registry, bool clearChanges = true)
        {
            var result = new Changes
            {
                changesDetected = s_DeletionDetected || s_ChangedAssetPaths.Count > 0 || s_MovedAssetPaths.Count > 0 || s_CreatedAssetPaths.Count > 0
            };
            
            if (!result.changesDetected)
            {
                return result;
            }

            var createdSources = new HashSet<string>();
            var changedSources = new HashSet<string>();
            var deletedSources = new HashSet<string>();
            var movedSources = new HashSet<string>();
            
            // gather new modules even when not referenced in the registry
            foreach (var path in s_ChangedAssetPaths)
            {
                if (!(path.EndsWith(Persistence.ModuleFileExtension) || 
                      path.EndsWith(Persistence.EntityGroupFileExtension)))
                {
                    continue;
                }

                if (File.Exists(path))
                {
                    changedSources.Add(AssetDatabase.AssetPathToGUID(path));
                }
            }
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            
            // capture any deleted assets
            foreach (var obj in registry.FindAllByType<IPersistentObject>().Where(obj => !string.IsNullOrEmpty(obj.PersistenceId)))
            {
                var path = AssetDatabase.GUIDToAssetPath(obj.PersistenceId);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                
                if (!asset || null == asset)
                {
                    deletedSources.Add(obj.PersistenceId);
                }  
            }

            var objects = registry.FindAllByType<IPersistentObject>().ToList();

            foreach (var path in s_ChangedAssetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (objects.Any(obj => string.Equals(obj.PersistenceId, guid)))
                {
                    changedSources.Add(guid);
                }
                else
                {
                    createdSources.Add(guid);
                }
            }

            foreach (var path in s_CreatedAssetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                createdSources.Add(guid);
            }

            foreach (var path in s_MovedAssetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                movedSources.Add(guid);
            }

            result.createdSources = createdSources;
            result.changedSources = changedSources;
            result.deletedSources = deletedSources;
            result.movedSources = movedSources;

            if (clearChanges)
            {
                ClearChanges();
            }

            return result;
        }

        /// <summary>
        /// Clear all detected changes
        /// </summary>
        public static void ClearChanges()
        {
            s_DeletionDetected = false;
            s_ChangedAssetPaths.Clear();
            s_MovedAssetPaths.Clear();
            s_CreatedAssetPaths.Clear();
        }

        /// <summary>
        /// Clear detected changes on the given file path
        /// </summary>
        public static void RemoveChanged(string path)
        {
            s_ChangedAssetPaths.Remove(path);
        }

        /// <summary>
        /// Marks this asset as being created
        /// </summary>
        /// <param name="path"></param>
        public static void MarkCreated(string path)
        {
            s_CreatedAssetPaths.Add(path);
        }
    }
}

