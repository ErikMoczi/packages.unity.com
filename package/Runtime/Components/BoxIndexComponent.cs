using System;
using Unity.Entities;

namespace Unity.Audio.Megacity
{
    [Serializable]
    public struct BoxIndex : IComponentData
    {
        public Entity prevBoundingBox;
        public Entity currBoundingBox;
    }

    public class BoxIndexComponent : ComponentDataProxy<BoxIndex>
    {
    }
}
