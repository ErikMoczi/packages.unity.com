// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinySprite2DSequencePlayer : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySprite2DSequencePlayer>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySprite2DSequencePlayer Construct(TinyObject tiny) => new TinySprite2DSequencePlayer(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Sprite2DSequencePlayer;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Sprite2DSequencePlayer;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySprite2DSequencePlayer(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySprite2DSequencePlayer(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @sequence
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@sequence));
            set => Tiny.AssignIfDifferent(nameof(@sequence), value);
        }

        public bool @paused
        {
            get => Tiny.GetProperty<bool>(nameof(@paused));
            set => Tiny.AssignIfDifferent(nameof(@paused), value);
        }

        public Core2D.TinyLoopMode @loop
        {
            get => Tiny.GetProperty<Core2D.TinyLoopMode>(nameof(@loop));
            set => Tiny.AssignIfDifferent(nameof(@loop), value);
        }

        public float @speed
        {
            get => Tiny.GetProperty<float>(nameof(@speed));
            set => Tiny.AssignIfDifferent(nameof(@speed), value);
        }

        public float @time
        {
            get => Tiny.GetProperty<float>(nameof(@time));
            set => Tiny.AssignIfDifferent(nameof(@time), value);
        }

        #endregion // Properties

        public void CopyFrom(TinySprite2DSequencePlayer other)
        {
            @sequence = other.@sequence;
            @paused = other.@paused;
            @loop = other.@loop;
            @speed = other.@speed;
            @time = other.@time;
        }
    }
}
