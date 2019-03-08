using System.Collections.Generic;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    [DisableAutoCreation]
    [Preserve]
    [UnityEngine.ExecuteAlways]
    public class BeginInitializationEntityCommandBufferSystem : EntityCommandBufferSystem {}

    [DisableAutoCreation]
    [Preserve]
    [UnityEngine.ExecuteAlways]
    public class EndInitializationEntityCommandBufferSystem : EntityCommandBufferSystem {}

    public class InitializationSystemGroup : ComponentSystemGroup
    {
        private BeginInitializationEntityCommandBufferSystem m_BeginEntityCommandBufferSystem;
        private EndInitializationEntityCommandBufferSystem m_EndEntityCommandBufferSystem;

        protected override void OnCreateManager()
        {
            m_BeginEntityCommandBufferSystem = World.GetOrCreateManager<BeginInitializationEntityCommandBufferSystem>();
            m_EndEntityCommandBufferSystem = World.GetOrCreateManager<EndInitializationEntityCommandBufferSystem>();
            m_systemsToUpdate.Add(m_BeginEntityCommandBufferSystem);
            m_systemsToUpdate.Add(m_EndEntityCommandBufferSystem);
        }

        public override void SortSystemUpdateList()
        {
            // Extract list of systems to sort (excluding built-in systems that are inserted at fixed points)
            var toSort = new List<ComponentSystemBase>(m_systemsToUpdate.Count - 2);
            foreach (var s in m_systemsToUpdate)
            {
                if (s is BeginInitializationEntityCommandBufferSystem ||
                    s is EndInitializationEntityCommandBufferSystem)
                {
                    continue;
                }
                toSort.Add(s);
            }
            m_systemsToUpdate = toSort;
            base.SortSystemUpdateList();
            // Re-insert built-in systems to construct the final list
            var finalSystemList = new List<ComponentSystemBase>(1 + m_systemsToUpdate.Count + 1);
            finalSystemList.Add(m_BeginEntityCommandBufferSystem);
            foreach (var s in m_systemsToUpdate)
                finalSystemList.Add(s);
            finalSystemList.Add(m_EndEntityCommandBufferSystem);
            m_systemsToUpdate = finalSystemList;
        }
    }

    [DisableAutoCreation]
    [Preserve]
    [UnityEngine.ExecuteAlways]
    public class BeginSimulationEntityCommandBufferSystem : EntityCommandBufferSystem {}

    [DisableAutoCreation]
    [Preserve]
    [UnityEngine.ExecuteAlways]
    public class EndSimulationEntityCommandBufferSystem : EntityCommandBufferSystem {}

    [DisableAutoCreation]
    public class LateSimulationSystemGroup : ComponentSystemGroup {}

    public class SimulationSystemGroup : ComponentSystemGroup
    {
        private BeginSimulationEntityCommandBufferSystem m_BeginEntityCommandBufferSystem;
        private LateSimulationSystemGroup m_lateSimulationGroup;
        private EndSimulationEntityCommandBufferSystem m_EndEntityCommandBufferSystem;
        protected override void OnCreateManager()
        {
            m_BeginEntityCommandBufferSystem = World.GetOrCreateManager<BeginSimulationEntityCommandBufferSystem>();
            m_lateSimulationGroup = World.GetOrCreateManager<LateSimulationSystemGroup>();
            m_EndEntityCommandBufferSystem = World.GetOrCreateManager<EndSimulationEntityCommandBufferSystem>();
            m_systemsToUpdate.Add(m_BeginEntityCommandBufferSystem);
            m_systemsToUpdate.Add(m_lateSimulationGroup);
            m_systemsToUpdate.Add(m_EndEntityCommandBufferSystem);
        }

        public override void SortSystemUpdateList()
        {
            // Extract list of systems to sort (excluding built-in systems that are inserted at fixed points)
            var toSort = new List<ComponentSystemBase>(m_systemsToUpdate.Count - 3);
            foreach (var s in m_systemsToUpdate)
            {
                if (s is BeginSimulationEntityCommandBufferSystem ||
                    s is LateSimulationSystemGroup ||
                    s is EndSimulationEntityCommandBufferSystem)
                {
                    continue;
                }
                toSort.Add(s);
            }
            m_systemsToUpdate = toSort;
            base.SortSystemUpdateList();
            m_lateSimulationGroup.SortSystemUpdateList(); // not handled by base-class sort call
            // Re-insert built-in systems to construct the final list
            var finalSystemList = new List<ComponentSystemBase>(1 + m_systemsToUpdate.Count + 2);
            finalSystemList.Add(m_BeginEntityCommandBufferSystem);
            foreach (var s in m_systemsToUpdate)
                finalSystemList.Add(s);
            finalSystemList.Add(m_lateSimulationGroup);
            finalSystemList.Add(m_EndEntityCommandBufferSystem);
            m_systemsToUpdate = finalSystemList;
        }
    }

    [DisableAutoCreation]
    [Preserve]
    [UnityEngine.ExecuteAlways]
    public class BeginPresentationEntityCommandBufferSystem : EntityCommandBufferSystem {}

    [DisableAutoCreation]
    [Preserve]
    [UnityEngine.ExecuteAlways]
    public class EndPresentationEntityCommandBufferSystem : EntityCommandBufferSystem {}

    public class PresentationSystemGroup : ComponentSystemGroup
    {
        private BeginPresentationEntityCommandBufferSystem m_BeginEntityCommandBufferSystem;
        private EndPresentationEntityCommandBufferSystem m_EndEntityCommandBufferSystem;

        protected override void OnCreateManager()
        {
            m_BeginEntityCommandBufferSystem = World.GetOrCreateManager<BeginPresentationEntityCommandBufferSystem>();
            m_EndEntityCommandBufferSystem = World.GetOrCreateManager<EndPresentationEntityCommandBufferSystem>();
            m_systemsToUpdate.Add(m_BeginEntityCommandBufferSystem);
            m_systemsToUpdate.Add(m_EndEntityCommandBufferSystem);
        }

        public override void SortSystemUpdateList()
        {
            // Extract list of systems to sort (excluding built-in systems that are inserted at fixed points)
            var toSort = new List<ComponentSystemBase>(m_systemsToUpdate.Count - 2);
            foreach (var s in m_systemsToUpdate)
            {
                if (s is BeginPresentationEntityCommandBufferSystem ||
                    s is EndPresentationEntityCommandBufferSystem)
                {
                    continue;
                }
                toSort.Add(s);
            }
            m_systemsToUpdate = toSort;
            base.SortSystemUpdateList();
            // Re-insert built-in systems to construct the final list
            var finalSystemList = new List<ComponentSystemBase>(1 + m_systemsToUpdate.Count + 1);
            finalSystemList.Add(m_BeginEntityCommandBufferSystem);
            foreach (var s in m_systemsToUpdate)
                finalSystemList.Add(s);
            finalSystemList.Add(m_EndEntityCommandBufferSystem);
            m_systemsToUpdate = finalSystemList;
        }
    }
}
