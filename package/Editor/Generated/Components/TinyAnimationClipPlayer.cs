// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Animation
{
    internal partial struct TinyAnimationClipPlayer : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAnimationClipPlayer>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAnimationClipPlayer Construct(TinyObject tiny) => new TinyAnimationClipPlayer(tiny);
        private static TinyId s_Id = CoreIds.Animation.AnimationClipPlayer;
        private static TinyType.Reference s_Ref = TypeRefs.Animation.AnimationClipPlayer;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAnimationClipPlayer(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAnimationClipPlayer(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.AnimationClip @animationClip
        {
            get => Tiny.GetProperty<UnityEngine.AnimationClip>(nameof(@animationClip));
            set => Tiny.AssignIfDifferent(nameof(@animationClip), value);
        }

        public float @time
        {
            get => Tiny.GetProperty<float>(nameof(@time));
            set => Tiny.AssignIfDifferent(nameof(@time), value);
        }

        public float @speed
        {
            get => Tiny.GetProperty<float>(nameof(@speed));
            set => Tiny.AssignIfDifferent(nameof(@speed), value);
        }

        public bool @paused
        {
            get => Tiny.GetProperty<bool>(nameof(@paused));
            set => Tiny.AssignIfDifferent(nameof(@paused), value);
        }

        public Core2D.TinyLoopMode @loopMode
        {
            get => Tiny.GetProperty<Core2D.TinyLoopMode>(nameof(@loopMode));
            set => Tiny.AssignIfDifferent(nameof(@loopMode), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAnimationClipPlayer other)
        {
            @animationClip = other.@animationClip;
            @time = other.@time;
            @speed = other.@speed;
            @paused = other.@paused;
            @loopMode = other.@loopMode;
        }
    }
}
