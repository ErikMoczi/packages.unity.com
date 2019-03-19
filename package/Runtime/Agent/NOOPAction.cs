using System;
using Unity.Entities;

namespace Unity.AI.Planner.Agent
{
    class NOOPAction<T> : IOperationalAction<T>
    {
        static NOOPAction<T>  s_Instance;
        public static NOOPAction<T> Instance => s_Instance ?? (s_Instance = new NOOPAction<T>());

        NOOPAction() { }

        public void BeginExecution(Entity stateEntity, ActionContext action, T actor) { }

        public void ContinueExecution(Entity stateEntity, ActionContext action, T actor) { }

        public void EndExecution(Entity stateEntity, ActionContext action, T actor) { }

        public OperationalActionStatus Status(Entity stateEntity, ActionContext action, T actor)
        {
            return OperationalActionStatus.Completed;
        }
    }
}
