using System;
using System.Collections.Generic;
using System.Reflection;
using GraphVisualizer;
using Unity.AI.Planner;
using Unity.AI.Planner.Agent;
using UnityEditor.AI.Planner.Utility;
using UnityEngine;
using UnityEngine.AI.Planner.Agent;

namespace UnityEditor.AI.Planner.Visualizer
{
    class PlanVisualizerWindow : EditorWindow, IHasCustomMenu
    {
        public IPolicyGraphInternal Plan
        {
            set
            {
                m_Plan = value;
                InitializePlanVisualizer(m_Plan, null);
            }
        }

        List<Type> m_AgentTypes;

        IGraphRenderer m_Renderer;
        IGraphLayout m_Layout;

        [NonSerialized]
        IPolicyGraphInternal m_Plan;
        IPlanVisualizer m_Visualizer;
        IVisualizerNode m_RootNodeOverride;

        GraphSettings m_GraphSettings;
        int m_MaxDepth = k_DefaultMaxDepth;
        int m_MaxChildrenNodes = k_DefaultMaxChildrenNodes;

        const float k_DefaultMaximumNormalizedNodeSize = 0.8f;
        const float k_DefaultMaximumNodeSizeInPixels = 100.0f;
        const float k_DefaultAspectRatio = 1.5f;
        const int k_DefaultMaxDepth = 2;
        const int k_DefaultMaxChildrenNodes = 3;

        PlanVisualizerWindow()
        {
            m_GraphSettings.maximumNormalizedNodeSize = k_DefaultMaximumNormalizedNodeSize;
            m_GraphSettings.maximumNodeSizeInPixels = k_DefaultMaximumNodeSizeInPixels;
            m_GraphSettings.aspectRatio = k_DefaultAspectRatio;
            m_GraphSettings.showLegend = false;
            m_GraphSettings.showInspector = false;

            m_AgentTypes = new List<Type>();
            ReflectionUtils.ForEachType(t =>
            {
                var baseType = t.BaseType;
                if (!t.IsAbstract && baseType != null && baseType.IsGenericType
                    && baseType.GetGenericTypeDefinition() == typeof(BaseAgent<>))
                {
                    m_AgentTypes.Add(t);
                }
            });
        }

        [MenuItem("Window/AI/Plan Visualizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PlanVisualizerWindow>("Plan Visualizer");
            if (window.position.x > Screen.width)
            {
                var position = window.position;
                position.x -= Screen.width - position.width;
                window.position = position;
            }
        }

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void OnSelectionChange()
        {
            SelectPlan();
        }

        void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            SelectPlan();
        }

        GameObject FindBaseAgentObject()
        {
            foreach (var agentType in m_AgentTypes)
            {
                var agent = (MonoBehaviour)FindObjectOfType(agentType);
                return agent != null ? agent.gameObject : null;
            }

            return null;
        }


        void SelectPlan()
        {
            var activeGameObject = Selection.activeGameObject;

            if (EditorApplication.isPlaying)
            {
                if (!activeGameObject)
                    activeGameObject = FindBaseAgentObject();

                if (activeGameObject)
                {
                    if (!GetPlan(activeGameObject, out var plan, out var getCurrentAction))
                    {
                        activeGameObject = FindBaseAgentObject();
                        GetPlan(activeGameObject, out plan, out getCurrentAction);
                    }

                    if (plan != null && getCurrentAction != null)
                    {
                        m_Plan = plan;
                        InitializePlanVisualizer(m_Plan, getCurrentAction);
                    }
                }
            }
            else
            {
                m_Visualizer = null;
                m_Plan = null;
            }
        }

        void InitializePlanVisualizer(IPolicyGraph plan, Func<ActionContext> getCurrentAction)
        {
            if (plan is PolicyGraphContainer graphContainer)
                m_Visualizer = new PlanVisualizer(graphContainer.World, graphContainer, getCurrentAction);
        }

        static bool GetPlan(GameObject go, out IPolicyGraphInternal plan, out Func<ActionContext> getCurrentAction)
        {
            plan = null;
            getCurrentAction = null;

            if (go == null)
                return false;

            var components = go.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (!component.enabled)
                    continue;

                var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (IsAssignableToGenericType(field.FieldType, typeof(Controller<>)))
                    {
                        var controller = field.GetValue(component);
                        if (controller != null)
                        {
                            var controllerType = controller.GetType();
                            var planField = controllerType.GetField("m_Plan", BindingFlags.NonPublic | BindingFlags.Instance);
                            plan = (IPolicyGraphInternal)planField.GetValue(controller);

                            var actionField = controllerType.GetField("m_CurrentAction", BindingFlags.NonPublic | BindingFlags.Instance);
                            getCurrentAction = () => (ActionContext)actionField.GetValue(controller);
                            break;
                        }
                    }
                }

                if (plan != null && getCurrentAction != null)
                    break;
            }

            return plan != null && getCurrentAction != null;
        }

        static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            var baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, genericType);
        }

        void ShowMessage(string msg)
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label(msg);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        void Update()
        {
            if (EditorApplication.isPlaying && m_Plan == null)
                SelectPlan();

            // If in Play mode, refresh the plan each update.
            if (EditorApplication.isPlaying)
                Repaint();
        }

        void OnInspectorUpdate()
        {
            // If not in Play mode, refresh the plan less frequently.
            if (!EditorApplication.isPlaying)
                Repaint();
        }

        void OnGUI()
        {
            GraphOnGUI();
            DrawGraph();
        }

        void DrawGraph()
        {
            if (m_Visualizer == null)
                return;

            if (m_Layout == null)
                m_Layout = new ReingoldTilford();

            m_Visualizer.MaxDepth = m_MaxDepth;
            m_Visualizer.MaxChildrenNodes = m_MaxChildrenNodes;
            m_Visualizer.RootNodeOverride = m_RootNodeOverride;
            m_Visualizer.Refresh();
            m_Layout.CalculateLayout((Graph)m_Visualizer);

            if (m_Renderer == null)
            {
                m_Renderer = new PlanGraphRenderer((renderer, vn) =>
                {
                    if (vn != null && vn.ExpansionNode)
                    {
                        if (vn.parent != null)
                        {
                            // We're looking to go into the children expansion of a node, so select the actual node;
                            // The one that was clicked on was placeholder for all of the children
                            m_RootNodeOverride = (IVisualizerNode)vn.parent;
                        }
                        else
                        {
                            // Navigate back up the hierarchy
                            m_RootNodeOverride = (IVisualizerNode)m_RootNodeOverride.parent;

                            // If there isn't another parent, then we're actually back at the root
                            if (m_RootNodeOverride.parent == null)
                                m_RootNodeOverride = null;
                        }

                        renderer.Reset();
                    }

                    m_GraphSettings.showInspector = vn != null;
                });
            }

            var toolbarHeight = EditorStyles.toolbar.fixedHeight;
            var graphRect = new Rect(0, toolbarHeight, position.width, position.height - toolbarHeight);

            m_Renderer.Draw(m_Layout, graphRect, m_GraphSettings);
        }

        void GraphOnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Max Depth:");
            m_MaxDepth = EditorGUILayout.IntSlider(m_MaxDepth, 1, 4);
            EditorGUILayout.Space();
            GUILayout.Label("Show Top Actions:");
            m_MaxChildrenNodes = EditorGUILayout.IntSlider(m_MaxChildrenNodes, 1, 16);
            GUILayout.FlexibleSpace();
            if (m_Plan != null)
            {
                var rootNode = m_Plan.RootNode;
                if (rootNode != null)
                    GUILayout.Label($"Root Iterations: {m_Plan.RootNode.Iterations}");
                EditorGUILayout.Space();
                GUILayout.Label($"Max Plan Depth: {m_Plan.MaxHorizonFromRoot}");
            }

            EditorGUILayout.EndHorizontal();

            if (m_Visualizer == null && m_Plan == null)
            {
                // Early out if there are no graphs.
                if (!EditorApplication.isPlaying)
                {
                    ShowMessage("You must be in play mode to visualize a plan");
                    return;
                }

                ShowMessage("Select an agent that has an AI controller");
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Legend"), m_GraphSettings.showLegend, ToggleLegend);
            menu.AddItem(new GUIContent("Inspector"), m_GraphSettings.showInspector, ToggleInspector);
        }

        void ToggleLegend()
        {
            m_GraphSettings.showLegend = !m_GraphSettings.showLegend;
        }

        void ToggleInspector()
        {
            m_GraphSettings.showInspector = !m_GraphSettings.showInspector;
        }
    }
}
