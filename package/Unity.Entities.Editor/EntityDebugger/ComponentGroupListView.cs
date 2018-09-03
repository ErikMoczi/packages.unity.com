using System;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Unity.Entities.Editor
{
    public interface IComponentGroupSelectionWindow : IWorldSelectionWindow
    {
        ComponentGroup ComponentGroupSelection { set; }
    }
    
    public class ComponentGroupListView : TreeView {
        private readonly Dictionary<int, ComponentGroup> componentGroupsById = new Dictionary<int, ComponentGroup>();

        public ComponentSystemBase SelectedSystem
        {
            get { return selectedSystem; }
            set
            {
                if (selectedSystem != value)
                {
                    selectedSystem = value;
                    Reload();
                }
            }
        }
        private ComponentSystemBase selectedSystem;

        IComponentGroupSelectionWindow window;

        private static TreeViewState GetStateForSystem(ComponentSystemBase system, List<TreeViewState> states, List<string> stateNames)
        {
            if (system == null)
                return new TreeViewState();
            
            var currentSystemName = system.GetType().FullName;

            var stateForCurrentSystem = states.Where((t, i) => stateNames[i] == currentSystemName).FirstOrDefault();
            if (stateForCurrentSystem != null)
                return stateForCurrentSystem;
            
            stateForCurrentSystem = new TreeViewState();
            if (system.ComponentGroups.Length > 0)
                stateForCurrentSystem.expandedIDs = new List<int> {1};
            states.Add(stateForCurrentSystem);
            stateNames.Add(currentSystemName);
            return stateForCurrentSystem;
        }

        public static ComponentGroupListView CreateList(ComponentSystemBase system, List<TreeViewState> states, List<string> stateNames,
            IComponentGroupSelectionWindow window)
        {
            var state = GetStateForSystem(system, states, stateNames);
            return new ComponentGroupListView(state, system, window);
        }

        public ComponentGroupListView(TreeViewState state, ComponentSystemBase system, IComponentGroupSelectionWindow window) : base(state)
        {
            this.window = window;
            selectedSystem = system;
            Reload();
            SelectionChanged(GetSelection());
        }

        public float Height => Mathf.Max(selectedSystem?.ComponentGroups.Length ?? 0, 1)*rowHeight;

        protected override TreeViewItem BuildRoot()
        {
            componentGroupsById.Clear();
            var currentId = 0;
            var root  = new TreeViewItem { id = currentId++, depth = -1, displayName = "Root" };
            if (window?.WorldSelection == null)
            {
                root.AddChild(new TreeViewItem { id = currentId, displayName = "No world selected"});
            }
            else if (SelectedSystem == null)
            {
                root.AddChild(new TreeViewItem { id = currentId, displayName = "Null System"});
            }
            else if (SelectedSystem.ComponentGroups.Length == 0)
            {
                root.AddChild(new TreeViewItem { id = currentId, displayName = "No Component Groups in Manager"});
            }
            else
            {
                foreach (var group in SelectedSystem.ComponentGroups)
                {
                    componentGroupsById.Add(currentId, group);
                    var types = group.Types;
                    var groupName = string.Join(", ", (from x in types.Skip(types.Length > 1 ? 1 : 0) select x.Name).ToArray());

                    var groupItem = new TreeViewItem { id = currentId++, displayName = groupName };
                    root.AddChild(groupItem);
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
            base.RowGUI(args);
            if (!componentGroupsById.ContainsKey(args.item.id))
                return;
            var countString = componentGroupsById[args.item.id].CalculateLength().ToString();
            DefaultGUI.LabelRightAligned(args.rowRect, countString, args.selected, args.focused);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (window == null)
                return;
            if (selectedIds.Count > 0 && componentGroupsById.ContainsKey(selectedIds[0]))
            {
                window.ComponentGroupSelection = componentGroupsById[selectedIds[0]];
            }
            else
            {
                window.ComponentGroupSelection = null;
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }
    }
}
