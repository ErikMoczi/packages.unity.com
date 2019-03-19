using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Unity.AI.Planner.Agent
{
    /// <summary>
    /// The control mechanism responsible for executing actions from the plan, monitoring progress of the operational
    /// actions, and updating the progress of the plan
    /// </summary>
    /// <typeparam name="TAgent">The type of agent for which the controller acts</typeparam>
    public class Controller<TAgent>
    {
        /// <summary>
        /// Entity corresponding to the current state
        /// </summary>
        public Entity CurrentStateEntity { get; protected set; }

        /// <summary>
        /// The operational action currently executing
        /// </summary>
        public IOperationalAction<TAgent> CurrentOperationalAction { get; private set; }

        IPolicyGraph m_Plan;
        TAgent m_Agent;
        Dictionary<string, IOperationalAction<TAgent>> m_ActionMapping;
        Func<IPolicyGraph, bool> m_CheckPlan;
        ActionContext m_CurrentAction;

        /// <summary>
        /// Initializes the controller
        /// </summary>
        /// <param name="plan">Plan for the controller to update and execute</param>
        /// <param name="stateEntity">Entity corresponding to the current state</param>
        /// <param name="agent">Agent controlled</param>
        /// <param name="checkPlan">A function to check if the plan is ready before the each action is taken</param>
        public Controller(IPolicyGraph plan, Entity stateEntity, TAgent agent,
            Dictionary<string, IOperationalAction<TAgent>> actionMapping, Func<IPolicyGraph, bool> checkPlan = null)
        {
            m_Plan = plan;
            CurrentStateEntity = stateEntity;
            m_Agent = agent;
            m_ActionMapping = actionMapping;
            m_CheckPlan = checkPlan;

            CurrentOperationalAction = NOOPAction<TAgent>.Instance;
        }

        /// <summary>
        /// Updates the controller, advances the plan or continues execution of an operational action
        /// </summary>
        public void Update()
        {
            if (m_CurrentAction.Equals(default))
            {
                if (ReadyToAct())
                    AdvancePlan();

                return;
            }

            switch (CurrentOperationalAction.Status(CurrentStateEntity, m_CurrentAction, m_Agent))
            {
                case OperationalActionStatus.InProgress:
                    CurrentOperationalAction.ContinueExecution(CurrentStateEntity, m_CurrentAction, m_Agent);
                    return;

                case OperationalActionStatus.NoLongerValid:
#if PLANNER_DEBUG
                    UnityEngine.Debug.Log($"Replan: Terminating {m_CurrentAction}");
#endif
                    RecomputePlan();
                    return;

                case OperationalActionStatus.Completed:
                    CompleteAction();
                    return;
            }
        }

        bool ReadyToAct()
        {
            // Can be called before planner has had a chance to set up an initial policy
            if (m_Plan == null || m_Plan.NextAction.Equals(default))
            {
                // Give a chance for the planner to update if the current state changed
                RecomputePlan();

                return false;
            }

            return m_CheckPlan == null || m_CheckPlan(m_Plan);
        }

        void AdvancePlan()
        {
            // Grab the next action from the policy.
            m_CurrentAction = m_Plan.NextAction;
            CurrentOperationalAction = m_ActionMapping[m_CurrentAction.Name];

            CurrentOperationalAction.BeginExecution(CurrentStateEntity, m_CurrentAction, m_Agent);
        }

        void RecomputePlan()
        {
            // Reset the planner state and compute a new plan.
            m_CurrentAction = default;
            CurrentOperationalAction = NOOPAction<TAgent>.Instance;
            m_Plan.Reset(CurrentStateEntity);
        }

        /// <summary>
        /// Forces the end of the current operational action. Updates the plan to the controller's current state.
        /// </summary>
        public void CompleteAction()
        {
            // End current domain action
            CurrentOperationalAction.EndExecution(CurrentStateEntity, m_CurrentAction, m_Agent);
            m_CurrentAction = default;
            CurrentOperationalAction = NOOPAction<TAgent>.Instance;

            // Update plan
            m_Plan.UpdatePlan(CurrentStateEntity);
        }
    }
}
