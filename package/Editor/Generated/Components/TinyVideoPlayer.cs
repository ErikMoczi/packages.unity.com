// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Video
{
    internal partial struct TinyVideoPlayer : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyVideoPlayer>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyVideoPlayer Construct(TinyObject tiny) => new TinyVideoPlayer(tiny);
        private static TinyId s_Id = CoreIds.Video.VideoPlayer;
        private static TinyType.Reference s_Ref = TypeRefs.Video.VideoPlayer;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyVideoPlayer(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyVideoPlayer(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @clip
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@clip));
            set => Tiny.AssignIfDifferent(nameof(@clip), value);
        }

        public bool @controls
        {
            get => Tiny.GetProperty<bool>(nameof(@controls));
            set => Tiny.AssignIfDifferent(nameof(@controls), value);
        }

        public bool @loop
        {
            get => Tiny.GetProperty<bool>(nameof(@loop));
            set => Tiny.AssignIfDifferent(nameof(@loop), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyVideoPlayer other)
        {
            @clip = other.@clip;
            @controls = other.@controls;
            @loop = other.@loop;
        }
    }
}
