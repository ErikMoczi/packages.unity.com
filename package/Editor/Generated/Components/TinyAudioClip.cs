// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Audio
{
    internal partial struct TinyAudioClip : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAudioClip>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAudioClip Construct(TinyObject tiny) => new TinyAudioClip(tiny);
        private static TinyId s_Id = CoreIds.Audio.AudioClip;
        private static TinyType.Reference s_Ref = TypeRefs.Audio.AudioClip;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAudioClip(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAudioClip(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyAudioClip other)
        {
        }
    }
}
