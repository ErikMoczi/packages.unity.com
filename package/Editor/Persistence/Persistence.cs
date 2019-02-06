﻿

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace Unity.Tiny
{
    /// <summary>
    /// Utility class to handle saving and loading of `Tiny` object streams
    ///
    /// The asset pipeline for editor assets works like this
    ///
    /// [IMPORT PIPELINE]
    ///     - Tiny assets are stores as a stream of tiny objects is any order
    ///     - Tiny assets are imported using the unity AssetPipeline
    ///     - During the importer we do a quick parse to extract the `Id` field from each top level object
    ///       and store that in the imported `UnityEngine.Object`
    /// 
    /// [EDITOR INITIALIZE]
    ///     - A mapping is built to map any Tiny object to it's corresponding asset <see cref="s_ObjectToAssetGuidMap"/>
    ///     - TODO: build asset dependency map at this stage
    ///
    /// [LOADING]
    ///     - All modules are loaded (@todo optim - load dependent files for a given project only)
    ///     - User loads a project
    ///     - The root persistent object is loaded (project or module)
    ///     - Based on the root object and (@todo user workspace) we load all dependency objects (using the map)
    ///     - The initial version is tracked for each loaded object
    ///
    /// [SAVING]
    ///     - The version is checked to avoid writing unchanged objects (optim)
    ///     - Object is written back to it's source path on disc
    /// 
    /// </summary>
    internal static class Persistence
    {
        internal class PostProcessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] importedAssets, 
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                var tinyPathsToRegister = new List<string>();
                Dictionary<string, string> remap = null;
                
                foreach (var path in importedAssets)
                {
                    if (!EndsWithTinyExtension(path))
                    {
                        continue;
                    }

                    var guid = AssetDatabase.AssetPathToGUID(path);
                    var obj = AssetDatabase.LoadMainAssetAtPath(path) as TinyScriptableObject;

                    if (!obj)
                    {
                        continue;
                    }
                    
                    if (s_AssetGuidToContentHashMap.TryGetValue(guid, out var hash))
                    {
                        if (hash == obj.Hash)
                        {
                            // This object has not changed in any way. No need to process it
                            continue;
                        }
                    }

                    // Always unregister the asset before checking duplicates or re-registering it
                    UnregisterAsset(guid);
                    
                    // Check for any already registered Ids
                    
                    // Query the current active database to see if any of these guids already registered
                    var duplicateIds = obj.Objects.Where(s_ObjectToAssetGuidMap.ContainsKey).ToList();
                    
                    if (duplicateIds.Count > 0)
                    {
                        if (null == remap)
                        {
                            remap = new Dictionary<string, string>();
                        }
                        
                        foreach (var id in duplicateIds)
                        {
                            if (!remap.ContainsKey(id))
                            {
                                remap.Add(id, TinyId.New().ToString());
                            }
                        }
                    }
                    
                    tinyPathsToRegister.Add(path);
                }

                if (null != remap)
                {
                    foreach (var path in tinyPathsToRegister)
                    {
                        RegistryObjectRemap.Remap(AsEnumerable(path), remap);
                    }
                }
                else
                {
                    // Register assets
                    foreach (var path in tinyPathsToRegister)
                    {
                        var guid = AssetDatabase.AssetPathToGUID(path);
                    
                        RegisterAsset(guid);
                    
                        // Change detection
                        TinyAssetWatcher.MarkChanged(path);
                    }
                }
                
                foreach (var path in deletedAssets)
                {
                    if (!EndsWithTinyExtension(path))
                    {
                        continue;
                    }
                    
                    // Untrack the asset
                    UnregisterAsset(AssetDatabase.AssetPathToGUID(path));
                    
                    // Change detection
                    TinyAssetWatcher.MarkChanged(path);
                }

                foreach (var path in movedAssets)
                {
                    if (!EndsWithTinyExtension(path))
                    {
                        continue;
                    }

                    // Change detection
                    TinyAssetWatcher.MarkMoved(path);
                }
            }
        }
        
        internal class PersistTransaction : IDisposable
        {
            private readonly IDictionary<IPersistentObject, string> m_Objects = new Dictionary<IPersistentObject, string>();
        
            public PersistTransaction()
            {
                AssetDatabase.StartAssetEditing();
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public void PersistObjectAs(IPersistentObject obj, string path)
            {
                // Defer asset patching until after the assets are re-imported
                m_Objects.Add(obj, path);
                
                if (!IsPersistentObjectChanged(obj))
                {
                    // Version match, this object is unchanged skip write to disc
                    return;
                }
        
                PersistContainersAs(obj.EnumerateContainers(), path);
                
                // Invoke the pre import hook for the persist operation
                PersistObjectPreImport(obj, path);
            }

            public void Dispose()
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                foreach (var kvp in m_Objects)
                {
                    var obj = kvp.Key;
                    var path = GetPathRelativeToProjectPath(kvp.Value);

                    // Invoke the post import step of the persist operation
                    PersistObjectPostImport(obj, path);
                }
            }
        }
        
        internal class LoadTransaction : IDisposable
        {
            private struct RemapInfo
            {
                public string MainObjectId;
                public string PersistenceId;
                public string Name;
                public bool Changed;
            }
            
            private readonly MemoryStream m_CommandStream = new MemoryStream();
            private readonly IList<RemapInfo> m_PersistenceIdRemap = new List<RemapInfo>();

            /// <summary>
            /// Pushes a load json operation into the transaction
            /// </summary>
            /// <param name="path">File to load</param>
            /// <param name="sourceIdentifier">SourceIdentifier to load under</param>
            /// <param name="persistenceId">PersistenceId to assign to the root object</param>
            public void LoadJson(string path, string sourceIdentifier, string persistenceId)
            {
                // Read and push the data to the command stream
                using (var json = File.OpenRead(path))
                using (var writer = new Serialization.CommandStream.CommandStreamWriter(m_CommandStream, true))
                {
                    writer.PushSourceIdentiferScope(sourceIdentifier);
                    Serialization.Json.JsonFrontEnd.Accept(json, m_CommandStream);
                    writer.PopSourceIdentiferScope();
                }

                RemapPersistenceId(persistenceId, path);
            }

            public void LoadBinary(Stream stream, string sourceIdentifier, string persistenceId, bool changed = false)
            {
                using (var writer = new Serialization.CommandStream.CommandStreamWriter(m_CommandStream, true))
                {
                    writer.PushSourceIdentiferScope(sourceIdentifier);
                    Serialization.Binary.BinaryFrontEnd.Accept(stream, m_CommandStream);
                    writer.PopSourceIdentiferScope();
                }

                RemapPersistenceId(persistenceId, null, changed);
            }

            private void RemapPersistenceId(string persistenceId, string path, bool changed = false)
            {
                if (string.IsNullOrEmpty(persistenceId))
                {
                    return;
                }

                if (!s_AssetGuidToObjectsMap.TryGetValue(persistenceId, out var ids) || ids.Length <= 0)
                {
                    return;
                }

                var mainObjectId = ids.First();

                m_PersistenceIdRemap.Add(new RemapInfo
                {
                    MainObjectId = mainObjectId,
                    PersistenceId = persistenceId,
                    Name = !string.IsNullOrEmpty(path) ? Path.GetFileNameWithoutExtension(path) : string.Empty,
                    Changed = changed
                });
            }

            /// <summary>
            /// Commits all LoadJson operations called on this transaction
            /// </summary>
            /// <param name="registry"></param>
            public void Commit(IRegistry registry)
            {
                // Accept the raw data from the command stream into the registry
                m_CommandStream.Position = 0;
                Serialization.CommandStream.CommandFrontEnd.Accept(m_CommandStream, registry);

                foreach (var type in registry.FindAllByType<TinyType>())
                {
                    type.Refresh();
                }
                
                foreach (var remap in m_PersistenceIdRemap)
                {
                    var mainObject = registry.FindById(new TinyId(remap.MainObjectId)) as IPersistentObject;
                    
                    if (null == mainObject)
                    {
                        continue;
                    }
                    
                    mainObject.PersistenceId = remap.PersistenceId;
                    mainObject.Name = !string.IsNullOrEmpty(remap.Name) ? remap.Name : mainObject.Name;

                    if (!remap.Changed)
                    {
                        RegisterVersions(mainObject);
                    }
                }
            }

            public void Dispose()
            {
                m_CommandStream?.Dispose();
            }
        }

        public const string ProjectFileImporterExtension = "utproject";
        public const string ProjectFileExtension = "." + ProjectFileImporterExtension;
        
        public const string ModuleFileImporterExtension = "utmodule";
        public const string ModuleFileExtension = "." + ModuleFileImporterExtension;
        
        public const string EntityGroupFileImporterExtension = "utdata";
        public const string EntityGroupFileExtension = "." + EntityGroupFileImporterExtension;
        
        public const string PrefabFileImporterExtension = "utprefab";
        public const string PrefabFileExtension = "." + PrefabFileImporterExtension;
        
        public const string TypeFileImporterExtension = "uttype";
        public const string TypeFileExtension = "." + TypeFileImporterExtension;
        
        public const string EntityGroupDirectory = "Entities";
        public const string TypeDirectory = "Components";

        /// <summary>
        /// Returns true if the given path is a `Tiny` object path (i.e. Ends with `ut*`)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool EndsWithTinyExtension(string path)
        {
            return path.EndsWith(ProjectFileExtension) ||
                   path.EndsWith(ModuleFileExtension) ||
                   path.EndsWith(EntityGroupFileExtension) ||
                   path.EndsWith(PrefabFileExtension) ||
                   path.EndsWith(TypeFileExtension);
        }
        
        /// <summary>
        /// Map of `Type` to `Asset Guid` (com.unity.tiny package only)
        /// ASSUMPTION: Package content is immutable, so this can be cached
        /// </summary>
        private static readonly Dictionary<Type, string[]> s_TypeToPackageAssetGuidMap = new Dictionary<Type, string[]>();
        
        /// <summary>
        /// Map of `TinyObjectGuid` to `AssetGuid`
        /// 
        /// Used to lookup which asset to load for any given object
        /// </summary>
        private static readonly Dictionary<string, string> s_ObjectToAssetGuidMap = new Dictionary<string, string>();
        
        /// <summary>
        /// Backmap of `AssetGuid` to `TinyObjectGuids`
        /// 
        /// Used to unregister and re-load assets
        /// </summary>
        private static readonly Dictionary<string, string[]> s_AssetGuidToObjectsMap = new Dictionary<string, string[]>();

        /// <summary>
        /// Mapping of `PersistentObject.Id` to save version
        /// 
        /// Used as to avoid saving unchanged assets
        /// </summary>
        private static readonly Dictionary<TinyId, int> s_VersionMap = new Dictionary<TinyId, int>();
        
        /// <summary>
        /// Mapping of `AssetGuid` to `ContentHash`
        ///
        /// Used to avoid `falsely` flagging assets as changed
        /// </summary>
        private static readonly Dictionary<string, string> s_AssetGuidToContentHashMap = new Dictionary<string, string>();

        /// <summary>
        /// Mapping of root directory guid to main tiny object guid (e.g. Project OR Module)
        /// </summary>
        /// <returns></returns>
        private static readonly Dictionary<string, string> s_DirectoryGuidToMainAssetGuidMap = new Dictionary<string, string>();

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (!UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                // Editor GUI
                EditorApplication.delayCall += InitializeDatabase;
            }
            else
            {
                // command line
                InitializeDatabase();
            }
        }

        private static void InitializeDatabase()
        {
            var guids = FindAllAssetsGuidsOfType(typeof(TinyScriptableObject)).OrderBy(g =>
            {
                // Naive order by creation time
                // @TODO Store the creation time in the meta file
                var assetPath = AssetDatabase.GUIDToAssetPath(g);
                var info = new FileInfo(assetPath);
                return info.CreationTimeUtc.Millisecond;
            });

            foreach (var guid in guids)
            {
                UnregisterAsset(guid);
                RegisterAsset(guid);
            }
        }
        
        /// <summary>
        /// Tracks `TinyScriptableObject` asset and caches it's content guids
        /// </summary>
        private static void RegisterAsset(string guid)
        {
            Assert.IsFalse(string.IsNullOrEmpty(guid));
            
            if (s_AssetGuidToObjectsMap.ContainsKey(guid))
            {
                return;
            }
            
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadMainAssetAtPath(path) as TinyScriptableObject;

            if (!obj)
            {
                return;
            }
            
            Assert.IsNotNull(obj, path);

            if (null == obj.Objects)
            {
                return;
            }
            
            // Check for any already registered Ids
            // This is a case that can happen if a file was duplicated
            var duplicateIds = obj.Objects.Where(s_ObjectToAssetGuidMap.ContainsKey).ToArray();
            
            if (duplicateIds.Length > 0)
            {
                var remap = new Dictionary<string, string>();

                foreach (var id in duplicateIds)
                {
                    remap.Add(id, TinyId.New().ToString());
                }
                
                // Perform automatic remapping of all invalid guids
                // @NOTE This only works for single monolithic files
                //       This does NOT work for the multi-file approach
                RegistryObjectRemap.Remap(AsEnumerable(path), remap);
                return;
            }
            
            s_AssetGuidToContentHashMap.Add(guid, obj.Hash);
            
            // This is a unique set
            // We can safely register all objects
            foreach (var id in obj.Objects)
            {
                if (s_ObjectToAssetGuidMap.ContainsKey(id))
                {
                    continue;
                }
                    
                s_ObjectToAssetGuidMap.Add(id, guid);
            }
            
            s_AssetGuidToObjectsMap.Add(guid, obj.Objects);

            if (obj is UTProject || obj is UTModule)
            {
                // This asset is a top level asset
                // Track its directory as a 'magic' folder where to facilitate adding and removing objects easily
                
                // Get the asset guid for the directory this asset lives in
                var directory = new FileInfo(path).Directory;
                var directoryPath = directory.FullName;
                var directoryAssetPath = GetPathRelativeToProjectPath(directoryPath);
                var directoryGuid = AssetDatabase.AssetPathToGUID(directoryAssetPath);
                
                if (!s_DirectoryGuidToMainAssetGuidMap.ContainsKey(directoryGuid))
                {
                    s_DirectoryGuidToMainAssetGuidMap.Add(directoryGuid, guid);
                }
                else
                {
                    // First registered asset wins in this case
                    // The user is expected to delete or move one of the assets
                    Debug.LogError($"[{TinyConstants.ApplicationName}] Multiple projects reside in the same directory, this is not supported FileName=[{obj.name}]");
                }
            }
        }
        
        /// <summary>
        /// Untracks a `TinyScriptableObject` asset all asociated content guids
        /// </summary>
        private static void UnregisterAsset(string guid)
        {
            Assert.IsFalse(string.IsNullOrEmpty(guid));
            
            string[] objects;
            if (!s_AssetGuidToObjectsMap.TryGetValue(guid, out objects))
            {
                return;
            }
            
            foreach (var key in objects)
            {
                s_ObjectToAssetGuidMap.Remove(key);
            }

            s_AssetGuidToContentHashMap.Remove(guid);
            s_AssetGuidToObjectsMap.Remove(guid);

            // Get the guid of the directory this asset lives in
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var fileInfo = new FileInfo(assetPath);
            var directory = fileInfo.Directory;
            var directoryPath = directory.FullName;
            var directoryAssetPath = GetPathRelativeToProjectPath(directoryPath);
            var directoryGuid = AssetDatabase.AssetPathToGUID(directoryAssetPath);
            
            s_DirectoryGuidToMainAssetGuidMap.Remove(directoryGuid);
        }
        
        /// <summary>
        /// High level save operation on a set of persistent objects
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="objects"></param>
        /// <param name="path"></param>
        public static void SaveObjectsAs(IRegistry registry, IEnumerable<IPersistentObject> objects, string path)
        {
            var fileInfo = new FileInfo(path);
            var directory = fileInfo.Directory;
            
            Assert.IsNotNull(directory);
            
            directory.Create();
            
            var rootObject = objects.First();
            var subObjects = objects.Skip(1).ToList();

            using (var transaction = new PersistTransaction())
            {
                // Save the root persistent object to the project directory
                // @TODO This is a forced save to disc (ignoring the version check optimization)
                //       This is a workaround since changes to the module are not propagated to the project
                transaction.PersistObjectAs(rootObject, path);

                foreach (var obj in subObjects)
                {
                    if (!string.IsNullOrEmpty(obj.PersistenceId))
                    {
                        // This asset has been saved before, save in place using the asset database to resolve the path
                        transaction.PersistObjectAs(obj, GetAssetPath(obj));
                    }
                    else
                    {
                        fileInfo = new FileInfo(Path.Combine(directory.FullName, GetRelativePathForSubAsset(obj)));
                        Assert.IsNotNull(fileInfo.Directory);

                        // Make sure the directory exists
                        fileInfo.Directory.Create();

                        // This is the first time saving this sub-asset (place it in the default directory)
                        transaction.PersistObjectAs(obj, fileInfo.FullName);
                    }
                }
            }

            // Fix-up names after the asset database has finished importing
            foreach (var obj in objects)
            {
                var assetPath = GetAssetPath(obj);
                
                // Fix to get prefab naming to follow when applying
                if (Path.GetFileName(assetPath) == obj.Name)
                {
                    continue;
                }
                
                var result = AssetDatabase.RenameAsset(assetPath, obj.Name);

                if (!string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning(result);
                }
            }

            foreach (var obj in subObjects)
            {
                // Re-register this object under to the `SourceIdentifier`
                registry.ChangeSource(obj.Id, obj.PersistenceId);
            }
        }

        ///  <summary>
        ///  Persists the given object to disc at the specified path
        /// 
        ///  @NOTE This will trigger an AssetImport and update the PersistenceId
        ///  
        ///  </summary>
        ///  <param name="obj">The object to save</param>
        ///  <param name="path">Full path to save to</param>
        /// <param name="forced">Force the save even if the object in unchanged.</param>
        /// <returns>Asset database path</returns>
        public static void PersistObject(IPersistentObject obj, string path, bool forced = false)
        {
            if (!forced && !IsPersistentObjectChanged(obj))
            {
                // Version match, this object is unchanged skip write to disc
                return;
            }
            
            // Persist the content to disc
            PersistContainersAs(obj.EnumerateContainers(), path);

            // Invoke the pre import hook for the persist operation
            PersistObjectPreImport(obj, path);
            
            // Force a synchronous re-import for this asset
            AssetDatabase.ImportAsset(GetPathRelativeToProjectPath(path), ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUncompressedImport);

            // Invoke the post import step of the persist operation
            PersistObjectPostImport(obj, path);
        }
        
        private static void PersistObjectPreImport(IPersistentObject obj, string path)
        {
            if (!string.IsNullOrEmpty(obj.PersistenceId))
            {
                string[] guids;
                if (!s_AssetGuidToObjectsMap.TryGetValue(obj.PersistenceId, out guids))
                {
                    return;
                }

                foreach (var guid in guids)
                {
                    s_VersionMap.Remove(new TinyId(guid));
                }
                
                UnregisterAsset(obj.PersistenceId);
            }
        }
        
        /// <summary>
        /// Performs the necessary patch up after saving and re-importing a persistent object 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        private static void PersistObjectPostImport(IPersistentObject obj, string path)
        {
            var assetPath = GetPathRelativeToProjectPath(path);
                
            // Clear this asset from the changed list
            TinyAssetWatcher.RemoveChanged(assetPath);
            
            if (string.IsNullOrEmpty(obj.PersistenceId))
            {
                // Add this asset to the newly created list for tracking
                TinyAssetWatcher.MarkCreated(assetPath);
            }
            
            // Fixup the persistenceId for this object
            obj.PersistenceId = AssetDatabase.AssetPathToGUID(assetPath);
            obj.Name = Path.GetFileNameWithoutExtension(assetPath);
            
            // Re-register the version
            RegisterAsset(obj.PersistenceId);
            
            s_VersionMap[obj.Id] = obj.Version;
            
            foreach (var o in obj.EnumerateContainers())
            {
                if (!(o is IIdentified<TinyId> identifiable) || !(o is IVersioned versioned))
                {
                    continue;
                }

                s_VersionMap[identifiable.Id] = versioned.Version;
            }
        }

        /// <summary>
        /// Returns true if the persistent object or one of its sub objects is changed
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsPersistentObjectChanged(IPersistentObject obj)
        {
            int version;
            if (!s_VersionMap.TryGetValue(obj.Id, out version) || version != obj.Version)
            {
                // The root object either did not exist before OR its version is different
                return true;
            }

            foreach (var o in obj.EnumerateContainers())
            {
                var identifiable = o as IIdentified<TinyId>;
                var versioned = o as IVersioned;

                if (null == identifiable || null == versioned)
                {
                    continue;
                }

                if (!s_VersionMap.TryGetValue(identifiable.Id, out version) || version != versioned.Version)
                {
                    // A sub-object either did not exist before OR its version is different
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Persists the collection of containers to the given path
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="path"></param>
        public static void PersistContainersAs(IEnumerable<IPropertyContainer> objects, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));

            var fileName = Path.GetFileName(path);
            
            Assert.IsFalse(string.IsNullOrEmpty(fileName));
            
            var directory = new FileInfo(path).Directory;
            
            Assert.IsNotNull(directory);
            
            // Ensure the directory exists
            directory.Create();

            // Save to a temp file to avoid corruption during serialiation
            var tempFile = new FileInfo(Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".uttemp"));

            // Serialize using a persistent scope
            using (Serialization.SerializationContext.Scope(Serialization.SerializationContext.Persistence))
            {
                Serialization.Json.JsonBackEnd.Persist(tempFile.FullName, objects);
            }

            // Overwrite in place
            var file = new FileInfo(path);
            File.Copy(tempFile.FullName, file.FullName, true);
            
            // Clear temp file
            File.Delete(tempFile.FullName);
        }
        
        /// <summary>
        /// Queries the project database and loads all module assets
        ///
        /// @NOTE We will have to address this as the number of modules increases and the size increases
        /// </summary>
        /// <param name="registry"></param>
        public static void LoadAllModules(IRegistry registry)
        {
            var guids = FindAllAssetsGuidsOfType(typeof(UTModule)).Distinct().ToList();
            var subAssetGuidsByModule = new Dictionary<string, List<string>>();

            // Load all modules as a single operation
            using (var transaction = new LoadTransaction())
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                
                    // Push all modules to the transaction
                    // Modules are loaded using thier guid as the source identifier
                    transaction.LoadJson(path, guid, guid);
                    subAssetGuidsByModule.Add(guid, new List<string>());
                }
                
                transaction.Commit(registry);
            }

            // Load all dependencies as a single operation
            using (var transaction = new LoadTransaction())
            {
                foreach (var guid in guids)
                {
                    // Fetch the module
                    var module = registry.FindAllBySource(guid).OfType<TinyModule>().First();
                    var directory = new DirectoryInfo(module.GetDirectoryPath());

                    // Load all sub assets
                    foreach (var filePath in EnumerateAssetFilesRecursive(directory))
                    {
                        var assetPath = GetPathRelativeToProjectPath(filePath.FullName);
                        var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                        transaction.LoadJson(assetPath, assetGuid, assetGuid);
                        subAssetGuidsByModule[guid].Add(assetGuid);
                    }
                }
                
                // Commit the transaction as a single operation
                transaction.Commit(registry);
            }

            // Re-link all references
            foreach (var kvp in subAssetGuidsByModule)
            {
                var module = registry.FindAllBySource(kvp.Key).OfType<TinyModule>().First();
                RebuildModuleReferences(registry, module, kvp.Value);
            }
        }
        
        /// <summary>
        /// Clears the given registry and loads a project at the given path as the working project
        ///
        /// The project is loaded using the `DefaultSourceIdentifier`
        ///
        /// @NOTE This will trigger a `LoadAllModules`
        /// </summary>
        public static TinyProject LoadProject(string projectPath, IRegistry registry)
        {
            var project = LoadMainAsset(projectPath, registry) as TinyProject;
            TinyUpdater.UpdateProject(project);
            return project;
        }

        /// <summary>
        /// Clears the given registry and loads a module at the given path as the working module
        /// 
        /// The module is loaded using the `DefaultSourceIdentifier`
        ///
        /// @NOTE This will trigger a `LoadAllModules`
        /// </summary>
        public static TinyModule LoadModule(string modulePath, IRegistry registry)
        {
            return LoadMainAsset(modulePath, registry) as TinyModule;
        }

        internal static void ShallowLoad(string mainAssetPath, IRegistry registry)
        {
            mainAssetPath = GetPathRelativeToProjectPath(mainAssetPath);
            var mainAssetGuid = AssetDatabase.AssetPathToGUID(mainAssetPath);
            if (string.IsNullOrEmpty(mainAssetGuid))
            {
                throw new NullReferenceException($"{mainAssetPath}: main asset GUID is null at path");
            }
            // Load and commit the main asset (project/module) as a single operation
            using (var transaction = new LoadTransaction())
            {
                transaction.LoadJson(mainAssetPath, TinyRegistry.DefaultSourceIdentifier, mainAssetGuid);
                transaction.Commit(registry);
            }
        }

        private static IPersistentObject LoadMainAsset(string mainAssetPath, IRegistry registry)
        {
            mainAssetPath = GetPathRelativeToProjectPath(mainAssetPath);
            var mainAssetGuid = AssetDatabase.AssetPathToGUID(mainAssetPath);
            if (string.IsNullOrEmpty(mainAssetGuid))
            {
                throw new NullReferenceException($"{mainAssetPath}: main asset GUID is null at path");
            }
         
            LoadAllModules(registry);
            
            // @TODO Fixme
            // This module was picked up during the `LoadAllModules` and loaded with the `guid` as the identifier
            // We need to unload this module and re-load it using the `DefaultSourceIdentifier`
            registry.UnregisterAllBySource(mainAssetGuid);

            // Load and commit the main asset (project/module) as a single operation
            using (var transaction = new LoadTransaction())
            {
                transaction.LoadJson(mainAssetPath, TinyRegistry.DefaultSourceIdentifier, mainAssetGuid);
                transaction.Commit(registry);
            }

            TinyModule module;
            var subAssetGuids = new List<string>();
            
            // Load all dependencies as a single operation
            using (var transaction = new LoadTransaction())
            {
                // Fetch the main module
                module = registry.FindAllBySource(TinyRegistry.DefaultSourceIdentifier).OfType<TinyModule>().First();
    
                var directory = new DirectoryInfo(module.GetDirectoryPath());
    
                // Load all sub assets
                foreach (var filePath in EnumerateAssetFilesRecursive(directory))
                {
                    var assetPath = GetPathRelativeToProjectPath(filePath.FullName);
                    var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                    transaction.LoadJson(assetPath, assetGuid, assetGuid);
                    subAssetGuids.Add(assetGuid);
                }
                
                // Commit the transaction as a single operation
                transaction.Commit(registry);
            }
            
            // @HACK For configuration entities
            // Since the `Configuration` entity is stored within the project it is loaded in the first pass. 
            // This means we have no user generated TypeInformation loaded.
            // We are doing a second load of the main asset to ensure the configuration entity is loaded with type information
            using (var transaction = new LoadTransaction())
            {
                transaction.LoadJson(mainAssetPath, TinyRegistry.DefaultSourceIdentifier, mainAssetGuid);
                transaction.Commit(registry);
            }

            if (!s_AssetGuidToObjectsMap.TryGetValue(mainAssetGuid, out var tinyGuids))
            {
                return null;
            }

            module = registry.FindAllBySource(TinyRegistry.DefaultSourceIdentifier).OfType<TinyModule>().First();
            RebuildModuleReferences(registry, module, subAssetGuids);

            return registry.FindById(new TinyId(tinyGuids[0])) as IPersistentObject;
        }

        private static void RebuildModuleReferences(IRegistry registry, TinyModule module, IEnumerable<string> subAssetGuids)
        {
            // Purge the current state if ANY
            // @NOTE This is required to ensure backwards compatibility with older projects
            module.EntityGroups.Clear();
            module.Configurations.Clear();
            module.Components.Clear();
            module.Structs.Clear();
            module.Enums.Clear();

            // Re-link all sub assets in our acceleration structures
            foreach (var assetGuid in subAssetGuids)
            {
                if (!s_AssetGuidToObjectsMap.TryGetValue(assetGuid, out var guids))
                {
                    Debug.LogError($"Failed to resolve AssetGuid=[{assetGuid}]");
                    continue;
                }

                var obj = registry.FindById(new TinyId(guids[0]));

                if (null == obj)
                {
                    Debug.LogError($"Failed to resolve TinyId=[{guids[0]}]");
                    continue;
                }
                
                if (obj is TinyType type)
                {
                    switch (type.TypeCode)
                    {
                        case TinyTypeCode.Configuration:
                            module.Configurations.Add(type.Ref);
                            break;
                        case TinyTypeCode.Component:
                            module.Components.Add(type.Ref);
                            break;
                        case TinyTypeCode.Struct:
                            module.Structs.Add(type.Ref);
                            break;
                        case TinyTypeCode.Enum:
                            module.Enums.Add(type.Ref);
                            break;
                    }
                }
                else if (obj is TinyEntityGroup group)
                {
                    module.EntityGroups.Add(group.Ref);
                }
            }

            RegisterVersions(module);
        }

        /// <summary>
        /// Force reload the given persistenceId
        /// </summary>
        public static void ReloadObject(IRegistry registry, string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            registry.UnregisterAllBySource(guid);
            
            using (var transaction = new LoadTransaction())
            {
                transaction.LoadJson(path, guid, guid);
                transaction.Commit(registry);
            }
        }

        /// <summary>
        /// Performs a synchronous load and commit operation
        /// </summary>
        public static void LoadJson(string path, string identifier, string persistenceId, IRegistry registry)
        {
            using (var transaction = new LoadTransaction())
            {
                transaction.LoadJson(path, identifier, persistenceId);
                transaction.Commit(registry);
            }
        }

        /// <summary>
        /// Updates the version information for the given persistent object
        /// </summary>
        public static void RegisterVersions(IPersistentObject persistentObject)
        {
            s_VersionMap[persistentObject.Id] = persistentObject.Version;
            
            // Unpack versions of ALL objects from this file
            foreach (var container in persistentObject.EnumerateContainers().OfType<IIdentified<TinyId>>())
            {
                var versioned = container as IVersioned;

                if (null == versioned)
                {
                    return;
                }
                    
                s_VersionMap[container.Id] = versioned.Version;
            }
        }

        /// <summary>
        /// Gets the asset path for the given persistent object based on the `PersistenceId` (unity asset guid)
        /// </summary>
        /// <param name="p">PersistentObject</param>
        /// <returns>Asset path if one exists; null otherwise</returns>
        internal static string GetAssetPath(IPersistentObject p)
        {
            var guid = p?.PersistenceId;
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        /// <summary>
        /// Gets all the persistence object ids associated with a Unity asset guid.
        /// </summary>
        /// <param name="assetGuid">Unity asset guid</param>
        /// <returns>An array of persistence object ids</returns>
        internal static string[] GetRegistryObjectIdsForAssetGuid(string assetGuid)
        {
            return s_AssetGuidToObjectsMap.TryGetValue(assetGuid, out var ids) ? ids : new string[0];
        }

        /// <summary>
        /// Gets the associated asset guid of a tiny id, if it exists.
        /// </summary>
        /// <param name="tinyId">The Tiny Id</param>
        /// <returns>Asset guid or null</returns>
        internal static string GetAssetGuidFromTinyGuid(TinyId tinyId)
        {
            return s_ObjectToAssetGuidMap.TryGetValue(tinyId.ToString(), out var assetGuid) ? assetGuid : null;
        }

        /// <summary>
        /// Returns the file name with extension for the given persistent object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        internal static string GetFileName(IPersistentObject obj)
        {
            return obj.Name + GetFileExtension(obj);
        }
        
        /// <summary>
        /// Returns the extension for the given persistent object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        internal static string GetFileExtension(IPersistentObject obj)
        {
            if (obj is TinyProject)
                return ProjectFileExtension;
            if (obj is TinyModule)
                return ModuleFileExtension;
            if (obj is TinyEntityGroup)
                return EntityGroupFileExtension;
            if (obj is TinyType)
                return TypeFileExtension;
            
            throw new NotSupportedException($"Persitent object of type {obj.GetType()} has no known file extension");
        }

        internal static string GetRelativePathForSubAsset(IPersistentObject obj)
        {
            var directory = GetRelativePathForPersistentObjectType(obj.GetType());
            var fileName = GetFileName(obj);
            return string.IsNullOrEmpty(directory) ? fileName : Path.Combine(directory, fileName);
        }

        internal static string GetRelativePathForPersistentObjectType(Type type)
        {
            if (typeof(TinyEntityGroup) == type)
                return EntityGroupDirectory;
            
            if (typeof(TinyType) == type)
                return TypeDirectory;

            return string.Empty;
        }

        private static IEnumerable<string> GetFileExtensionsForAssetType(Type type)
        {
            if (typeof(UTModule) == type || typeof(TinyScriptableObject) == type)
                yield return ProjectFileImporterExtension;
            
            if (typeof(UTModule) == type || typeof(TinyScriptableObject) == type)
                yield return ModuleFileImporterExtension;
            
            if (typeof(UTEntityGroup) == type || typeof(TinyScriptableObject) == type)
                yield return EntityGroupFileImporterExtension;
            
            if (typeof(UTType) == type || typeof(TinyScriptableObject) == type)
                yield return TypeFileImporterExtension;
        }
        
        private static IEnumerable<string> FindAllAssetsGuidsOfType(Type type)
        {
            var typeName = type.Name;
            
            // Find all assets in the AssetDatabase
            // @NOTE This only looks in `Assets/*`
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeName))
            {
                yield return guid;
            }

            string[] packageAssets;
            if (!s_TypeToPackageAssetGuidMap.TryGetValue(type, out packageAssets))
            {
                var list = new List<string>();

                foreach (var ext in GetFileExtensionsForAssetType(type))
                {
                    list.AddRange(FindPackageAssetGuidsByExtension(ext));
                }

                packageAssets = list.ToArray();
                s_TypeToPackageAssetGuidMap[type] = packageAssets;
            }

            foreach (var guid in packageAssets)
            {
                yield return guid;
            }
        }

        private static IEnumerable<string> FindPackageAssetGuidsByExtension(string extension)
        {
            const string dbPath = TinyConstants.PackagePath + "/Runtime/Modules/";
            var realPath = Path.GetFullPath(dbPath);
            
            var dir = new DirectoryInfo(realPath);
            
            if (!dir.Exists)
            {
                yield break;
            }
            
            var prefixLen = realPath.Length;
            foreach (var f in dir.GetFiles("*." + extension, SearchOption.AllDirectories))
            {
                var fileName = f.FullName.ToForwardSlash();
                var assetPath = dbPath + fileName.Substring(prefixLen, fileName.Length - prefixLen);
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogError($"No AssetDatabase GUID found for file: {f.FullName}, with path: {assetPath}");
                    continue;
                }

                yield return guid;
            }
        }

        /// <summary>
        /// Given an on assetGuid (e.g. UTType or UTEntityGroup) this will return the assetGuid of the MainAsset (e.g. UTProject or UTModule)
        /// </summary>
        /// <param name="subAssetGuid"></param>
        /// <returns></returns>
        internal static string GetMainAssetGuid(string subAssetGuid)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(subAssetGuid);
            var fileInfo = new FileInfo(assetPath);
            var directory = fileInfo.Directory;

            // Recurse up the file system to find the first project path
            while (null != directory)
            {
                var directoryGuid = GetGuidRelativeToProjectPath(directory.FullName);
                if (s_DirectoryGuidToMainAssetGuidMap.TryGetValue(directoryGuid, out var mainAssetGuid))
                {
                    return mainAssetGuid;
                }
                        
                directory = directory?.Parent;
            }

            return null;
        }

        /// <summary>
        /// Returns the `Unity` asset path for the given full path
        ///
        /// E.g. C:/Projects/MyProject/Assets/Foo.png -> Assets/Foo.png
        /// </summary>
        internal static string GetPathRelativeToProjectPath(string path)
        {
            var assetPath = Path.GetFullPath(path).ToForwardSlash();
            
            // check if the given path is a package path (relative or installed)
            // assumption: true if the path contains the package name
            var packagePartIndex = assetPath.LastIndexOf(TinyConstants.PackageName, StringComparison.Ordinal);
            if (packagePartIndex >= 0)
            {
                var localPath = TinyConstants.PackagePath + assetPath.Substring(assetPath.IndexOf('/', packagePartIndex));
                return localPath;
            }
            
            // otherwise, we assume it can be any path, and attempt normalization
            var projectPath = new DirectoryInfo(".").FullName.ToForwardSlash() + "/";
            assetPath = assetPath.Replace(projectPath, string.Empty);
            return assetPath;
        }
        
        /// <summary>
        /// Returns the `Unity` asset guid for the given full path
        /// </summary>
        internal static string GetGuidRelativeToProjectPath(string path)
        {
            var assetPath = GetPathRelativeToProjectPath(path);
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            return assetGuid;
        }
        
        private static IEnumerable<T> AsEnumerable<T>(T o)
        {
            yield return o;
        }
        
        /// <summary>
        /// Enumerates all sub assets of the given directory
        ///
        /// @NOTE This method will skip any nested modules or projects
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private static IEnumerable<FileInfo> EnumerateAssetFilesRecursive(DirectoryInfo directory)
        {
            foreach (var file in EnumerateAssetFiles(directory))
            {
                yield return file;
            }
            
            foreach (var subDirectory in directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                var subDirectoryGuid = GetGuidRelativeToProjectPath(subDirectory.FullName);
                
                // This directory hosts a sub-project or module; skip
                if (s_DirectoryGuidToMainAssetGuidMap.ContainsKey(subDirectoryGuid))
                {
                    continue;
                }

                // All files of this directory should be considered as sub assets
                foreach (var file in EnumerateAssetFilesRecursive(subDirectory))
                {
                    yield return file;
                }
            }
        }

        /// <summary>
        /// Enumerates all `Tiny` files types in the given directory 
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private static IEnumerable<FileInfo> EnumerateAssetFiles(DirectoryInfo directory)
        {
            foreach (var filePath in directory.EnumerateFiles($"*{TypeFileExtension}", SearchOption.TopDirectoryOnly))
            {
                yield return filePath;
            }
            
            foreach (var filePath in directory.EnumerateFiles($"*{EntityGroupFileExtension}", SearchOption.TopDirectoryOnly))
            {
                yield return filePath;
            }
            
            foreach (var filePath in directory.EnumerateFiles($"*{PrefabFileExtension}", SearchOption.TopDirectoryOnly))
            {
                yield return filePath;
            }
        }
    }
}

