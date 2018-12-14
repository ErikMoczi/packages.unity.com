// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Audio
{
    internal partial struct TinyAudioSourceStart : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAudioSourceStart>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAudioSourceStart Construct(TinyObject tiny) => new TinyAudioSourceStart(tiny);
        private static TinyId s_Id = CoreIds.Audio.AudioSourceStart;
        private static TinyType.Reference s_Ref = TypeRefs.Audio.AudioSourceStart;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAudioSourceStart(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAudioSourceStart(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyAudioSourceStart other)
        {
        }
    }
}
