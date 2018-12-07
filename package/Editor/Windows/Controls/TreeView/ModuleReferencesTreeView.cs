
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class ModuleReferencesTreeView : RegistryTreeView
    {
        #region State
        public const string SharedStateKey = "Tiny.ModuleReferenceTreeView.TreeState";

        [Serializable]
        internal class ReferencedState : State
        {
            // We are not saving anything new, but we could in the future.
        }
        #endregion

        #region Items
        internal class ReferencedModuleItem : ModuleItem
        {
            public ReferencedModuleItem(TinyModule module, RegistryTreeView treeView)
                : base(module, treeView)
            {
                if (module.Dependencies.Count > 0)
                {
                    var dependency = new LabelItem("Dependencies", treeView);
                    AddChild(dependency);
                    foreach (var d in module.Dependencies.Deref(Registry).OrderBy(m => m.Name))
                    {
                        if (d == module)
                        {
                            continue;
                        }

                        dependency.AddChild(new DependencyItem(d, module, treeView));
                    }
                }

                if (module.Types.Any())
                {
                    var includes = new LabelItem("Includes", treeView);
                    AddChild(includes);
                    foreach (var type in module.Types.Deref(Registry).OrderBy(t => t.Name))
                    {
                        includes.AddChild(new IncludeItem(type, treeView));
                    }
                }
            }

            public override void OnGUI(GUIArgs args)
            {
                var oldEnable = GUI.enabled;

                var canEdit = !(Value.IsRequired || Value == args.MainModule);
                GUI.enabled &= canEdit;
                var content = new GUIContent(Value.Name, Value.Documentation.Summary);

                var rect = args.rect;
                rect.x += args.indent;
                rect.width -= args.indent;

                const float toggleSize = 15;
                var toggleRect = rect;
                toggleRect.width = toggleSize;

                if (canEdit)
                {
                    if (EditorGUI.ToggleLeft(toggleRect, GUIContent.none, args.MainModule.Dependencies.Contains(Value.Ref)))
                    {
                        args.MainModule.AddExplicitModuleDependency(Value.Ref);
                    }
                    else
                    {
                        args.MainModule.RemoveExplicitModuleDependency(Value.Ref);
                    }
                }

                var labelRect = rect;
                labelRect.width -= toggleSize;
                labelRect.x += toggleSize;
                EditorGUI.LabelField(labelRect, content);

                GUI.enabled = oldEnable;
            }
        }

        internal class DependencyItem : ModuleItem
        {
            private TinyModule DependencyOf;

            public DependencyItem(TinyModule module, TinyModule dependencyOf, RegistryTreeView treeView)
                : base(module, treeView)
            {
                DependencyOf = dependencyOf;
            }

            public override int id => Value.Id.GetHashCode() * 17 ^ DependencyOf.Id.GetHashCode();
        }

        internal class IncludeItem : TypeItem
        {
            public IncludeItem(TinyType type, RegistryTreeView treeView)
                : base(type, treeView) { }

            private Texture2D GetIconForType(TinyType type)
            {
                if (type.IsComponent)
                {
                    return TinyIcons.Component;
                }

                if (type.IsStruct)
                {
                    return TinyIcons.Struct;
                }

                if (type.IsEnum)
                {
                    return TinyIcons.Enum;
                }

                return null;
            }

            public override Texture2D icon => GetIconForType(Value);
        }
        #endregion // Items

        private readonly TinyModule.Reference ModuleRef;
        private TinyModule Module => ModuleRef.Dereference(Registry);
        private readonly EditorContextType Type;
        private TinyModule CurrentlyActive => TinyEditorApplication.Module;
        private static ReferencedState SharedState = null;

        public ModuleReferencesTreeView(TinyProject project, ReferencedState state)
            : this(project.Module.Dereference(project.Registry), EditorContextType.Project, state) { }

        public ModuleReferencesTreeView(TinyModule module, EditorContextType contextType, ReferencedState state)
            : base(module.Registry, state)
        {
            ModuleRef = (TinyModule.Reference)module;
            Type = contextType;
            SetFilter(Filters.Module);
            Reload();
        }

        public static ReferencedState LoadSharedState()
        {
            if (null == SharedState)
            {
                var stateSave = EditorPrefs.GetString(SharedStateKey, null);
                return (SharedState = string.IsNullOrEmpty(stateSave) ? new ReferencedState() : JsonUtility.FromJson<ReferencedState>(stateSave));
            }

            return SharedState;
        }

        public static void SaveSharedState()
        {
            var saveState = JsonUtility.ToJson(SharedState);
            EditorPrefs.SetString(SharedStateKey, saveState);
        }

        protected override void OverrideDefaults()
        {
            CreateRootModule.ShouldCreateRootOfType = ShouldCreateModule;
            CreateRootModule.Create = (m, t) => new ReferencedModuleItem(m, t);
        }

        private bool ShouldCreateModule(TinyModule module)
        {
            return !(module.Name == TinyProject.MainProjectName && (Type == EditorContextType.Module || Selection.activeObject is UTModule));
        }

        protected override void OnItemClicked(ItemBase item)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TinyScriptableObject>(Persistence.GetAssetPath(item.GetValue() as IPersistentObject)));
        }

        protected override void OnItemDoubleClicked(ItemBase item)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TinyScriptableObject>(Persistence.GetAssetPath(item.GetValue() as IPersistentObject));
            if (null != asset)
            {
                Selection.activeObject = asset;
            }
        }

        public override void OnGUI(Rect rect)
        {
            var oldEnable = GUI.enabled;
            GUI.enabled = true;
            try
            {
                base.OnGUI(rect);
            }
            finally
            {
                GUI.enabled = oldEnable;
            }
        }

        protected override void OnItemRowGUI(ItemBase item, GUIArgs args)
        {
            var oldEnable = GUI.enabled;
            GUI.enabled = !(Module.IsRuntimeIncluded || CurrentlyActive != Module);
            base.OnItemRowGUI(item, args);
            GUI.enabled = oldEnable;
        }

        protected override void OnSelectionChanged(List<ItemBase> selection)
        {
            // We'll handle it our way.
        }
    }
}
