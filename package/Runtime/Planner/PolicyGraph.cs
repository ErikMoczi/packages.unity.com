using System;
using Unity.Entities;

namespace Unity.AI.Planner
{
    struct PolicyGraphNode : IComponentData, IPolicyGraphNode
    {
        public float PolicyValue; // Initially, this might be the heuristic reward, after expansion it is max over action values
        public Entity StateEntity;
        public Entity OptimalActionEntity;
        public Bool Complete;
        public int Iterations;
        public int Horizon;

        bool IPolicyGraphNode.Complete => Complete;
        int IPolicyGraphNode.Iterations => Iterations;
    }

    [InternalBufferCapacity(2)]
    struct PolicyGraphNodeParent : IBufferElementData
    {
        public Entity ParentPolicyGraphNodeEntity;
    }

    [InternalBufferCapacity(4)]
    struct ActionNodeArgument : IBufferElementData
    {
        public int DomainObjectReferenceIndex;
    }

    struct ActionNode : IComponentData, IEquatable<ActionNode>
    {
        public float ActionValue;
        public int VisitCount;
        public Bool Complete;
        public Guid ActionSystemGuid;

        public bool Equals(ActionNode other)
        {
            return ActionValue.Equals(other.ActionValue) && VisitCount == other.VisitCount
                && Complete.Equals(other.Complete) && ActionSystemGuid.Equals(other.ActionSystemGuid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ActionNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ActionValue.GetHashCode();
                hashCode = (hashCode * 397) ^ VisitCount;
                hashCode = (hashCode * 397) ^ Complete.GetHashCode();
                hashCode = (hashCode * 397) ^ ActionSystemGuid.GetHashCode();
                return hashCode;
            }
        }
    }

    [InternalBufferCapacity(5)]
    struct ActionNodeReference : IBufferElementData
    {
        public Entity ActionNodeEntity;
    }

    [InternalBufferCapacity(3)]
    struct ActionResult: IBufferElementData
    {
        public float Probability;
        public float TransitionUtilityValue; // the reward for taking the action
        public Entity PolicyGraphNodeEntity;
    }

    struct CreatedStateInfo : IComponentData
    {
        public Entity ParentPolicyGraphNodeEntity;
        public Entity ParentStateEntity;
        public Entity ActionNodeEntity;
        public int StateHorizon;

        public float Probability;
        public float TransitionUtilityValue;
    }

    // Flags
    struct Selected : IComponentData { }
}
