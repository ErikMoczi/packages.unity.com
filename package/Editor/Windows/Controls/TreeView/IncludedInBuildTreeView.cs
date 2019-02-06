
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class IncludedInBuildTreeView : RegistryTreeView
    {
        public enum ExportState
        {
            Explicit = 0,
            Implicit = 1,
            Excluded = 2
        }

        private static bool Toggle(Rect rect, bool value)
        {
            rect.width = 25.0f;
            return EditorGUI.Toggle(rect, GUIContent.none, value);
        }

        internal class IncludedAssetItem : AssetItem
        {
            public IncludedAssetItem(TinyAssetInfo info, RegistryTreeView treeView)
                : base(info, treeView)
            {
            }

            protected override AssetItem CreateSubAssetItem(TinyAssetInfo assetInfo, RegistryTreeView treeView)
            {
                return new IncludedAssetItem(assetInfo, treeView);
            }

            public override void OnGUI(GUIArgs args)
            {
                if (Value.ExplicitReferences.Count > 0)
                {
                    var rect = args.rect;
                    rect.width -= 15.0f;
                    EditorGUI.LabelField(rect, "(Explicit)", TinyStyles.RightAlignedLabel);
                }
                base.OnGUI(args);
            }
        }

        internal class IncludedModuleItem : ModuleItem
        {
            private readonly List<TinyModule.Reference> m_Dependencies;

            public IncludedModuleItem(TinyModule value, List<TinyModule.Reference> dependencies, RegistryTreeView treeView)
                : base(value, treeView)
            {
                m_Dependencies = dependencies;
            }

            public override void OnGUI(GUIArgs args)
            {
                var oldEnable = GUI.enabled;
                var oldColor = GUI.color;
                GUI.enabled &= !(Value.IsRequired || args.MainModule == Value);

                ExportState state;

                if (args.MainModule == Value || args.MainModule.ContainsExplicitModuleDependency(Value.Ref))
                {
                    state = ExportState.Explicit;
                }
                else if (TreeView is IncludedInBuildTreeView includedTree && includedTree.MainDependencies.Contains(Value.Ref))
                {
                    state = ExportState.Implicit;
                    GUI.color = Color.cyan;
                }
                else
                {
                    state = ExportState.Excluded;
                    GUI.color = Color.white;
                }

                if (Toggle(args.rect, state == ExportState.Explicit))
                {
                    args.MainModule.AddExplicitModuleDependency(Value.Ref);
                }
                else
                {
                    args.MainModule.RemoveExplicitModuleDependency(Value.Ref);
                }

                GUI.color = oldColor;

                GUI.enabled = oldEnable;

                args.rect = IndentRect(args.rect, 15.0f);
                args.DefaultOnGUI(args);
            }
        }

        private List<TinyModule.Reference> MainDependencies;

        public IncludedInBuildTreeView(IRegistry registry, State state)
            : base(registry, state)
        {
        }

        protected override void CacheResources()
        {
            MainDependencies = TinyEditorApplication.Module.EnumerateDependencies().Ref().ToList();
        }

        protected override void OverrideDefaults()
        {
            CreateRootModule.Create = (m, t) => new IncludedModuleItem(m, MainDependencies, t);
            CreateRootType.ShouldCreateRootOfType = ShouldIncludeType;
            CreateRootEntityGroup.ShouldCreateRootOfType = ShouldIncludeEntityGroup;
            CreateRootEntity.ShouldCreateRootOfType = ShouldIncludeEntity;
            CreateRootAsset.Create = (a, t) => new IncludedAssetItem(a, t);
        }

        private bool ShouldIncludeType(TinyType type)
        {
            var reference = type.Ref;
            foreach (var moduleRef in MainDependencies)
            {
                var module = moduleRef.Dereference(Registry);
                foreach (var typeRef in module.Types)
                {
                    if (typeRef.Equals(reference))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ShouldIncludeEntityGroup(TinyEntityGroup group)
        {
            return MainModule.EntityGroups.Contains((TinyEntityGroup.Reference)group);
        }

        private static bool ShouldIncludeEntity(TinyEntity entity)
        {
            return false;
        }

        protected override void OnItemContextClicked(ItemBase item, GenericMenu menu)
        {
            if (item is AssetItem assetItem)
            {
                var module = TinyEditorApplication.Module;
                if (assetItem.Value.Object is Texture2D)
                {
                    PopulateMenu<TinyTextureSettings>(assetItem, menu);
                }
                else if (assetItem.Value.Object is AudioClip)
                {
                    PopulateMenu<TinyAudioClipSettings>(assetItem, menu);
                }
                else if (assetItem.Value.Object is AnimationClip)
                {
                    PopulateMenu<TinyAnimationClipSettings>(assetItem, menu);
                }
                else
                {
                    PopulateMenu<TinyGenericAssetExportSettings>(assetItem, menu);
                }

                if (assetItem.Value.ExplicitReferences.Contains(TinyEditorApplication.Module.Ref))
                {
                    menu.AddItem(new GUIContent("Remove explicit reference"), false, () =>
                    {
                        module.Assets.Remove(TinyEditorApplication.Module.GetAsset(assetItem.Value.Object));
                        Reload();
                    });
                }
                else
                {
                    menu.AddItem(new GUIContent("Make reference explicit"), false, () =>
                    {
                        var obj = assetItem.Value.Object;
                        module.AddAsset(obj);
                        Reload();
                    });
                }
            }

            var entityGroupItem = item as EntityGroupItem;
            if (null != entityGroupItem)
            {
                var groupRef = (TinyEntityGroup.Reference)entityGroupItem.Value;
                if (MainModule.StartupEntityGroup.Equals(groupRef))
                {
                    menu.AddDisabledItem(new GUIContent("Set as Startup"));
                }
                else
                {
                    menu.AddItem(new GUIContent("Set as Startup"), false, () =>
                    {
                        MainModule.StartupEntityGroup = groupRef;
                        Reload();
                    });
                }
            }
        }

        private void PopulateMenu<TSettings>(AssetItem assetItem, GenericMenu menu)
            where TSettings : TinyAssetExportSettings, ICopyable<TSettings>, new()
        {
            var asset = MainModule.GetAsset(assetItem.Value.Object);
            var settings = asset?.ExportSettings as TSettings;
            if (null == settings)
            {
                menu.AddItem(new GUIContent("Override Export Settings"), false, () =>
                {
                    CreateAssetExportInfo<TSettings>(new [] { assetItem.Value });
                    Reload();
                });
            }
            else
            {
                menu.AddItem(new GUIContent("Remove Override Settings"), false, () =>
                {
                    ClearAssetExportSettings(new [] { assetItem.Value });
                    Reload();
                });
            }
        }

        private void CreateAssetExportInfo<TSettings>(IEnumerable<TinyAssetInfo> assetInfos)
            where TSettings : TinyAssetExportSettings, ICopyable<TSettings>, new()
        {
            var project = Project;
            var module = MainModule;

            foreach (var assetInfo in assetInfos)
            {
                CreateAssetExportInfo<TSettings>(project, module, assetInfo);
            }
        }

        private static void CreateAssetExportInfo<TSettings>(TinyProject project, TinyModule module, TinyAssetInfo assetInfo)
            where TSettings : TinyAssetExportSettings, ICopyable<TSettings>, new()
        {
            var asset = module.GetAsset(assetInfo.Object) ?? module.AddAsset(assetInfo.Object);
            var settings = asset.CreateExportSettings<TSettings>();
            settings.CopyFrom(TinyUtility.GetAssetExportSettings(project, assetInfo.Object) as TSettings);
        }

        private void ClearAssetExportSettings(IEnumerable<TinyAssetInfo> assetInfos)
        {
            var module = MainModule;

            foreach (var assetInfo in assetInfos)
            {
                var asset = module.GetAsset(assetInfo.Object);
                asset?.ClearExportSettings();;
            }
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var objects = DragAndDrop.objectReferences;

            if (objects.Length <= 0)
            {
                return DragAndDropVisualMode.None;
            }

            if (args.performDrop)
            {
                var module = TinyEditorApplication.Module;

                foreach (var @object in objects)
                {
                    var path = AssetDatabase.GetAssetPath(@object);

                    if (TinyScriptUtility.EndsWithTinyScriptExtension(path))
                    {
                        // scripts should not be added as asset references
                        continue;
                    }
                    module.AddAsset(@object);
                }
                SetFilter(Filters.Asset);
            }

            return DragAndDropVisualMode.Generic;
        }
    }
}
