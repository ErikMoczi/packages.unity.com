

namespace Unity.Tiny
{
    internal struct FallbackYieldInstruction : ITinyYieldInstruction
    {
        public bool HasCompleted => true;
    }
}

