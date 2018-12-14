

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Tiny.Serialization.Binary;

namespace Unity.Tiny
{
    [TinyInitializeOnLoad]
    internal static class TinyTemp
    {
        private const int KTempVersion = 6;
        private const string KTempFileName = "project";
        private static readonly FileInfo s_TempFileInfo;

        private enum SaveType : byte
        {
            /// <summary>
            /// The file only contains a single PersistenceId reference to the real content
            /// This is an unchanged file that the user was viewing
            /// </summary>
            PersistentUnchanged = 0,
            
            /// <summary>
            /// The file contains a PersistenceId and a FULL serialized copy of the object
            /// @NOTE This can become a diff in the future
            /// </summary>
            PersistentChanged = 1,
            
            /// <summary>
            /// This file contains a FULL serialized object that does NOT have a PersistentId
            /// </summary>
            Temporary = 2
        }

        static TinyTemp()
        {
            s_TempFileInfo = new FileInfo(Path.Combine(Application.temporaryCachePath, KTempFileName + ".uttemp"));
        }

        private static FileInfo GetTempLocation()
        {
            return s_TempFileInfo; 
        }

        /// <summary>
        /// Saves a persistent project WITHOUT changes
        ///
        /// The expectation of the user is that when loading an unchanged project from temp (e.g. domain reload)
        /// the same project is loaded and any on disc changes are reflected automatically
        /// </summary>
        /// <param name="objects"></param>
        public static void SavePersistentUnchanged(IList<IPersistentObject> objects)
        {
            using (var stream = new FileStream(GetTempLocation().FullName, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // Temp file header
                writer.Write(KTempVersion);
                writer.Write((byte) SaveType.PersistentUnchanged);
                
                // We only want to save the `PersistenceId`
                // Any on disc changes are loaded automatically as expected
                writer.Write(objects.First().PersistenceId ?? string.Empty);
            }
        }

        /// <summary>
        /// Saves a persistent project WITH changes
        ///
        /// The expectation of the user is that when loading any changes that they have made a preserved
        /// If the on disc project has changed we prompt the user to resolve the conflict
        /// </summary>
        /// <param name="objects"></param>
        public static void SavePersistentChanged(IList<IPersistentObject> objects)
        {
            using (var stream = new FileStream(GetTempLocation().FullName, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // Temp file header
                writer.Write(KTempVersion);
                writer.Write((byte) SaveType.PersistentChanged);
                
                // Write a header for each persistent object in the project including a hash of the content
                // This is used to detect any changes and promt the user when loading
                writer.Write(objects.Count);
                foreach (var obj in objects)
                {
                    var persistenceId = obj.PersistenceId ?? string.Empty;
                    writer.Write(persistenceId);
                    writer.Write(TinyCryptography.ComputeHash(File.ReadAllText(AssetDatabase.GUIDToAssetPath(persistenceId))));
                }
    
                // Write each persistent object to the stream as binary prefixed with the `SourceIdentifierScope`
                // the `SourceIdentifierScope` to load objects into the correct bucket on deserialization
                foreach (var obj in objects)
                {
                    string sourceIdentifier;
                    if (!obj.Registry.TryGetSourceIdentifier(obj.Id, out sourceIdentifier))
                    {
                        sourceIdentifier = TinyRegistry.DefaultSourceIdentifier;
                    }
                    
                    writer.Write(sourceIdentifier);
                    writer.Write(Persistence.IsPersistentObjectChanged(obj));
                    BinaryBackEnd.Persist(stream, obj.EnumerateContainers());
                }
            }
        }

        /// <summary>
        /// Saves a temporary project
        ///
        /// This is a project that has never been saved to disc and only exists in memory
        /// </summary>
        public static void SaveTemporary(IList<IPersistentObject> objects)
        {
            using (var stream = new FileStream(GetTempLocation().FullName, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // Temp file header
                writer.Write(KTempVersion);
                writer.Write((byte) SaveType.Temporary);
                
                // Write each persistent object to the stream as binary prefixed with the `SourceIdentifierScope` 
                writer.Write(objects.Count);
                foreach (var obj in objects)
                {
                    writer.Write(TinyRegistry.DefaultSourceIdentifier);
                    BinaryBackEnd.Persist(stream, obj.EnumerateContainers());
                }
            }
        }

        public static void Delete()
        { 
            var location = GetTempLocation().FullName;
            if (!File.Exists(location))
            {
                return;
            }
            File.Delete(location);
        }

        public static bool Exists()
        {
            return File.Exists(GetTempLocation().FullName);
        }
        
        /// <summary>
        /// @TODO This method has too many conditionals and checks... it should be managed at a higher level
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="persistenceId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool Accept(IRegistry registry, out string persistenceId)
        {
            Assert.IsTrue(Exists());
            
            Persistence.LoadAllModules(registry);

            persistenceId = null;

            using (var stream = File.OpenRead(GetTempLocation().FullName))
            using (var reader = new BinaryReader(stream))
            {
                var version = reader.ReadInt32();

                if (version != KTempVersion)
                {
                    Debug.LogWarning($"TinyTemp format has been changed from version {version} to version {KTempVersion}. Workspace was not reloaded");
                    return false;
                }
                
                var type = (SaveType) reader.ReadByte();
                int count;

                var persistenceIds = ListPool<string>.Get();

                try
                {
                    switch (type)
                    {
                        case SaveType.PersistentUnchanged:
                            persistenceId = reader.ReadString();
                            return false;
                        case SaveType.PersistentChanged:
                            count = reader.ReadInt32();
                            var changed = false;
                            for (var i = 0; i < count; i++)
                            {
                                var id = reader.ReadString();
                                var hash = reader.ReadString();

                                persistenceIds.Add(id);

                                if (!string.IsNullOrEmpty(hash) && !string.Equals(hash, TinyCryptography.ComputeHash(File.ReadAllText(AssetDatabase.GUIDToAssetPath(id)))))
                                {
                                    changed = true;
                                }
                            }

                            // Ask the user if they want to keep their changes or reload from disc
                            if (changed && EditorUtility.DisplayDialog($"{TinyConstants.ApplicationName} assets changed",
                                    $"{TinyConstants.ApplicationName} assets have changed on disk, would you like to reload the current project?",
                                    "Yes",
                                    "No"))
                            {
                                return false;
                            }

                            break;
                        case SaveType.Temporary:
                            count = reader.ReadInt32();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // This is to handle module editing.
                    // We want to unregister it from its current source and re-register it with the persistenceId as the scope
                    if (!string.IsNullOrEmpty(persistenceId))
                    {
                        registry.UnregisterAllBySource(persistenceId);
                    }

                    using (var transaction = new Persistence.LoadTransaction())
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var sourceIdentifier = reader.ReadString();
                            var changed = reader.ReadBoolean();
                            transaction.LoadBinary(stream, sourceIdentifier, persistenceIds[i], changed);
                        }

                        transaction.Commit(registry);
                    }
                }
                finally
                {
                    ListPool<string>.Release(persistenceIds);
                }
            }

            return true;
        }
    }
}

