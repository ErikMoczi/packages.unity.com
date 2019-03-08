using System;
#if !UNITY_ZEROPLAYER
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.LowLevel;
using UnityEngine.Experimental.PlayerLoop;
#endif

namespace Unity.Entities
{
    // Updating before or after a system constrains the scheduler ordering of these systems within a ComponentSystemGroup.
    // Both the before & after system must be a members of the same ComponentSystemGroup.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UpdateBeforeAttribute : Attribute
    {
        public UpdateBeforeAttribute(Type systemType)
        {
            SystemType = systemType;
        }

        public Type SystemType { get; }
    }

    // Updating before or after a system constrains the scheduler ordering of these systems within a ComponentSystemGroup.
    // Both the before & after system must be a members of the same ComponentSystemGroup.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UpdateAfterAttribute : Attribute
    {
        public UpdateAfterAttribute(Type systemType)
        {
            SystemType = systemType;
        }

        public Type SystemType { get; }
    }

    // The specified Type must be a ComponentSystemGroup.
    // Updating in a group means this system will be automatically updated by the specified ComponentSystemGroup.
    // The system may order itself relative to other systems in the group with UpdateBegin and UpdateEnd,
    // There is nothing preventing systems from being in multiple groups, it can be added if there is a use-case for it
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateInGroupAttribute : Attribute
    {
        public UpdateInGroupAttribute(Type groupType)
        {
            GroupType = groupType;
        }

        public Type GroupType { get; }
    }

#if !UNITY_ZEROPLAYER
    public static class ScriptBehaviourUpdateOrder
    {
        private static void InsertManagerIntoSubsystemList<T>(PlayerLoopSystem[] subsystemList, int insertIndex, T mgr)
            where T : ScriptBehaviourManager
        {
            var del = new DummyDelegateWrapper(mgr);
            subsystemList[insertIndex].type = typeof(T);
            subsystemList[insertIndex].updateDelegate = del.TriggerUpdate;
        }

        public static void UpdatePlayerLoop(World world)
        {
            var playerLoop = PlayerLoop.GetDefaultPlayerLoop();
            if (world != null)
            {
                // Insert the root-level systems into the appropriate PlayerLoopSystem subsystems:
                for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    int subsystemListLength = playerLoop.subSystemList[i].subSystemList.Length;
                    if (playerLoop.subSystemList[i].type == typeof(Update))
                    {
                        var newSubsystemList = new PlayerLoopSystem[subsystemListLength + 1];
                        for (var j = 0; j < subsystemListLength; ++j)
                            newSubsystemList[j] = playerLoop.subSystemList[i].subSystemList[j];
                        InsertManagerIntoSubsystemList(newSubsystemList,
                            subsystemListLength + 0, world.GetOrCreateManager<SimulationSystemGroup>());
                        playerLoop.subSystemList[i].subSystemList = newSubsystemList;
                    }
                    else if (playerLoop.subSystemList[i].type == typeof(PreLateUpdate))
                    {
                        var newSubsystemList = new PlayerLoopSystem[subsystemListLength + 1];
                        for (var j = 0; j < subsystemListLength; ++j)
                            newSubsystemList[j] = playerLoop.subSystemList[i].subSystemList[j];
                        InsertManagerIntoSubsystemList(newSubsystemList,
                            subsystemListLength + 0, world.GetOrCreateManager<PresentationSystemGroup>());
                        playerLoop.subSystemList[i].subSystemList = newSubsystemList;
                    }
                    else if (playerLoop.subSystemList[i].type == typeof(Initialization))
                    {
                        var newSubsystemList = new PlayerLoopSystem[subsystemListLength + 1];
                        for (var j = 0; j < subsystemListLength; ++j)
                            newSubsystemList[j] = playerLoop.subSystemList[i].subSystemList[j];
                        InsertManagerIntoSubsystemList(newSubsystemList,
                            subsystemListLength + 0, world.GetOrCreateManager<InitializationSystemGroup>());
                        playerLoop.subSystemList[i].subSystemList = newSubsystemList;
                    }
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
            currentPlayerLoop = playerLoop;
        }

        public static PlayerLoopSystem CurrentPlayerLoop => currentPlayerLoop;
        private static PlayerLoopSystem currentPlayerLoop;

        public static void SetPlayerLoop(PlayerLoopSystem playerLoop)
        {
            PlayerLoop.SetPlayerLoop(playerLoop);
            currentPlayerLoop = playerLoop;
        }

        // FIXME: HACK! - mono 4.6 has problems invoking virtual methods as delegates from native, so wrap the invocation in a non-virtual class
        internal class DummyDelegateWrapper
        {

            internal ScriptBehaviourManager Manager => m_Manager;
            private readonly ScriptBehaviourManager m_Manager;

            public DummyDelegateWrapper(ScriptBehaviourManager man)
            {
                m_Manager = man;
            }

            public void TriggerUpdate()
            {
                m_Manager.Update();
            }
        }
    }
#endif
}
