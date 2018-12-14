
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
    internal partial class RegistryTreeView : TreeView
    {
        [Flags]
        public enum Filters
        {
            Nothing = 0,
            Project = 1 << 0,
            Module = 1 << 1,
            Enum = 1 << 2,
            Struct = 1 << 3,
            Component = 1 << 4,
            Type = Enum | Struct | Component,
            EntityGroup = 1 << 5,
            Entity = 1 << 6,
            Asset = 1 << 7,
            ComponentSystem = 1 << 8,
            EntityFilter = 1 << 9,
            ComponentBehaviour = 1 << 10,
            Script = ComponentSystem | EntityFilter | ComponentBehaviour,
            Everything = ~Nothing
        }

        [Serializable]
        internal class State : TreeViewState
        {
            public string CurrentSearchFilter;
        }

        public struct GUIArgs
        {
            public Rect rect;
            public float indent;
            public TinyModule MainModule;
            public Action<GUIArgs> DefaultOnGUI;
            public bool IsMouseOver;
        }

        #region Fields
        protected readonly IRegistry Registry;
        private readonly TinyProject.Reference ProjectRef;
        private readonly TinyModule.Reference MainModuleRef;
        private Filters m_Filter = Filters.Everything;
        private TinyToolbar m_ToolBar;
        #endregion

        #region Properties

        public bool AlternatingBackground
        {
            get => showAlternatingRowBackgrounds;
            set => showAlternatingRowBackgrounds = value;
        }
        
        public Filters Filter => m_Filter;
        protected TinyProject Project => ProjectRef.Dereference(Registry);
        protected TinyModule MainModule => MainModuleRef.Dereference(Registry);
        public RootItemFactory<TinyProject> CreateRootProject { get; } = new RootItemFactory<TinyProject> { Create = Defaults.CreateItem, ShouldCreateRootOfType = Defaults.AlwaysCreate, MatchesFilter = Defaults.MatchesFilter };
        public RootItemFactory<TinyModule> CreateRootModule { get; } = new RootItemFactory<TinyModule> { Create = Defaults.CreateItem, ShouldCreateRootOfType = Defaults.AlwaysCreate, MatchesFilter = Defaults.MatchesFilter };
        public RootItemFactory<TinyType> CreateRootType { get; } = new RootItemFactory<TinyType> { Create = Defaults.CreateItem, ShouldCreateRootOfType = Defaults.AlwaysCreate, MatchesFilter = Defaults.MatchesFilter };
        public RootItemFactory<TinyEntityGroup> CreateRootEntityGroup { get; } = new RootItemFactory<TinyEntityGroup> { Create = Defaults.CreateItem, ShouldCreateRootOfType = Defaults.AlwaysCreate, MatchesFilter = Defaults.MatchesFilter };
        public RootItemFactory<TinyEntity> CreateRootEntity { get; } = new RootItemFactory<TinyEntity> { Create = Defaults.CreateItem, ShouldCreateRootOfType = Defaults.AlwaysCreate, MatchesFilter = Defaults.MatchesFilter };
        public RootItemFactory<TinyAssetInfo> CreateRootAsset { get; } = new RootItemFactory<TinyAssetInfo> { Create = Defaults.CreateItem, ShouldCreateRootOfType = Defaults.AlwaysCreate, MatchesFilter = Defaults.MatchesFilter };
        public RootItemFactory<IScriptObject> CreateRootScript { get; } = new RootItemFactory<IScriptObject> { Create = Defaults.CreateItem, ShouldCreateRootOfType = Defaults.AlwaysCreate, MatchesFilter = Defaults.MatchesFilter };
        #endregion

        #region API
        public RegistryTreeView(IRegistry registry, State state)
            : base(state)
        {
            Registry = registry;
            ProjectRef = (TinyProject.Reference)TinyEditorApplication.Project;
            MainModuleRef = (TinyModule.Reference)TinyEditorApplication.Module;
            m_ToolBar = CreateToolbar();
            Reload();
        }

        public void SetFilter(Filters filter)
        {
            if (m_Filter != filter)
            {
                m_Filter = filter;
                Reload();
            }
        }

        public void DrawToolbar()
        {
            m_ToolBar.DrawLayout();
        }

        protected virtual void CacheResources()
        {
        }

        protected virtual void OverrideDefaults()
        {
        }

        protected virtual void OnItemClicked(ItemBase item)
        {
            item.OnSingleClick();
        }

        protected virtual void OnItemDoubleClicked(ItemBase item)
        {
            item.OnDoubleClick();
        }

        protected virtual void OnItemContextClicked(ItemBase item, GenericMenu menu)
        {
            item.OnContextClicked(menu);
        }

        protected virtual void OnItemRowGUI(ItemBase item, GUIArgs args)
        {
            item.OnGUI(args);
        }

        protected virtual void OnSelectionChanged(List<ItemBase> selection)
        {
            Selection.instanceIDs = selection
                .Select(treeItem =>
                {
                    if (treeItem is AssetItem assetItem)
                    {
                        return assetItem.Value.Object;
                    }

                    if (treeItem is ScriptItem scriptItem)
                    {
                        var source = scriptItem.Value.Source.File;
                        source = Persistence.GetPathRelativeToProjectPath(source);
                        return AssetDatabase.LoadAssetAtPath<TextAsset>(source);
                    }

                    var item = treeItem.GetValue() as IRegistryObject;
                    if (item is TinyModule && item.Name == TinyProject.MainProjectName)
                    {
                        item = TinyEditorApplication.Project;
                    }

                    return AssetDatabase.LoadAssetAtPath<TinyScriptableObject>(Persistence.GetAssetPath(item as IPersistentObject));
                }).NotNull().Select(obj => obj.GetInstanceID()).ToArray();
        }
        #endregion

        #region Implementation
        private void CreateRootItems<TValue>(RootItemFactory<TValue> factory, Root root)
            where TValue : class, IRegistryObject
        {
            if (factory.ShouldCreateRootOfType == Defaults.NeverCreate)
            {
                return;
            }
            foreach (var value in Registry.FindAllByType<TValue>().OrderBy(p => p.Name))
            {
                if (factory.MatchesFilter(value, m_Filter) && factory.ShouldCreateRootOfType(value))
                {
                    root.AddChild(factory.Create(value, this));
                }
            }
        }

        private void CreateRootItems(RootItemFactory<TinyAssetInfo> factory, Root root)
        {
            if (factory.ShouldCreateRootOfType == Defaults.NeverCreate)
            {
                return;
            }
            foreach (var assetInfo in AssetIterator.EnumerateRootAssets(TinyEditorApplication.Module).OrderBy(i => i.Name))
            {
                if (factory.MatchesFilter(assetInfo, m_Filter) && factory.ShouldCreateRootOfType(assetInfo))
                {
                    root.AddChild(factory.Create(assetInfo, this));
                }
            }
        }
        
        private void CreateRootItems(RootItemFactory<IScriptObject> factory, Root root)
        {
            if (factory.ShouldCreateRootOfType == Defaults.NeverCreate)
            {
                return;
            }
            var scripting = TinyEditorApplication.EditorContext.Context.GetManager<IScriptingManager>();
            var meta = scripting.Metadata;
            if (meta != null)
            {
                foreach (var scriptObject in meta.AllObjects)
                {
                    if (factory.MatchesFilter(scriptObject, m_Filter) && factory.ShouldCreateRootOfType(scriptObject))
                    {
                        root.AddChild(factory.Create(scriptObject, this));
                    }
                }
            }
        }
        
        protected sealed override TreeViewItem BuildRoot()
        {
            CacheResources();
            OverrideDefaults();

            var root = new Root(this);
            CreateRootItems(CreateRootProject, root);
            CreateRootItems(CreateRootModule, root);
            CreateRootItems(CreateRootType, root);
            CreateRootItems(CreateRootEntityGroup, root);
            CreateRootItems(CreateRootEntity, root);
            CreateRootItems(CreateRootAsset, root);
            CreateRootItems(CreateRootScript, root);
            return root;
        }

        protected sealed override void SingleClickedItem(int id)
        {
            if (FindItem(id, rootItem) is ItemBase item)
            {
                OnItemClicked(item);
            }
        }

        protected sealed override void DoubleClickedItem(int id)
        {
            if (FindItem(id, rootItem) is ItemBase item)
            {
                OnItemDoubleClicked(item);
            }
        }

        protected sealed override void ContextClickedItem(int id)
        {
            if (FindItem(id, rootItem) is ItemBase item)
            {
                var menu = new GenericMenu();
                OnItemContextClicked(item, menu);
                if (menu.GetItemCount() > 0)
                {
                    menu.ShowAsContext();
                }
            }
        }

        protected sealed override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return (item as ItemBase)?.GetItemHeight(MainModule) ?? base.GetCustomRowHeight(row, item);
        }

        protected sealed override void RowGUI(RowGUIArgs args)
        {
            if (args.item is ItemBase item)
            {
                OnItemRowGUI(item, new GUIArgs { rect = args.rowRect, indent = GetContentIndent(item), MainModule = MainModule,
                    DefaultOnGUI = (a) =>
                    {
                        args.rowRect = a.rect;
                        base.RowGUI(args);
                    },
                    IsMouseOver = args.rowRect.Contains(Event.current.mousePosition)
                });
                return;
            }

            var rect = args.rowRect;
            rect.height = EditorGUIUtility.singleLineHeight;
            args.rowRect = rect;
            base.RowGUI(args);
        }

        protected sealed override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            OnSelectionChanged(selectedIds.Select(id => FindItem(id, rootItem) as ItemBase).NotNull().ToList());
        }

        private TinyToolbar CreateToolbar()
        {
            return new TinyToolbar().Add(new TinyToolbar.Search
            {
                Alignment = TinyToolbar.Alignment.Center,
                SearchString = searchString,
                Changed = str =>
                {
                    searchString = str;
                }
            });
        }

        #endregion
    }
}
