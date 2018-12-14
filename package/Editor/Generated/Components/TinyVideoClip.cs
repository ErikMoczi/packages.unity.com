// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Video
{
    internal partial struct TinyVideoClip : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyVideoClip>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyVideoClip Construct(TinyObject tiny) => new TinyVideoClip(tiny);
        private static TinyId s_Id = CoreIds.Video.VideoClip;
        private static TinyType.Reference s_Ref = TypeRefs.Video.VideoClip;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyVideoClip(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyVideoClip(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public string @src
        {
            get => Tiny.GetProperty<string>(nameof(@src));
            set => Tiny.AssignIfDifferent(nameof(@src), value);
        }

        public Video.TinyVideoClipLoadingStatus @status
        {
            get => Tiny.GetProperty<Video.TinyVideoClipLoadingStatus>(nameof(@status));
            set => Tiny.AssignIfDifferent(nameof(@status), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyVideoClip other)
        {
            @src = other.@src;
            @status = other.@status;
        }
    }
}
