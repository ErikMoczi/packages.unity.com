using System;
using Unity.Entities;

namespace Unity.Audio.Megacity
{
    [Serializable]
    public struct MusicTrigger : ISharedComponentData
    {
        public int musicIndex;
    }

    public class MusicTriggerComponent : SharedComponentDataProxy<MusicTrigger>
    {
    }
}
