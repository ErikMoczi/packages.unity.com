using System;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.Entities.Editor
{
    public interface IEntitySelectionWindow : IWorldSelectionWindow
    {
        Entity EntitySelection { set; }
    }

    public interface IWorldSelectionWindow
    {
        World WorldSelection { get; }
    }
    
    public class EntityListView : TreeView {
        private readonly Dictionary<int, Entity> entitiesById = new Dictionary<int, Entity>();

        public ComponentGroup SelectedComponentGroup
        {
            get { return selectedComponentGroup; }
            set
            {
                if (selectedComponentGroup != value)
                {
                    selectedComponentGroup = value;
                    Reload();
                }
            }
        }
        private ComponentGroup selectedComponentGroup;

        IEntitySelectionWindow window;

        public EntityListView(TreeViewState state, ComponentGroup componentGroup, IEntitySelectionWindow window) : base(state)
        {
            this.window = window;
            SelectedComponentGroup = componentGroup;
            Reload();
            SelectionChanged(GetSelection());
        }
        
        public void RefreshData()
        {
            Reload();
        }

        private TreeViewItem CreateEntityItem(Entity entity)
        {
            entitiesById.Add(entity.Index, entity);
            return new TreeViewItem { id = entity.Index };
        }

        protected override TreeViewItem BuildRoot()
        {
            entitiesById.Clear();
            var managerId = -1;
            var root  = new TreeViewItem { id = managerId--, depth = -1, displayName = "Root" };
            if (window?.WorldSelection == null)
            {
                root.AddChild(new TreeViewItem { id = managerId, displayName = "No world selected"});
            }
            else
            {
                if (SelectedComponentGroup == null)
                {
                    var array = window.WorldSelection.GetExistingManager<EntityManager>().GetAllEntities(Allocator.Temp);
                    for (var i = 0; i < array.Length; ++i)
                        root.AddChild(CreateEntityItem(array[i]));
                    array.Dispose();
                }
                else
                {
                    window.WorldSelection.GetExistingManager<EntityManager>().CompleteAllJobs();
                    var entityArray = SelectedComponentGroup.GetEntityArray();
                    for (var i = 0; i < entityArray.Length; ++i)
                        root.AddChild(CreateEntityItem(entityArray[i]));
                }

                if (entitiesById.Count == 0)
                {
                    root.AddChild(new TreeViewItem { id = managerId, displayName = "ComponentGroup is empty"});
                }
                SetupDepthsFromParentsAndChildren(root);
            }
            return root;
        }

        public override void OnGUI(Rect rect)
        {
            if (window?.WorldSelection?.GetExistingManager<EntityManager>()?.IsCreated == true)
                base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item.displayName == null)
                args.label = args.item.displayName = $"Entity {entitiesById[args.item.id].Index.ToString()}";
            base.RowGUI(args);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (window == null)
                return;
            if (selectedIds.Count > 0 && entitiesById.ContainsKey(selectedIds[0]))
            {
                window.EntitySelection = entitiesById[selectedIds[0]];
            }
            else
            {
                window.EntitySelection = Entity.Null;
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public void SelectNothing()
        {
            SetSelection(new List<int>());
        }

        public void SetEntitySelection(Entity entitySelection)
        {
            if (entitySelection != Entity.Null && window.WorldSelection.GetExistingManager<EntityManager>().Exists(entitySelection))
                SetSelection(new List<int>{entitySelection.Index});
        }
    }
}
