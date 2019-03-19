using System;
using System.Collections.Generic;
using System.Text;
using GraphVisualizer;
using Unity.AI.Planner;
using Unity.AI.Planner.DomainLanguage.TraitBased;
using Unity.Entities;
using UnityEditor.AI.Planner.Utility;
using UnityEngine;

namespace UnityEditor.AI.Planner.Visualizer
{
    abstract class BaseVisualizerNode : Node, IVisualizerNode
    {
        public bool ExpansionNode { get; }
        public string Label { get; }

        protected World m_World;
        protected EntityManager m_EntityManager;

        protected BaseVisualizerNode(World world, EntityManager entityManager, string label, object content, bool expansion = false,  float weight = 1, bool active = false)
            : base(content, weight, active)
        {
            Label = label;
            ExpansionNode = expansion;
            m_World = world;
            m_EntityManager = entityManager;
        }

        public abstract IEnumerable<Node> GetNodeChildren(int maxDepth, int maxChildrenNodes);

        protected abstract string GetExpansionString();

        public override string GetContentTypeName()
        {
            if (ExpansionNode)
                return weight == 0f ? "\u2026" : GetExpansionString();

            return Label;
        }

        protected static string InfoString(string key, double value) => string.Format(
                Math.Abs(value) < 100000.0 ? "<b>{0}:</b> {1:0.000}" : "<b>{0}:</b> {1:E4}", key, value);

        protected static string InfoString(string key, int value) => $"<b>{key}:</b> {value:D}";

        protected static string InfoString(string key, object value) => "<b>" + key + ":</b> " + (value ?? string.Empty);

    }

    class VisualizerActionNode : BaseVisualizerNode
    {
        public ActionNode ActionNode { get; }

        public VisualizerActionNode(World world, EntityManager entityManager, object content, ActionNode actionNode,
            float weight = 1, bool active = false, bool expansion = false)
            : base(world, entityManager, GetActionName(world, actionNode.ActionSystemGuid), content, expansion, weight, active)
        {
            ActionNode = actionNode;
        }

        public override Type GetContentType()
        {
            if (m_World != null && m_World.IsCreated)
            {
                var actionSystemGuid = ActionNode.ActionSystemGuid;
                foreach (var manager in m_World.BehaviourManagers)
                {
                    if (manager is IActionSystem actionSystem)
                    {
                        if (actionSystem.ActionSystemGuid == actionSystemGuid)
                            return actionSystem.GetType();
                    }
                }
            }

            return ActionNode.Equals(default) ? content?.GetType() : ActionNode.GetType();
        }

        protected override string GetExpansionString()
        {
            var graphNode = (Entity)content;
            var actionResultBuffer = m_EntityManager.GetBuffer<ActionResult>(graphNode);
            return $"{actionResultBuffer.Length} State(s)";
        }

        public override string ToString()
        {
            var graphNodeEntity = (Entity)content;
            if (ExpansionNode || graphNodeEntity == default || !m_EntityManager.IsCreated)
                return base.ToString();

            var actionNode = m_EntityManager.GetComponentData<ActionNode>(graphNodeEntity);
            if (actionNode.ActionSystemGuid == default)
                return base.ToString();

            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine(Label);

            sb.AppendLine();
            sb.AppendLine(InfoString("Policy Value", actionNode.ActionValue));
            sb.AppendLine(InfoString("Iterations", actionNode.VisitCount));
            sb.AppendLine(InfoString("Complete", actionNode.Complete));

            // todo arguments?

            return sb.ToString();
        }

        public static string GetActionName(World world, Guid actionSystemGuid)
        {
            if (!world.IsCreated)
                return actionSystemGuid.ToString();

            foreach (var manager in world.BehaviourManagers)
            {
                if (manager is IActionSystem actionSystem)
                {
                    if (actionSystem.ActionSystemGuid == actionSystemGuid)
                        return actionSystem.Name;
                }
            }

            return null;
        }

        struct ActionResultNode : IComparable<ActionResultNode>
        {
            public ActionNode ActionNode;
            public PolicyGraphNode Child;
            public Entity ChildEntity;
            public float Probability;

            public int CompareTo(ActionResultNode other) => other.Probability.CompareTo(Probability);
        }

        public override IEnumerable<Node> GetNodeChildren(int maxDepth, int maxChildrenNodes)
        {
            var graphNodeEntity = (Entity)content;
            if (graphNodeEntity == default)
                yield break;

            var actionResultBuffer = m_EntityManager.GetBuffer<ActionResult>(graphNodeEntity);

            // Handle current node
            if (maxDepth > 0 && depth > maxDepth)
            {
                if (depth == maxDepth + 1 && actionResultBuffer.Length > 0)
                {
                    var expansionNode = new VisualizerActionNode(m_World, m_EntityManager, graphNodeEntity, default,
                        Mathf.Min(weight, float.Epsilon), expansion: true);
                    yield return expansionNode;
                }

                yield break;
            }

            // Grab all child nodes and sort them
            var minProbability = float.MaxValue;
            var maxProbability = float.MinValue;
            var sortedChildrenNodes = new List<ActionResultNode>();
            var actionNode = m_EntityManager.GetComponentData<ActionNode>(graphNodeEntity);
            for (var i = 0; i < actionResultBuffer.Length; i++)
            {
                var actionResult = actionResultBuffer[i];
                var policyNodeEntity = actionResult.PolicyGraphNodeEntity;
                var resultProbability = actionResult.Probability;
                minProbability = Mathf.Min(minProbability, resultProbability);
                maxProbability = Mathf.Max(maxProbability, resultProbability);

                sortedChildrenNodes.Add(new ActionResultNode()
                {
                    ActionNode = actionNode,
                    Child = m_EntityManager.GetComponentData<PolicyGraphNode>(policyNodeEntity),
                    ChildEntity = policyNodeEntity,
                    Probability = resultProbability,
                });
            }

            sortedChildrenNodes.Sort();

            // Yield children
            var c = 0;
            foreach (var childPolicyNode in sortedChildrenNodes)
            {
                if (c >= maxChildrenNodes)
                    yield break;

                var probability = childPolicyNode.Probability;
                var nodeWeight = Math.Abs(probability - 1.0f) < 10e-6 ?
                    float.MaxValue :
                    Mathf.InverseLerp(minProbability, maxProbability, childPolicyNode.Probability);
                yield return new VisualizerPolicyNode(m_World, m_EntityManager, childPolicyNode.ChildEntity, childPolicyNode.Child, nodeWeight);

                c++;
            }
        }
    }

    class VisualizerPolicyNode : BaseVisualizerNode
    {
        public PolicyGraphNode PolicyGraphNode { get; }

        public VisualizerPolicyNode(World world, EntityManager entityManager, object content, PolicyGraphNode policyGraphNode,
            float weight = 1, bool active = false, bool expansion = false)
            : base(world, entityManager, GetPolicyNodeLabel(policyGraphNode), content, expansion, weight, active)
        {
            PolicyGraphNode = policyGraphNode;
        }

        public override Type GetContentType()
        {
            return PolicyGraphNode.Equals(default) ? content?.GetType() : PolicyGraphNode.GetType();
        }

        protected override string GetExpansionString()
        {
            var graphNode = (Entity)content;
            var actionReferenceBuffer = m_EntityManager.GetBuffer<ActionNodeReference>(graphNode);
            return $"{actionReferenceBuffer.Length} Action(s)";
        }

        public override string ToString()
        {
            var graphNodeEntity = (Entity)content;
            if (ExpansionNode || graphNodeEntity == default || !m_EntityManager.IsCreated)
                return base.ToString();

            var pgn = m_EntityManager.GetComponentData<PolicyGraphNode>(graphNodeEntity);
            if (pgn.StateEntity == default)
                return base.ToString();

            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine(Label);

            sb.AppendLine();
            sb.AppendLine(InfoString("Entity", graphNodeEntity.Index));
            sb.AppendLine(InfoString("State Entity", pgn.StateEntity.Index));

            sb.AppendLine();
            if (pgn.OptimalActionEntity != default)
            {
                var optimalActionNode = m_EntityManager.GetComponentData<ActionNode>(pgn.OptimalActionEntity);
                sb.AppendLine(InfoString("Optimal Action", VisualizerActionNode.GetActionName(m_World, optimalActionNode.ActionSystemGuid)));
            }

            var actionReferenceBuffer = m_EntityManager.GetBuffer<ActionNodeReference>(graphNodeEntity);
            for (var i = 0; i < actionReferenceBuffer.Length; i++)
            {
                var actionNodeEntity = actionReferenceBuffer[i].ActionNodeEntity;
                var actionNode = m_EntityManager.GetComponentData<ActionNode>(actionNodeEntity);

                var actionResultsBuffer = m_EntityManager.GetBuffer<ActionResult>(actionNodeEntity);
                var transitionUtilityValue = string.Empty;
                if (actionResultsBuffer.Length > 0)
                    transitionUtilityValue = actionResultsBuffer[0].TransitionUtilityValue.ToString();

                sb.AppendLine($"    {VisualizerActionNode.GetActionName(m_World, actionNode.ActionSystemGuid)}: {actionNode.ActionValue} ({transitionUtilityValue})");
            }

            sb.AppendLine();
            sb.AppendLine(InfoString("Policy Value", pgn.PolicyValue));
            sb.AppendLine(InfoString("Iterations", pgn.Iterations));

            sb.AppendLine();
            sb.AppendLine(InfoString("Complete", pgn.Complete));

            sb.AppendLine();
            var stateString = TraitBasedDomain.GetStateString(m_EntityManager, pgn.StateEntity);
            if (GUILayout.Button("Copy State", EditorStyles.miniButton))
                stateString.CopyToClipboard();

            sb.AppendLine(InfoString("State", null));
            sb.AppendLine(stateString);

            return sb.ToString();
        }

        static string GetPolicyNodeLabel(PolicyGraphNode policyGraphNode)
        {
            return policyGraphNode.Equals(default) ? null : $"State: {policyGraphNode.StateEntity.Index}";
        }

        struct PolicyChildNode : IComparable<PolicyChildNode>
        {
            public PolicyGraphNode PolicyGraphNode;
            public ActionNode Child;
            public Entity ChildEntity;

            public int CompareTo(PolicyChildNode other) => other.Child.ActionValue.CompareTo(Child.ActionValue);
        }

        public override IEnumerable<Node> GetNodeChildren(int maxDepth, int maxChildrenNodes)
        {
            var graphNodeEntity = (Entity)content;
            var actionReferenceBuffer = m_EntityManager.GetBuffer<ActionNodeReference>(graphNodeEntity);

            // Handle current node
            if (maxDepth > 0 && depth > maxDepth)
            {
                if (depth == maxDepth + 1 && actionReferenceBuffer.Length > 0)
                {
                    var expansionNode = new VisualizerPolicyNode(m_World, m_EntityManager, graphNodeEntity, default,
                        Mathf.Min(weight, float.Epsilon), expansion: true);
                    yield return expansionNode;
                }

                yield break;
            }

            // Grab all child nodes and sort them
            var minActionValue = float.MaxValue;
            var maxActionValue = float.MinValue;
            var sortedChildrenNodes = new List<PolicyChildNode>();
            var policyGraphNode = m_EntityManager.GetComponentData<PolicyGraphNode>(graphNodeEntity);
            for (var i = 0; i < actionReferenceBuffer.Length; i++)
            {
                var actionNodeEntity = actionReferenceBuffer[i].ActionNodeEntity;
                var actionNode = m_EntityManager.GetComponentData<ActionNode>(actionNodeEntity);
                var actionValue = actionNode.ActionValue;
                minActionValue = Mathf.Min(minActionValue, actionValue);
                maxActionValue = Mathf.Max(maxActionValue, actionValue);

                sortedChildrenNodes.Add(new PolicyChildNode()
                {
                    PolicyGraphNode = policyGraphNode,
                    Child = actionNode,
                    ChildEntity = actionNodeEntity
                });
            }

            sortedChildrenNodes.Sort();

            // Yield children
            var pgn = m_EntityManager.GetComponentData<PolicyGraphNode>(graphNodeEntity);
            var optimalActionNode = pgn.OptimalActionEntity != default ?
                m_EntityManager.GetComponentData<ActionNode>(pgn.OptimalActionEntity) : default;
            var c = 0;
            foreach (var childPolicyNode in sortedChildrenNodes)
            {
                if (c >= maxChildrenNodes)
                    yield break;

                var action = childPolicyNode.Child;
                var nodeWeight = !optimalActionNode.Equals(default) && action.Equals(optimalActionNode) ? float.MaxValue
                    : Mathf.InverseLerp(minActionValue, maxActionValue, childPolicyNode.Child.ActionValue);
                yield return new VisualizerActionNode(m_World, m_EntityManager, childPolicyNode.ChildEntity, action, nodeWeight);

                c++;
            }
        }
    }


    class PlanVisualizer : Graph, IPlanVisualizer
    {
        public int MaxDepth { get; set; }
        public int MaxChildrenNodes { get; set; }
        public IVisualizerNode RootNodeOverride { get; set; }

        World m_World;
        EntityManager m_EntityManager;
        PlannerSystem m_Planner;
        Func<ActionContext> m_GetCurrentAction;
        PolicyGraphContainer m_PolicyGraph;

        public PlanVisualizer(World world, PolicyGraphContainer graphContainer, Func<ActionContext> getCurrentAction)
        {
            m_World = world;
            m_EntityManager = m_World.GetExistingManager<EntityManager>();
            m_Planner = m_World.GetExistingManager<PlannerSystem>();
            m_PolicyGraph = graphContainer;
            m_GetCurrentAction = getCurrentAction;
        }

        protected override IEnumerable<Node> GetChildren(Node node)
        {
            if (!m_EntityManager.IsCreated)
                yield break;

            foreach (var childNode in ((BaseVisualizerNode) node).GetNodeChildren(MaxDepth, MaxChildrenNodes))
                yield return childNode;
        }

        protected override void Populate()
        {
            if (!m_EntityManager.IsCreated)
                return;

            if (RootNodeOverride == null)
                PopulateWithRoot();
            else
                PopulateWithRootOverride();
        }

        void PopulateWithRoot()
        {
            var rootNodeEntity = m_PolicyGraph.PolicyGraphRootEntity;
            var rootPolicyGraphNode = m_EntityManager.GetComponentData<PolicyGraphNode>(rootNodeEntity);
            if (rootNodeEntity.Equals(default))
                return;

            // If action is in progress or optimal action is assigned, use action node as root.
            var action = m_GetCurrentAction != null ? m_GetCurrentAction() : default;
            var actionEntity = action == default ? rootPolicyGraphNode.OptimalActionEntity : action.ActionEntity;
            if (actionEntity != default)
            {
                var actionNode = m_EntityManager.GetComponentData<ActionNode>(actionEntity);
                var visualizerActionNode = CreateActionNode(actionEntity, actionNode, 1f, true);
                AddNodeHierarchy(visualizerActionNode);
                return;
            }

            // If the root has not yet been expanded, root the graph at the policy graph node.
            var rootPolicyNode = CreatePolicyNode(rootNodeEntity, rootPolicyGraphNode, 1f, true);
            AddNodeHierarchy(rootPolicyNode);
        }

        void PopulateWithRootOverride()
        {
            var parentVisualizerNode = RootNodeOverride.parent;
            Entity parentNodeEntity = default;

            if (RootNodeOverride is VisualizerActionNode visActionNode)
            {
                var visualizerNode = CreateActionNode((Entity) RootNodeOverride.content, visActionNode.ActionNode, 1f, true);
                AddNodeHierarchy(visualizerNode);

                PolicyGraphNode parentPolicyGraphNode = default;
                if (parentVisualizerNode != null)
                {
                    parentNodeEntity = (Entity)parentVisualizerNode.content;
                    parentPolicyGraphNode = ((VisualizerPolicyNode)parentVisualizerNode).PolicyGraphNode;
                }

                var backNode = CreatePolicyNode(parentNodeEntity, parentPolicyGraphNode, 0f, false, true);
                backNode.AddChild(visualizerNode);
                AddNode(backNode);
            }
            else if (RootNodeOverride is VisualizerPolicyNode visPolicyNode)
            {
                var visualizerNode = CreatePolicyNode((Entity) RootNodeOverride.content, visPolicyNode.PolicyGraphNode,
                    1f, true);
                AddNodeHierarchy(visualizerNode);

                ActionNode parentAction = default;
                if (parentVisualizerNode != null)
                {
                    parentNodeEntity = (Entity)parentVisualizerNode.content;
                    parentAction = ((VisualizerActionNode)parentVisualizerNode).ActionNode;
                }

                var backNode = CreateActionNode(parentNodeEntity, parentAction, 0f, false, true);
                backNode.AddChild(visualizerNode);
                AddNode(backNode);
            }

        }

        BaseVisualizerNode CreateActionNode(Entity nodeEntity, ActionNode actionNode,
            float weight, bool active = false, bool expansion = false)
        {
            return new VisualizerActionNode(m_World, m_EntityManager, nodeEntity, actionNode, weight, active, expansion);
        }

        BaseVisualizerNode CreatePolicyNode(Entity nodeEntity, PolicyGraphNode policyNode,
             float weight, bool active = false, bool expansion = false)
        {
            return new VisualizerPolicyNode(m_World, m_EntityManager, nodeEntity, policyNode, weight, active, expansion);
        }
    }
}
