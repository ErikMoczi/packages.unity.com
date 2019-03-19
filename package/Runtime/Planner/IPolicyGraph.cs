using Unity.Entities;

namespace Unity.AI.Planner
{
    /// <summary>
    /// Interface for the policy graph
    /// </summary>
    public interface IPolicyGraph
    {
        /// <summary>
        /// Action context for the optimal action at the root state of the graph
        /// </summary>
        ActionContext NextAction { get; }

        /// <summary>
        /// Updates the policy root to the given state
        /// </summary>
        /// <param name="stateEntity">Entity corresponding to the state for the new policy root</param>
        void UpdatePlan(Entity stateEntity);

        /// <summary>
        /// Resets the policy graph with a new state for the root
        /// </summary>
        /// <param name="stateEntity">Entity corresponding to the state for the new policy root</param>
        void Reset(Entity stateEntity);
    }

    interface IPolicyGraphInternal : IPolicyGraph
    {
        IPolicyGraphNode RootNode { get; }

        int MaxHorizonFromRoot { get; }
    }
}
