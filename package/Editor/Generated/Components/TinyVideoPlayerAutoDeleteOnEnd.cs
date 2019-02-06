// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Video
{
    internal partial struct TinyVideoPlayerAutoDeleteOnEnd : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyVideoPlayerAutoDeleteOnEnd>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyVideoPlayerAutoDeleteOnEnd Construct(TinyObject tiny) => new TinyVideoPlayerAutoDeleteOnEnd(tiny);
        private static TinyId s_Id = CoreIds.Video.VideoPlayerAutoDeleteOnEnd;
        private static TinyType.Reference s_Ref = TypeRefs.Video.VideoPlayerAutoDeleteOnEnd;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyVideoPlayerAutoDeleteOnEnd(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyVideoPlayerAutoDeleteOnEnd(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyVideoPlayerAutoDeleteOnEnd other)
        {
        }
    }
}
