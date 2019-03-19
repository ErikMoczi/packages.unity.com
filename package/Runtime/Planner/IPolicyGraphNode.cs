using System;

namespace Unity.AI.Planner
{
    interface IPolicyGraphNode
    {
        bool Complete { get; }
        int Iterations { get; }
    }
}
