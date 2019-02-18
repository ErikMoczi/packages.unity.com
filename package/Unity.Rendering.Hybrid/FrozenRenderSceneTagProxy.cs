using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [Serializable]
    public struct FrozenRenderSceneTag : ISharedComponentData, IEquatable<FrozenRenderSceneTag>
    {
        public int4             Location;
        public int              SubsectionIndex;
        public int              HasStreamedLOD;

        public bool Equals(FrozenRenderSceneTag other)
        {
            return Location.Equals(other.Location) && SubsectionIndex == other.SubsectionIndex;
        }
    }

    public class FrozenRenderSceneTagProxy : SharedComponentDataProxy<FrozenRenderSceneTag>
    {
    }
}
