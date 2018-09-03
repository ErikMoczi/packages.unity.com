using System.Linq;
using System.Collections.Generic;
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
                componentGroupListView.TouchSelection();
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
                entityListView.TouchSelection();
            }
        }

        private ComponentGroup componentGroupSelection;
        
        public Entity EntitySelection
        {
            get { return selectionProxy.Entity; }
            set
            {
                var entityManager = WorldSelection?.GetExistingManager<EntityManager>();
                if (value != Entity.Null && entityManager != null)
                {
                    selectionProxy.SetEntity(entityManager, value);
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
            get
            {
                if (worldSelection != null && worldSelection.IsCreated)
                    return worldSelection;
                return null;
            }
            set
            {
                if (worldSelection != value)
                {
                    worldSelection = value;
                    if (worldSelection != null)
                        lastSelectedWorldName = worldSelection.Name;
                    
                    CreateSystemListView();
                    systemListView.multiColumnHeader.ResizeToFit();
                    systemListView.TouchSelection();
                }
            }
        }

        private void CreateEntityListView()
        {
            entityListView = new EntityListView(entityListState, ComponentGroupSelection, this);
        }

        private void CreateSystemListView()
        {
            systemListView = SystemListView.CreateList(systemListStates, systemListStateNames, this);
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

        private readonly string[] noWorldsName = new[] {"No worlds"};

        void OnEnable()
        {
            selectionProxy = ScriptableObject.CreateInstance<EntitySelectionProxy>();
            selectionProxy.hideFlags = HideFlags.HideAndDontSave;
            CreateSystemListView();
            CreateComponentGroupListView();
            CreateEntityListView();
            systemListView.TouchSelection();
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
            if (Time.realtimeSinceStartup > lastUpdate + 0.5f) 
            { 
                Repaint(); 
            } 
        } 

        void WorldPopup()
        {
            if (World.AllWorlds.Count == 0)
            {
                var guiEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.Popup(0, noWorldsName);
                GUI.enabled = guiEnabled;
            }
            else
            {
                if (WorldSelection == null || !WorldSelection.IsCreated)
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
            if (World.AllWorlds.Count != 0)
            {
                if (repainted)
                    systemListView.UpdateIfNecessary();
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
                GUILayout.FlexibleSpace();
                var system = SystemSelection as ComponentSystemBase;
                if (system != null)
                {
                    var running = system.Enabled && system.ShouldRunSystem();
                    AlignHeader(() => GUILayout.Label($"running: {(running ? "yes" : "no")}"));
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
            if (repainted)
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

        private void OnSelectionChange()
        {
            if (Selection.activeObject != selectionProxy)
            {
                entityListView.SelectNothing();
            }
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

            GUILayout.BeginHorizontal();
            
            GUILayout.BeginVertical(GUILayout.Width(kSystemListWidth)); // begin System List
            SystemHeader();
            
            GUILayout.BeginVertical();
            SystemList();
            GUILayout.EndVertical();
            
            GUILayout.EndVertical(); // end System List
            
            GUILayout.BeginVertical(GUILayout.Width(position.width - kSystemListWidth)); // begin Entity List

            EntityHeader();
            ComponentGroupList();
            EntityList();
            
            GUILayout.EndVertical(); // end Component List
            
            GUILayout.EndHorizontal();

            lastUpdate = Time.realtimeSinceStartup;

            repainted = Event.current.type == EventType.Repaint;
        }
    }
}