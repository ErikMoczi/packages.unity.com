
using System;
using UnityEditor;

namespace Unity.Tiny
{
    internal static class AssetMenuItems
    {
        private const int k_BasePriority = 150;
        private const int k_TinyOffset = 200;

        private const string k_AssetsMenuItemPrefix = "Assets/Create/" + TinyConstants.ApplicationName + "/";
        private const string k_TinyMenuItemPrefix = TinyConstants.ApplicationName + "/Create/";
        private const string k_EntityGroup = "EntityGroup";
        private const string k_CreateComponentItem = "Component";
        private const string k_CreateStructItem = "Struct";
        private const string k_CreateEnumItem = "Enum";
        private const string k_CreateConfigurationItem = "Configuration";
        private const string k_CreateTypeScriptSystemItem = "TypeScript System";
        private const string k_CreateTypeScriptBehaviourItem = "TypeScript Behaviour";

        [MenuItem(k_TinyMenuItemPrefix + k_EntityGroup, isValidateFunction: true)]
        [MenuItem(k_AssetsMenuItemPrefix + k_EntityGroup, isValidateFunction: true)]
        private static bool ValidateProjectIsOpened()
        {
            // We should NOT allow root modules to create entity group.
            return TinyEditorApplication.ContextType == EditorContextType.Project;
        }

        [MenuItem(k_AssetsMenuItemPrefix + k_CreateComponentItem, isValidateFunction: true)]
        [MenuItem(k_TinyMenuItemPrefix + k_CreateComponentItem, isValidateFunction: true)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateComponentItem, isValidateFunction: true)]
        [MenuItem(k_TinyMenuItemPrefix + k_CreateStructItem, isValidateFunction: true)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateStructItem, isValidateFunction: true)]
        [MenuItem(k_TinyMenuItemPrefix + k_CreateEnumItem, isValidateFunction: true)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateEnumItem, isValidateFunction: true)]
        [MenuItem(k_TinyMenuItemPrefix + k_CreateConfigurationItem, isValidateFunction: true)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateConfigurationItem, isValidateFunction: true)]
        [MenuItem(k_TinyMenuItemPrefix + k_CreateTypeScriptSystemItem, isValidateFunction: true)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateTypeScriptSystemItem, isValidateFunction: true)]
        [MenuItem(k_TinyMenuItemPrefix + k_CreateTypeScriptBehaviourItem, isValidateFunction: true)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateTypeScriptBehaviourItem, isValidateFunction: true)]
        private static bool ValidateContextNotNull()
        {
            return TinyEditorApplication.ContextType != EditorContextType.None;
        }

        [MenuItem(k_TinyMenuItemPrefix + k_EntityGroup, priority = k_BasePriority + k_TinyOffset)]
        [MenuItem(k_AssetsMenuItemPrefix + k_EntityGroup, priority = k_BasePriority)]
        public static void CreateEntityGroup()
        {
            CreateAsset<TinyEntityGroup>(TinyAction.CreateEntityGroup);
        }

        [MenuItem(k_TinyMenuItemPrefix + k_CreateComponentItem, priority = k_BasePriority + k_TinyOffset + 50)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateComponentItem, priority = k_BasePriority + 50)]
        public static void CreateComponent()
        {
            CreateAsset<TinyType>(TinyAction.CreateComponentType);
        }

        [MenuItem(k_TinyMenuItemPrefix+ k_CreateStructItem, priority = k_BasePriority + k_TinyOffset + 51)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateStructItem, priority = k_BasePriority + 51)]
        public static void CreateStruct()
        {
            CreateAsset<TinyType>(TinyAction.CreateStructType);
        }

        [MenuItem(k_TinyMenuItemPrefix + k_CreateEnumItem, priority = k_BasePriority + k_TinyOffset + 52)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateEnumItem, priority = k_BasePriority + 52)]
        public static void CreateEnum()
        {
            CreateAsset<TinyType>(TinyAction.CreateEnumType);
        }
        
        [MenuItem(k_TinyMenuItemPrefix + k_CreateConfigurationItem, priority = k_BasePriority + k_TinyOffset + 52)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateConfigurationItem, priority = k_BasePriority + 52)]
        public static void CreateConfiguration()
        {
            CreateAsset<TinyType>(TinyAction.CreateConfigurationType);
        }

        [MenuItem(k_TinyMenuItemPrefix+ k_CreateTypeScriptSystemItem, priority = k_BasePriority + k_TinyOffset + 150)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateTypeScriptSystemItem, priority = k_BasePriority + 150)]
        public static void CreateTypeScriptSystemAsset()
        {
            var module = TinyEditorApplication.Module;
            var scripting = TinyEditorApplication.EditorContext.Context.GetManager<TinyScriptingManager>().Metadata;
            var systemName = TinyUtility.GetUniqueName(scripting.Systems, "NewSystem");
            
            var systemPath = AssetDatabase.GUIDToAssetPath(module.ScriptRootDirectory) + "/" + systemName +
                TinyScriptUtility.TypeScriptExtension;
            
            TinyAction.CreateScript(systemPath, (userFileName) =>
            {
                userFileName = userFileName.Replace(" ", string.Empty);
                userFileName = TinyUtility.GetUniqueName(scripting.Systems, userFileName);
                return $@"
namespace {module.Namespace} {{

    /** New System */
    export class {userFileName} extends ut.ComponentSystem {{
        
        OnUpdate():void {{

        }}
    }}
}}
";
            });
        }

        [MenuItem(k_TinyMenuItemPrefix+ k_CreateTypeScriptBehaviourItem, priority = k_BasePriority + k_TinyOffset + 151)]
        [MenuItem(k_AssetsMenuItemPrefix + k_CreateTypeScriptBehaviourItem, priority = k_BasePriority + 151)]
        public static void CreateTypeScriptBehaviourAsset()
        {
            var module = TinyEditorApplication.Module;
            var scripting = TinyEditorApplication.EditorContext.Context.GetManager<TinyScriptingManager>().Metadata;
            var behaviourName = TinyUtility.GetUniqueName(scripting.Behaviours, "NewBehaviour");

            var behaviourPath = AssetDatabase.GUIDToAssetPath(module.ScriptRootDirectory) + "/" + behaviourName +
                TinyScriptUtility.TypeScriptExtension;

            TinyAction.CreateScript(behaviourPath, (userFileName) =>
            {
                userFileName = userFileName.Replace(" ", string.Empty);
                userFileName = TinyUtility.GetUniqueName(scripting.Systems, userFileName);
                var filterName = $"{userFileName}Filter";
                
                return $@"
namespace {module.Namespace} {{

    export class {filterName} extends ut.EntityFilter {{
        node: ut.Core2D.TransformNode;
        position?: ut.Core2D.TransformLocalPosition;
        rotation?: ut.Core2D.TransformLocalRotation;
        scale?: ut.Core2D.TransformLocalScale;
    }}

    export class {userFileName} extends ut.ComponentBehaviour {{

        data: {filterName};

        // ComponentBehaviour lifecycle events
        // uncomment any method you need
        
        // this method is called for each entity matching the {filterName} signature, once when enabled
        //OnEntityEnable():void {{ }}
        
        // this method is called for each entity matching the {filterName} signature, every frame it's enabled
        //OnEntityUpdate():void {{ }}

        // this method is called for each entity matching the {filterName} signature, once when disabled
        //OnEntityDisable():void {{ }}

    }}
}}
";
            });
        }

        private static void CreateAsset<TPersistentObject>(Action<IRegistry, TinyModule.Reference, Action<TPersistentObject>> creator)
            where TPersistentObject : IPersistentObject
        {
            var registry = TinyEditorApplication.Registry;
            var module = TinyEditorApplication.Module;

            if (null == registry || null == module)
            {
                return;
            }

            creator.Invoke(registry, module.Ref, obj =>
            {
                var path = Persistence.GetAssetPath(obj);
                Selection.activeInstanceID = AssetDatabase.LoadAssetAtPath<TinyScriptableObject>(path).GetInstanceID();
            });
        }
    }
}
