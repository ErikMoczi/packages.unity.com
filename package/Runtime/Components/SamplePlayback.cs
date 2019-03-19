using Unity.Entities;
using UnityEngine;

namespace Unity.Audio.Megacity
{
    /// <summary>
    /// Add this, together with a valid SharedAudioClip on any entity,
    /// and the clip will start playing.
    /// Changes you make to this component will be reflected in the sound
    /// in real time.
    /// </summary>
    /// <see cref="SharedAudioClip"/>
    public struct SamplePlayback : IComponentData
    {
        public float Volume;
        public float Left;
        public float Right;
        public float Pitch;
        public int Loop;
    }

    /// <summary>
    /// Add this, together with a SamplePlayback on any entity,
    /// and the clip will start playing.
    /// </summary>
    /// <see cref="SamplePlayback"/>
    public struct SharedAudioClip : IComponentData
    {
        public int ClipInstanceID;
    }
}
