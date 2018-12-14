// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Audio
{
    internal partial struct TinyAudioSourceStop : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAudioSourceStop>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAudioSourceStop Construct(TinyObject tiny) => new TinyAudioSourceStop(tiny);
        private static TinyId s_Id = CoreIds.Audio.AudioSourceStop;
        private static TinyType.Reference s_Ref = TypeRefs.Audio.AudioSourceStop;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAudioSourceStop(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAudioSourceStop(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyAudioSourceStop other)
        {
        }
    }
}
