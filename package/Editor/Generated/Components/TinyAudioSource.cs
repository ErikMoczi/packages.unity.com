// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Audio
{
    internal partial struct TinyAudioSource : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAudioSource>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAudioSource Construct(TinyObject tiny) => new TinyAudioSource(tiny);
        private static TinyId s_Id = CoreIds.Audio.AudioSource;
        private static TinyType.Reference s_Ref = TypeRefs.Audio.AudioSource;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAudioSource(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAudioSource(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.AudioClip @clip
        {
            get => Tiny.GetProperty<UnityEngine.AudioClip>(nameof(@clip));
            set => Tiny.AssignIfDifferent(nameof(@clip), value);
        }

        public float @volume
        {
            get => Tiny.GetProperty<float>(nameof(@volume));
            set => Tiny.AssignIfDifferent(nameof(@volume), value);
        }

        public bool @loop
        {
            get => Tiny.GetProperty<bool>(nameof(@loop));
            set => Tiny.AssignIfDifferent(nameof(@loop), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAudioSource other)
        {
            @clip = other.@clip;
            @volume = other.@volume;
            @loop = other.@loop;
        }
    }
}
