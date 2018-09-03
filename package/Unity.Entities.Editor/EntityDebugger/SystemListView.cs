using System;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Entities.Editor
{
    public interface ISystemSelectionWindow
    {
        ScriptBehaviourManager SystemSelection { set; }
    }
    
    public class SystemListView : TreeView
    {
        private readonly Dictionary<Type, List<ScriptBehaviourManager>> managersByGroup = new Dictionary<Type, List<ScriptBehaviourManager>>();
        private readonly List<ScriptBehaviourManager> floatingManagers = new List<ScriptBehaviourManager>();
        private readonly Dictionary<int, ScriptBehaviourManager> managersByID = new Dictionary<int, ScriptBehaviourManager>();
        private readonly Dictionary<ScriptBehaviourManager, Recorder> recordersByManager = new Dictionary<ScriptBehaviourManager, Recorder>();

        private readonly World world;

        private const float kToggleWidth = 22f;
        private const float kTimingWidth = 70f;

        private readonly ISystemSelectionWindow window;

        private static GUIStyle RightAlignedLabel
        {
            get
            {
                if (rightAlignedText == null)
                {
                    rightAlignedText = new GUIStyle(GUI.skin.label);
                    rightAlignedText.alignment = TextAnchor.MiddleRight;
                }

                return rightAlignedText;
            }
        }

        private static GUIStyle rightAlignedText;

        private static MultiColumnHeaderState GetHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = GUIContent.none,
                    contextMenuText = "Enabled",
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    width = kToggleWidth,
                    minWidth = kToggleWidth,
                    maxWidth = kToggleWidth,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("System Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortingArrowAlignment = TextAlignment.Right,
                    canSort = true,
                    sortedAscending = true,
                    width = 100,
                    minWidth = 100,
                    maxWidth = 2000,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("main (ms)"),
                    headerTextAlignment = TextAlignment.Right,
                    canSort = false,
                    width = kTimingWidth,
                    minWidth = kTimingWidth,
                    maxWidth = kTimingWidth,
                    autoResize = false,
                    allowToggleVisibility = false
                }
            };
            
            return new MultiColumnHeaderState(columns);
        }

        private static TreeViewState GetStateForWorld(World world, List<TreeViewState> states, List<string> stateNames)
        {
            if (world == null)
                return new TreeViewState();
            
            var currentWorldName = world.Name;

            var stateForCurrentWorld = states.Where((t, i) => stateNames[i] == currentWorldName).FirstOrDefault();
            if (stateForCurrentWorld != null)
                return stateForCurrentWorld;
            
            stateForCurrentWorld = new TreeViewState();
            states.Add(stateForCurrentWorld);
            stateNames.Add(currentWorldName);
            return stateForCurrentWorld;
        }

        public static SystemListView CreateList(World world, List<TreeViewState> states, List<string> stateNames, ISystemSelectionWindow window)
        {
            var state = GetStateForWorld(world, states, stateNames);
            var header = new MultiColumnHeader(GetHeaderState());
            return new SystemListView(state, header, world, window);
        }
        
        private SystemListView(TreeViewState state, MultiColumnHeader header, World world, ISystemSelectionWindow window) : base(state, header)
        {
            this.window = window;
            this.world = world;
            columnIndexForTreeFoldouts = 1;
            Reload();
            SelectionChanged(GetSelection());
        }

        static int CompareSystem(ScriptBehaviourManager x, ScriptBehaviourManager y)
        {
            var xIsEntityManager = x is EntityManager;
            var yIsEntityManager = y is EntityManager;
            if (xIsEntityManager == yIsEntityManager)
            {
                return string.CompareOrdinal(x.GetType().Name, y.GetType().Name);
            }
            else
            {
                return xIsEntityManager ? -1 : 1;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            managersByGroup.Clear();
            managersByID.Clear();
            floatingManagers.Clear();
            recordersByManager.Clear();

            if (world != null)
            {
                Dictionary<Type, ScriptBehaviourUpdateOrder.ScriptBehaviourGroup> allGroups;
                Dictionary<Type, ScriptBehaviourUpdateOrder.DependantBehavior> dependencies;
                ScriptBehaviourUpdateOrder.CollectGroups(world.BehaviourManagers, out allGroups, out dependencies);
            
                foreach (var manager in world.BehaviourManagers)
                {
                    var hasGroup = false;
                    foreach (var attributeData in manager.GetType().GetCustomAttributesData())
                    {
                        if (attributeData.AttributeType == typeof(UpdateInGroupAttribute))
                        {
                            var groupType = (Type) attributeData.ConstructorArguments[0].Value;
                            if (!managersByGroup.ContainsKey(groupType))
                                managersByGroup[groupType] = new List<ScriptBehaviourManager>{manager};
                            else
                                managersByGroup[groupType].Add(manager);
                            hasGroup = true;
                            break;
                        }
                    }

                    if (!hasGroup)
                    {
                        floatingManagers.Add(manager);
                    }
                }
                foreach (var managerSet in managersByGroup.Values)
                {
                    managerSet.Sort(CompareSystem);
                }
            }
            floatingManagers.Sort(CompareSystem);
            
            var currentID = 0;
            var root  = new TreeViewItem { id = currentID++, depth = -1, displayName = "Root" };
            if (managersByGroup.Count == 0 && floatingManagers.Count == 0)
            {
                root.AddChild(new TreeViewItem { id = currentID++, displayName = "No ComponentSystems Loaded"});
            }
            else
            {

                foreach (var manager in floatingManagers)
                {
                    managersByID.Add(currentID, manager);
                    var recorder = Recorder.Get($"{world.Name} {manager.GetType().FullName}");
                    recordersByManager.Add(manager, recorder);
                    recorder.enabled = true;
                    var managerItem = new TreeViewItem { id = currentID++, displayName = manager.GetType().Name.ToString() };
                    root.AddChild(managerItem);
                }
                foreach (var group in (from g in managersByGroup.Keys orderby g.Name select g))
                {
                    var groupItem = new TreeViewItem { id = currentID++, displayName = group.Name };
                    root.AddChild(groupItem);
                    foreach (var manager in managersByGroup[group])
                    {
                        managersByID.Add(currentID, manager);
                        var recorder = Recorder.Get($"{world.Name} {manager.GetType().FullName}");
                        recordersByManager.Add(manager, recorder);
                        recorder.enabled = true;
                        
                        var managerItem = new TreeViewItem { id = currentID++, displayName = manager.GetType().Name.ToString() };
                        groupItem.AddChild(managerItem);
                    }
                }
                SetupDepthsFromParentsAndChildren(root);
            }
            return root;
        }

        protected override void RowGUI (RowGUIArgs args)
        {
            if (args.item.depth == -1)
                return;
            var item = args.item;

            var enabled = GUI.enabled;
            
            if (managersByID.ContainsKey(item.id))
            {
                var manager = managersByID[item.id];
                var toggleRect = args.GetCellRect(0);
                toggleRect.xMin = toggleRect.xMin + 4f;
                manager.Enabled = GUI.Toggle(toggleRect, manager.Enabled, GUIContent.none);
                
                GUI.enabled = (manager as ComponentSystemBase)?.ShouldRunSystem() ?? true;

                var timingRect = args.GetCellRect(2);
                var recorder = recordersByManager[manager];
                GUI.Label(timingRect, (recorder.elapsedNanoseconds * 0.000001f).ToString("f2"), RightAlignedLabel);
            }

            var indent = GetContentIndent(item);
            var nameRect = args.GetCellRect(1);
            nameRect.xMin = nameRect.xMin + indent;
            GUI.Label(nameRect, item.displayName);
            GUI.enabled = enabled;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (window == null)
                return;
            if (selectedIds.Count > 0 && managersByID.ContainsKey(selectedIds[0]))
            {
                window.SystemSelection = managersByID[selectedIds[0]];
            }
            else
            {
                window.SystemSelection = null;
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

    }
}
