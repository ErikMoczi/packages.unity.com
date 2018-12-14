


using System;

namespace Unity.Tiny
{
    internal interface ITinyYieldInstruction
    {
        bool HasCompleted { get; }
    }

    internal interface ITinyThrowingYieldInstruction
    {
        Exception ThrownDuringExecution { get; }
    }
}

