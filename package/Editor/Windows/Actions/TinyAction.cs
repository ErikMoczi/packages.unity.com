

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// Helper class for asset creation with user interaction
    /// </summary>
    internal static class TinyAction
    {
        public static void CreateEntityGroup(IRegistry registry, TinyModule.Reference moduleReference, Action<TinyEntityGroup> onComplete = null)
        {
            // Find a unique name for this entity group
            var module = moduleReference.Dereference(registry);
            var name = TinyUtility.GetUniqueName(module.EntityGroups, "NewEntityGroup");
            
            // Build our `CreateAsset` request object
            var create = ScriptableObject.CreateInstance<DoCreateEntityGroup>();

            // Initialize the "create asset request"
            create.Registry = registry;
            create.MainModule = moduleReference;
            create.OnComplete = onComplete;
            
            var path = GetPathName<TinyEntityGroup>(name);
            
            // Ensure the path is created and imported to the database
            CreatePath(path);

            // This will prompt the user in the `Asset` window to name the asset using a 'Unity' like flow
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                create,
                path, 
                TinyIcons.ScriptableObjects.EntityGroup,
                null);
        }
        
        public static void CreatePrefab(IRegistry registry, TinyModule.Reference moduleReference, string defaultName, Action<TinyEntityGroup> onComplete = null)
        {
            // Find a unique name for this entity group
            var module = moduleReference.Dereference(registry);
            var name = TinyUtility.GetUniqueName(module.EntityGroups, defaultName);
            
            // Build our `CreateAsset` request object
            var create = ScriptableObject.CreateInstance<DoCreatePrefab>();

            // Initialize the "create asset request"
            create.Registry = registry;
            create.MainModule = moduleReference;
            create.OnComplete = onComplete;
            
            var path = GetPathName<TinyEntityGroup>(name);
            
            // Ensure the path is created and imported to the database
            CreatePath(path);

            // This will prompt the user in the `Asset` window to name the asset using a 'Unity' like flow
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                create,
                path, 
                TinyIcons.ScriptableObjects.Prefab,
                null);
        }

        public static void CreateScript(string defaultPath, Func<string, string> nameToContents)
        {
            // Build our `CreateAsset` request object
            var create = ScriptableObject.CreateInstance<DoCreateScript>();

            // Initialize the "create asset request"
            create.NameToContents = nameToContents;

            // This will prompt the user in the `Asset` window to name the asset using a 'Unity' like flow
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                create,
                defaultPath, 
                TinyIcons.ScriptableObjects.TypeScript,
                null);
        }
        
        public static void CreateComponentType(IRegistry registry, TinyModule.Reference moduleReference, Action<TinyType> onComplete = null)
        {
            CreateType(registry, moduleReference, "NewComponent", TinyTypeCode.Component, onComplete);
        }
        
        public static void CreateStructType(IRegistry registry, TinyModule.Reference moduleReference, Action<TinyType> onComplete = null)
        {
            CreateType(registry, moduleReference, "NewStruct", TinyTypeCode.Struct, onComplete);
        }
        
        public static void CreateEnumType(IRegistry registry, TinyModule.Reference moduleReference, Action<TinyType> onComplete = null)
        {
            CreateType(registry, moduleReference, "NewEnum", TinyTypeCode.Enum, type =>
            {
                type.BaseType = TinyType.Int32.Ref;
                onComplete?.Invoke(type);
            });
        }
        
        public static void CreateConfigurationType(IRegistry registry, TinyModule.Reference moduleReference, Action<TinyType> onComplete = null)
        {
            CreateType(registry, moduleReference, "NewConfiguration", TinyTypeCode.Configuration, onComplete);
        }

        private static void CreateType(
            IRegistry registry, 
            TinyModule.Reference moduleReference, 
            string name, 
            TinyTypeCode typeCode, 
            Action<TinyType> onComplete)
        {
            // Ensure we have a unique name
            var module = moduleReference.Dereference(registry);
            name = TinyUtility.GetUniqueName(module.Types, name);
            
            // Build our `CreateAsset` request object
            var create = ScriptableObject.CreateInstance<DoCreateType>();

            // Initialize the "create asset request"
            create.Registry = registry;
            create.MainModule = moduleReference;
            create.OnComplete = onComplete;
            create.TypeCode = typeCode;
            
            var path = GetPathName<TinyType>(name);
            
            // Ensure the path is created and imported to the database
            CreatePath(path);

            // This will prompt the user in the `Asset` window to name the asset using a 'Unity' like flow
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                create,
                path, 
                TinyIcons.ScriptableObjects.GetIconForTypeCode(typeCode),
                null);
        }
        
        private static string GetPathName<T>(string assetName)
        {
            var root = TinyEditorApplication.EditorContext.GetPersistentObjects().First();
            
            // Get the project directory
            var directory = Path.GetDirectoryName(Persistence.GetAssetPath(root));
            
            var path = Path.Combine(
                directory,
                Persistence.GetRelativePathForPersistentObjectType(typeof(T))
            );

            var rootAssetPath = path.ToForwardSlash();
            var currentFolder = AssetDatabase.GetAssetPath(Selection.activeObject).ToForwardSlash();

            string assetPath;

            if(currentFolder.StartsWith(rootAssetPath))
            {
                if(File.Exists(currentFolder))
                {
                    //if the folder is actually a file, then get it's directory
                    assetPath = Path.GetDirectoryName(currentFolder);
                }
                else
                {
                    assetPath = currentFolder;
                }
            }
            else
            {
                //if the current folder is outside of the project, use the root as default
                assetPath = rootAssetPath;
            }

            return Path.Combine(assetPath, assetName).ToForwardSlash();
        }

        private static void CreatePath(string path)
        {
            new FileInfo(path).Directory.Create();
            AssetDatabase.ImportAsset(Path.GetDirectoryName(path), ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUncompressedImport);
        }
    }

    internal abstract class TinyCreateAssetRequest<T> : EndNameEditAction
    {
        public IRegistry Registry { protected get; set; }
        public TinyModule.Reference MainModule { protected get; set; }
        public Action<T> OnComplete { protected get; set; }
    }

    internal class DoCreateScript : EndNameEditAction
    {
        public Func<string, string> NameToContents { protected get; set; }
        
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var contents = NameToContents.Invoke(Path.GetFileNameWithoutExtension(pathName));
            File.WriteAllText(pathName, contents);
            AssetDatabase.ImportAsset(pathName, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUncompressedImport);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(pathName);

            if (asset)
            {
                Selection.activeInstanceID = asset.GetInstanceID();
            }
        }
    }
    
    internal class DoCreateEntityGroup : TinyCreateAssetRequest<TinyEntityGroup>
    {
        /// <summary>
        /// Invoked when the user has chosen a unique name for the new asset
        /// </summary>
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var module = MainModule.Dereference(Registry);
            var name = Path.GetFileNameWithoutExtension(pathName);

            if (module.EntityGroups.Any(t => t.Name == name))
            {
                Debug.LogError($"[{TinyConstants.ApplicationName}] Name=[{name}] is already used by another EntityGroup");
                return;
            }
            
            TinyEntityGroup group;
            
            using (Registry.DontTrackChanges())
            {
                using (Registry.SourceIdentifierScope(TinyRegistry.TempSourceIdentifier))
                {
                    group = Registry.CreateEntityGroup(TinyId.New(), Path.GetFileNameWithoutExtension(pathName));
                }
                
                Persistence.PersistObject(group, pathName + Persistence.EntityGroupFileExtension);
                Registry.ChangeSource(group.Id, group.PersistenceId);
                
                module.AddEntityGroupReference((TinyEntityGroup.Reference) group);
            }
            
            module.IncrementVersion(null, module);
            OnComplete?.Invoke(group);
        }
    }
    
    internal class DoCreatePrefab : TinyCreateAssetRequest<TinyEntityGroup>
    {
        /// <summary>
        /// Invoked when the user has chosen a unique name for the new asset
        /// </summary>
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var module = MainModule.Dereference(Registry);
            var name = Path.GetFileNameWithoutExtension(pathName);

            if (module.EntityGroups.Any(t => t.Name == name))
            {
                Debug.LogError($"[{TinyConstants.ApplicationName}] Name=[{name}] is already used by another EntityGroup");
                return;
            }
            
            TinyEntityGroup group;
            
            using (Registry.DontTrackChanges())
            {
                using (Registry.SourceIdentifierScope(TinyRegistry.TempSourceIdentifier))
                {
                    group = Registry.CreateEntityGroup(TinyId.New(), Path.GetFileNameWithoutExtension(pathName));
                }
                
                Persistence.PersistObject(group, pathName + Persistence.PrefabFileExtension);
                Registry.ChangeSource(group.Id, group.PersistenceId);
                
                module.AddEntityGroupReference((TinyEntityGroup.Reference) group);
            }
            
            module.IncrementVersion(null, module);
            OnComplete?.Invoke(group);
        }
    }
    
    internal class DoCreateType : TinyCreateAssetRequest<TinyType>
    {
        public TinyTypeCode TypeCode { protected get; set; }
        
        /// <summary>
        /// Invoked when the user has chosen a unique name for the new asset
        /// </summary>
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var module = MainModule.Dereference(Registry);
            var name = Path.GetFileNameWithoutExtension(pathName);

            if (module.Types.Any(t => t.Name == name))
            {
                Debug.LogError($"[{TinyConstants.ApplicationName}] Name=[{name}] is already used by another Type");
                return;
            }

            if (TinyScriptUtility.IsReservedKeyword(name))
            {
                Debug.LogError($"[{TinyConstants.ApplicationName}] TypeName=[{name}] is a reserved keyword");
                return;
            }
            
            TinyType type;
            
            using (Registry.DontTrackChanges())
            {
                using (Registry.SourceIdentifierScope(TinyRegistry.TempSourceIdentifier))
                {
                    type = Registry.CreateType(TinyId.New(), name, TypeCode);
                }
                
                Persistence.PersistObject(type, pathName + Persistence.TypeFileExtension);
                Registry.ChangeSource(type.Id, type.PersistenceId);

                switch (TypeCode)
                {
                    case TinyTypeCode.Component:
                        module.AddComponentReference((TinyType.Reference) type);
                        break;
                    case TinyTypeCode.Struct:
                        module.AddStructReference((TinyType.Reference) type);
                        break;
                    case TinyTypeCode.Enum:
                        module.AddEnumReference((TinyType.Reference) type);
                        break;
                    case TinyTypeCode.Configuration:
                        module.AddConfigurationReference((TinyType.Reference) type);
                        break;
                    default:
                        Debug.LogError("Invalid type code");
                        break;
                }
            }
            
            module.IncrementVersion(null, module);
            OnComplete?.Invoke(type);
        }
    }
}

