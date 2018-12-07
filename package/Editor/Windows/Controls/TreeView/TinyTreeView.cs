

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
    /// <inheritdoc />
    /// <summary>
    /// Generic tree view with specialized types
    /// </summary>
    internal abstract class TinyTreeView<TState, TModel> : TinyTreeView
        where TState : TinyTreeState
        where TModel : TinyTreeModel
    {
        public new TState State => (TState) base.State;
        protected new TModel Model => (TModel) base.Model;

        protected TinyTreeView(TState state, TModel model) : base(state, model)
        {
        }
    }

    internal delegate void KeyboardEventHandler();
    internal delegate void ContentMenuEventHandler(GenericMenu menu);
    internal delegate void RenameEventHandler(TinyRegistryObjectBase @object);

    /// <inheritdoc cref="TreeView" />
    /// <summary>
    /// Base class to provide common functionality accross all Tiny tree classes
    /// </summary>
    internal abstract class TinyTreeView : TreeView, IDrawable, IDirtyable
    {
        private bool m_Dirty;

        protected TinyTreeState State { get; }
        protected TinyTreeModel Model { get; }

        public bool HasContextMenu { get; set; }

        public string SearchString
        {
            get { return searchString; }
            set { State.SearchString = searchString = value; }
        }

        /// <summary>
        /// Invoked when key events are pressed and the tree view has focus
        /// </summary>
        // public event KeyboardEventHandler OnKeyEvent;
        public event ContentMenuEventHandler OnContextMenuEvent;
        public event RenameEventHandler OnRenameEnded;

        protected TinyTreeView(TinyTreeState state, TinyTreeModel model) : base(state.TreeViewState,
            new MultiColumnHeader(state.MultiColumnHeaderState))
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;

            State = state;
            Model = model;

            multiColumnHeader.sortingChanged += HandleSortingChanged;

            Reload();
        }

        protected abstract override TreeViewItem BuildRoot();

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            SortTree(root, rows);
            return rows;
        }
        
        protected static MultiColumnHeaderState.Column CreateVersionColumn()
        {
            return new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Version"),
                headerTextAlignment = TextAlignment.Left,
                sortedAscending = true,
                sortingArrowAlignment = TextAlignment.Left,
                autoResize = false,
                canSort = true,
                maxWidth = 60,
                allowToggleVisibility = false
            };
        }

        public static MultiColumnHeaderState.Column CreateTagColumn()
        {
            return new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByLabel")),
                headerTextAlignment = TextAlignment.Center,
                autoResize = false,
                canSort = true,
                maxWidth = 60,
                allowToggleVisibility = false
            };
        }

        public static MultiColumnHeaderState.Column CreateNameColumn()
        {
            return new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Name"),
                headerTextAlignment = TextAlignment.Left,
                sortedAscending = true,
                sortingArrowAlignment = TextAlignment.Left,
                width = 150,
                minWidth = 60,
                autoResize = false,
                allowToggleVisibility = false
            };
        }

        protected static MultiColumnHeaderState.Column CreateModuleColumn()
        {
            return new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Module"),
                headerTextAlignment = TextAlignment.Left,
                sortedAscending = true,
                sortingArrowAlignment = TextAlignment.Left,
                width = 200,
                minWidth = 60,
                canSort = true,
                autoResize = false,
            };
        }

        protected static MultiColumnHeaderState.Column CreateAssetColumn()
        {
            return new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Asset"),
                headerTextAlignment = TextAlignment.Center,
                autoResize = false,
                canSort = false,
                width = 200,
                minWidth = 60,
                allowToggleVisibility = false
            };
        }

        protected static MultiColumnHeaderState.Column CreateDescriptionColumn()
        {
            return new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Description"),
                headerTextAlignment = TextAlignment.Left,
                width = 200,
                minWidth = 60,
                autoResize = true,
                canSort = false,
                allowToggleVisibility = true
            };
        }

        protected int GenerateInstanceId(IIdentified<TinyId> identified)
        {
            var instanceId = State.GetInstanceId(identified.Id);
            Model.Register(instanceId, identified);
            return instanceId;
        }

        protected void ClearInstanceIds()
        {
            Model.ClearIds();
            State.ClearUnusedInstanceIds(Model.Registry);
        }

        public IEnumerable<object> GetSelected()
        {
            return GetSelection().Select(id => Model.FindByInstanceId(id));
        }

        public void BeginRename(TinyId id)
        {
            var instanceId = State.GetInstanceId(id);
            var listItem = FindItem(instanceId, rootItem);
            BeginRename(listItem);
        }

        /// <summary>
        /// @TODO Make this a static utility method, on the registry?
        /// </summary>
        private void DeleteSelected()
        {
            var selections = GetSelected();

            foreach (var selection in selections)
            {
                if (!CanRenameOrDelete(selection))
                {
                    continue;
                }
                
                // Speical case for module
                if (selection is TinyModule.Reference)
                {
                    Model.Registry.Unregister(((TinyModule.Reference) selection).Id);
                }
                // Speical case for fields
                else if (selection is TinyField)
                {
                    var field = (TinyField) selection;
                    field?.DeclaringType.RemoveField(field);
                }
                else if (selection is IReference)
                {
                    var modules = TinyUtility.GetModules(Model.Registry, (IReference) selection).ToList();

                    foreach (var module in modules)
                    {
                        if (selection is TinyType.Reference)
                        {
                            var reference = (TinyType.Reference) selection;
                            module.RemoveTypeReference(reference);
                            Model.Registry.Unregister(reference.Id);
                        }
                        else if (selection is TinyEntityGroup.Reference)
                        {
                            var reference = (TinyEntityGroup.Reference) selection;
                            module.RemoveEntityGroupReference(reference);
                            Model.Registry.Unregister(reference.Id);
                        }
                    }
                }
            }
            
            Reload();
        }

        public bool CanRenameOrDelete(object selection)
        {
            if (null == selection)
            {
                return false;
            }
            
            // Special case for module
            if (selection is TinyModule.Reference)
            {
                return false;
            }
            
            if (selection is IReference)
            {
                if (!IncludedByMainModule(selection)) 
                {
                    return false;
                }
            }
            
            if (selection is TinyEntityGroup.Reference)
            {
                var group = ((TinyEntityGroup.Reference) selection).Dereference(Model.Registry);
                return string.IsNullOrEmpty(group?.PersistenceId);
            }
            
            if (selection is TinyType.Reference)
            {
                var type = ((TinyType.Reference) selection).Dereference(Model.Registry);
                return string.IsNullOrEmpty(type?.PersistenceId);
            }
            
            // Special case for fields
            if (selection is TinyField)
            {
                var field = (TinyField) selection;

                if (TinyUtility.GetModules(field.DeclaringType).FirstOrDefault() != Model.MainModule.Dereference(Model.Registry))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IncludedByMainModule(object selection)
        {
            var module = TinyUtility.GetModules(Model.Registry, (IReference) selection).FirstOrDefault();
            return module == Model.MainModule.Dereference(Model.Registry);
        }

        public void SetDirty()
        {
            m_Dirty = true;
        }

        public virtual bool DrawLayout()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                Reload();
            }

            GUILayout.Space(1);

            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            OnGUI(rect);

            return false;
        }

        protected override void KeyEvent()
        {
            base.KeyEvent();

            // OnKeyEvent?.Invoke();

            if (Event.current.type != EventType.KeyDown)
            {
                return;
            }

            if (Event.current.keyCode != KeyCode.Delete)
            {
                return;
            }

            var selections = GetSelected().NotNull();

            if (!selections.Any(CanRenameOrDelete))
            {
                return;
            }
            
            if (EditorUtility.DisplayDialog(string.Empty, "Are you sure you want to destroy the selected object(s)?",
                "Destroy", "No"))
            {
                DeleteSelected();
            }
        }

        protected override void ContextClicked()
        {
            base.ContextClicked();

            var menu = new GenericMenu();

            OnContextMenuEvent?.Invoke(menu);
            
            var selections = GetSelected().NotNull();

            if (selections.Any(CanRenameOrDelete))
            {
                menu.AddItem(new GUIContent("Destroy"), false, () =>
                {
                    if (EditorUtility.DisplayDialog(string.Empty, "Are you sure you want to destroy the selected object(s)?", "Destroy", "No"))
                    {
                        DeleteSelected();
                    }
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Destroy"));
            }

            menu.ShowAsContext();
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return true;
            }

            if (item.hasChildren && item.children[0] != null)
            {
                if (item.children.Any(child => DoesItemMatchSearch(child, search)))
                {
                    return true;
                }
            }

            if (string.IsNullOrEmpty(item.displayName))
            {
                return false;
            }

            return item.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SortTree(TreeViewItem root, ICollection<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return;
            }

            SortChildren(root);

            TreeToList(root, rows);
        }

        protected virtual void SortChildren(TreeViewItem item)
        {
            if (item.children == null)
            {
                return;
            }

            foreach (var child in item.children)
            {
                SortChildren(child);
            }

            item.children = SortRows(item.children);
        }

        protected virtual List<TreeViewItem> SortRows(List<TreeViewItem> rows)
        {
            return rows;
        }

        private void TreeToList(TreeViewItem root, ICollection<TreeViewItem> result)
        {
            result.Clear();

            if (root.children == null)
            {
                return;
            }

            var stack = new Stack<TreeViewItem>();

            for (var i = root.children.Count - 1; i >= 0; i--)
            {
                stack.Push(root.children[i]);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (!DoesItemMatchSearch(current, searchString))
                {
                    continue;
                }

                result.Add(current);

                if (!current.hasChildren || current.children[0] == null)
                {
                    continue;
                }

                if (!IsExpanded(current.id) && string.IsNullOrEmpty(searchString))
                {
                    continue;
                }

                for (var i = current.children.Count - 1; i >= 0; i--)
                {
                    if (!DoesItemMatchSearch(current.children[i], searchString))
                    {
                        continue;
                    }

                    stack.Push(current.children[i]);
                }
            }
        }

        protected override bool CanRename(TreeViewItem item)
        {
            var treeViewItem = item as TinyTreeViewItem;
            return treeViewItem != null && treeViewItem.Editable;
        }

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            var cellRect = GetCellRectForTreeFoldouts(rowRect);
            CenterRectUsingSingleLineHeight(ref cellRect);
            return base.GetRenameRect(cellRect, row, item);
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename)
            {
                return;
            }
            
            var @object = Model.FindByInstanceId(args.itemID);

            if (args.originalName == args.newName)
            {
                return;
            }

            if (!IsValidName(@object, args.newName))
            {
                Debug.LogWarningFormat("Invalid name  [{0}]", args.newName);
                return;
            }

            // @HACK
            if (@object is TinyField)
            {
                var field = (TinyField) @object;
                field.Name = args.newName;
            }
            else if (@object is IReference)
            {
                var obj = Model.Registry.Dereference((IReference)@object);

                if (null == obj)
                {
                    return;
                }

                obj.Name = args.newName;
                OnRenameEnded?.Invoke(obj);
            }

            Reload();
            SetSelection(new List<int> {args.itemID}, TreeViewSelectionOptions.RevealAndFrame);
        }

        public void SetSelection(TinyId id)
        {
            SetSelection(new List<int> { State.GetInstanceId(id) }, TreeViewSelectionOptions.RevealAndFrame | 
                                                                    TreeViewSelectionOptions.FireSelectionChanged);
        }

        protected virtual bool IsValidName(object @object, string name)
        {
            return TinyUtility.IsValidObjectName(name);
        }

        private void HandleSortingChanged(MultiColumnHeader header)
        {
            SortTree(rootItem, GetRows());
        }
    }
}

