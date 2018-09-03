using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Entities.Properties;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace Unity.Entities.Editor
{
    public class EntityDebugger : EditorWindow, ISystemSelectionWindow, IEntitySelectionWindow, IComponentGroupSelectionWindow {
        private const float kSystemListWidth = 350f;

        [MenuItem("Window/Entity Debugger", false, 2017)]
        static void OpenWindow()
        {
            GetWindow<EntityDebugger>("Entity Debugger");
        }

        private static GUIStyle Box
        {
            get
            {
                if (box == null)
                {
                    box = new GUIStyle(GUI.skin.box);
                    box.margin = new RectOffset();
                    box.padding = new RectOffset(1, 1, 1, 1);
                }

                return box;
            }
        }

        private static GUIStyle box;

        public ScriptBehaviourManager SystemSelection
        {
            get { return systemSelection; }
            set
            {
                systemSelection = value;
                CreateComponentGroupListView();
            }
        }

        private ScriptBehaviourManager systemSelection;

        public ComponentGroup ComponentGroupSelection
        {
            get { return componentGroupSelection; }
            set
            {
                componentGroupSelection = value;
                entityListView.SelectedComponentGroup = value;
            }
        }

        private ComponentGroup componentGroupSelection;
        
        public Entity EntitySelection
        {
            get { return selectionProxy.Entity; }
            set
            {
                if (value != Entity.Null)
                {
                    selectionProxy.SetEntity(WorldSelection.GetExistingManager<EntityManager>(), value);
                    Selection.activeObject = selectionProxy;
                }
                else if (Selection.activeObject == selectionProxy)
                {
                    Selection.activeObject = null;
                }
            }
        }

        private EntitySelectionProxy selectionProxy;
        
        [SerializeField] private List<TreeViewState> componentGroupListStates = new List<TreeViewState>();
        [SerializeField] private List<string> componentGroupListStateNames = new List<string>();
        private ComponentGroupListView componentGroupListView;
        
        [SerializeField] private List<TreeViewState> systemListStates = new List<TreeViewState>();
        [SerializeField] private List<string> systemListStateNames = new List<string>();
        private SystemListView systemListView;

        [SerializeField] private TreeViewState entityListState = new TreeViewState();
        private EntityListView entityListView;
        
        private string[] worldNames => (from x in World.AllWorlds select x.Name).ToArray();

        private void SelectWorldByName(string name)
        {
            foreach (var world in World.AllWorlds)
            {
                if (world.Name == name)
                {
                    WorldSelection = world;
                    return;
                }
            }

            WorldSelection = null;
        }
        
        public World WorldSelection
        {
            get { return worldSelection; }
            set
            {
                if (worldSelection != value)
                {
                    worldSelection = value;
                    if (worldSelection != null)
                        lastSelectedWorldName = worldSelection.Name;
                    CreateSystemListView();
                }
            }
        }

        private void CreateEntityListView()
        {
            entityListView = new EntityListView(entityListState, ComponentGroupSelection, this);
        }

        private void CreateSystemListView()
        {
            systemListView = SystemListView.CreateList(WorldSelection, systemListStates, systemListStateNames, this);
        }

        private void CreateComponentGroupListView()
        {
            componentGroupListView = ComponentGroupListView.CreateList(SystemSelection as ComponentSystemBase, componentGroupListStates, componentGroupListStateNames, this);
        }

        private World worldSelection;
        [SerializeField] private string lastSelectedWorldName;

        private int selectedWorldIndex
        {
            get { return World.AllWorlds.IndexOf(WorldSelection); }
            set
            {
                if (value >= 0 && value < World.AllWorlds.Count)
                    WorldSelection = World.AllWorlds[value];
            }
        }

        private bool worldsExist;

        private readonly string[] noWorldsName = new[] {"No worlds"};
        private bool worldsAppeared;

        void OnEnable()
        {
            selectionProxy = ScriptableObject.CreateInstance<EntitySelectionProxy>();
            selectionProxy.hideFlags = HideFlags.HideAndDontSave;
            CreateEntityListView();
            CreateSystemListView();
            CreateComponentGroupListView();
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
        }

        private void OnDisable()
        {
            if (selectionProxy)
                DestroyImmediate(selectionProxy);
            
            EditorApplication.playModeStateChanged -= OnPlayModeStateChange;
        }

        void OnPlayModeStateChange(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode && Selection.activeObject == selectionProxy)
                Selection.activeObject = null;
        }
        
        private float lastUpdate;
        
        void Update() 
        { 
            if (EditorApplication.isPlaying && Time.time > lastUpdate + 0.5f) 
            { 
                Repaint(); 
            } 
        } 

        void WorldPopup()
        {
            if (!worldsExist)
            {
                var guiEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.Popup(0, noWorldsName);
                GUI.enabled = guiEnabled;
            }
            else
            {
                if (worldsAppeared && WorldSelection == null)
                {
                    SelectWorldByName(lastSelectedWorldName);
                    if (WorldSelection == null)
                    {
                        WorldSelection = World.AllWorlds[0];
                    }
                }
                selectedWorldIndex = EditorGUILayout.Popup(selectedWorldIndex, worldNames);
            }
        }

        void SystemList()
        {
            var rect = GUIHelpers.GetExpandingRect();
            if (worldsExist)
            {
                if (worldsAppeared)
                    systemListView.multiColumnHeader.ResizeToFit();
                systemListView.OnGUI(rect);
            }
            else
            {
                GUIHelpers.ShowCenteredNotification(rect, "No systems (Try pushing Play)");
            }
        }

        void SystemHeader()
        {

            GUILayout.BeginHorizontal();
            GUILayout.Label("Systems", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            AlignHeader(WorldPopup);
            GUILayout.EndHorizontal();
        }

        void EntityHeader()
        {
            GUILayout.BeginHorizontal();
            if (SystemSelection == null)
            {
                GUILayout.Label("All Entities", EditorStyles.boldLabel);
            }
            else
            {
                var type = SystemSelection.GetType();
                AlignHeader(() => GUILayout.Label(type.Namespace, EditorStyles.label));
                GUILayout.Label(type.Name, EditorStyles.boldLabel);
                if (SystemSelection is ComponentSystemBase)
                {
                    GUILayout.FlexibleSpace();

                    var system = (ComponentSystemBase) SystemSelection;
                    var running = system.Enabled && system.ShouldRunSystem();
                    AlignHeader(() => GUILayout.Label($"running: {running}"));
                }
            }
            GUILayout.EndHorizontal();
        }

        void ComponentGroupList()
        {
            if (SystemSelection is ComponentSystemBase)
            {
                GUILayout.BeginVertical(Box, GUILayout.Height(componentGroupListView.Height + 4f));
                componentGroupListView.OnGUI(GUIHelpers.GetExpandingRect());
                GUILayout.EndVertical();
            }
        }

        void EntityList()
        {
            var showingAllEntities = !(SystemSelection is ComponentSystemBase);
            var componentGroupHasEntities = ComponentGroupSelection != null && !ComponentGroupSelection.IsEmptyIgnoreFilter;
            var somethingToShow = showingAllEntities || componentGroupHasEntities;
            if (!somethingToShow)
                return;
            if (repainted && EditorApplication.isPlaying && !EditorApplication.isPaused)
                entityListView.RefreshData();
            entityListView.OnGUI(GUIHelpers.GetExpandingRect());
        }

        void AlignHeader(System.Action header)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(6f);
            header();
            GUILayout.EndVertical();
        }

        private bool repainted = false;

        void OnGUI()
        {
            if (Selection.activeObject == selectionProxy)
            {
                if (!WorldSelection?.GetExistingManager<EntityManager>()?.Exists(selectionProxy.Entity) ?? true)
                {
                    Selection.activeObject = null;
                    entityListView.SelectNothing();
                }
            }

            var worldsExisted = worldsExist;
            worldsExist = World.AllWorlds.Count > 0;
            worldsAppeared = !worldsExisted && worldsExist;
            
            GUILayout.BeginHorizontal();
            
            GUILayout.BeginVertical(GUILayout.Width(kSystemListWidth)); // begin System List
            SystemHeader();
            
            GUILayout.BeginVertical();
            SystemList();
            GUILayout.EndVertical();
            
            GUILayout.EndVertical(); // end System List
            
            GUILayout.BeginVertical(GUILayout.Width(position.width - kSystemListWidth)); // begin Entity List

            if (EditorApplication.isPlaying)
            {
                EntityHeader();
                ComponentGroupList();
                EntityList();
            }
            
            GUILayout.EndVertical(); // end Component List
            
            GUILayout.EndHorizontal();

            lastUpdate = EditorApplication.isPlaying ? 0f : Time.time;

            repainted = Event.current.type == EventType.Repaint;
        }
    }
}