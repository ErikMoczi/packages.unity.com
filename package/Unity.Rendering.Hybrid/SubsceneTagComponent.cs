using System;

namespace Unity.Entities
{
    [Serializable]
    public struct SubsceneTag : ISharedComponentData, IEquatable<SubsceneTag>
    {
        public int Location;
        public int SubsectionIndex;

        public SubsceneTag(int location, int subsectionIndex)
        {
            Location = location;
            SubsectionIndex = subsectionIndex;
        }

        public override int GetHashCode()
        {
            return (Location << 8) + SubsectionIndex;
        }

        public bool Equals(SubsceneTag other)
        {
            return Location == other.Location && SubsectionIndex == other.SubsectionIndex;
        }

        public static bool operator <(SubsceneTag lhs, SubsceneTag rhs)
        {
            return lhs.Location != rhs.Location ? lhs.Location < rhs.Location :
                lhs.SubsectionIndex < rhs.SubsectionIndex;
        }

        public static bool operator >(SubsceneTag lhs, SubsceneTag rhs)
        {
            return lhs.Location != rhs.Location ? lhs.Location > rhs.Location :
                lhs.SubsectionIndex > rhs.SubsectionIndex;
        }

        public override string ToString()
        {
            return $"SubSceneTag Location = {Location}, SubSectionIndex: {SubsectionIndex}";
        }
    }

    public class SubsceneTagComponent : SharedComponentDataWrapper<SubsceneTag>
    {
    }
}
