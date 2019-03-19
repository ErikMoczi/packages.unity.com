using System;
using Unity.Entities;

namespace Unity.AI.Planner.Agent
{
    /// <summary>
    /// The status of an operational action, used to monitor the progress of such actions
    /// </summary>
    public enum OperationalActionStatus
    {
        InProgress,
        NoLongerValid,
        Completed
    }

    /// <summary>
    /// An interface used to mark implementations of operational actions. Base interface for <see cref="Unity.AI.Planner.Agent.IOperationalAction{T}"/>.
    /// </summary>
    public interface IOperationalAction { }

    /// <summary>
    /// The required interface for operational actions, as used in executing a plan
    /// </summary>
    /// <typeparam name="TAgent">Agent type</typeparam>
    public interface IOperationalAction<TAgent>: IOperationalAction
    {
        /// <summary>
        /// Begins the execution of the operational action
        /// </summary>
        /// <param name="stateEntity">Entity corresponding to the current state</param>
        /// <param name="action">Action context for the planner representation of the operational action</param>
        /// <param name="agent">The agent enacting the operational action</param>
        void BeginExecution(Entity stateEntity, ActionContext action, TAgent agent);

        /// <summary>
        /// Continues the execution of the operational action
        /// </summary>
        /// <param name="stateEntity">Entity corresponding to the current state</param>
        /// <param name="action">Action context for the planner representation of the operational action</param>
        /// <param name="agent">The agent enacting the operational action</param>
        void ContinueExecution(Entity stateEntity, ActionContext action, TAgent agent);

        /// <summary>
        /// Ends the execution of the operational action
        /// </summary>
        /// <param name="stateEntity">Entity corresponding to the current state</param>
        /// <param name="action">Action context for the planner representation of the operational action</param>
        /// <param name="agent">The agent enacting the operational action</param>
        void EndExecution(Entity stateEntity, ActionContext action, TAgent agent);

        /// <summary>
        /// Reports the execution status of the operational action
        /// </summary>
        /// <param name="stateEntity">Entity corresponding to the current state</param>
        /// <param name="action">Action context for the planner representation of the operational action</param>
        /// <param name="agent">The agent enacting the operational action</param>
        /// <returns>Returns the status of the operational action</returns>
        OperationalActionStatus Status(Entity stateEntity, ActionContext action, TAgent agent);
    }
}
