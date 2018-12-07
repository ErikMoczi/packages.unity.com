// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tweens
{
    internal partial struct TinyTweenComponent : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTweenComponent>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTweenComponent Construct(TinyObject tiny) => new TinyTweenComponent(tiny);
        private static TinyId s_Id = CoreIds.Tweens.TweenComponent;
        private static TinyType.Reference s_Ref = TypeRefs.Tweens.TweenComponent;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTweenComponent(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTweenComponent(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @target
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@target));
            set => Tiny.AssignIfDifferent(nameof(@target), value);
        }

        public uint @offset
        {
            get => Tiny.GetProperty<uint>(nameof(@offset));
            set => Tiny.AssignIfDifferent(nameof(@offset), value);
        }

        public float @duration
        {
            get => Tiny.GetProperty<float>(nameof(@duration));
            set => Tiny.AssignIfDifferent(nameof(@duration), value);
        }

        public Tweens.TinyTweenFunc @func
        {
            get => Tiny.GetProperty<Tweens.TinyTweenFunc>(nameof(@func));
            set => Tiny.AssignIfDifferent(nameof(@func), value);
        }

        public Core2D.TinyLoopMode @loop
        {
            get => Tiny.GetProperty<Core2D.TinyLoopMode>(nameof(@loop));
            set => Tiny.AssignIfDifferent(nameof(@loop), value);
        }

        public bool @destroyWhenDone
        {
            get => Tiny.GetProperty<bool>(nameof(@destroyWhenDone));
            set => Tiny.AssignIfDifferent(nameof(@destroyWhenDone), value);
        }

        public float @t
        {
            get => Tiny.GetProperty<float>(nameof(@t));
            set => Tiny.AssignIfDifferent(nameof(@t), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTweenComponent other)
        {
            @target = other.@target;
            @offset = other.@offset;
            @duration = other.@duration;
            @func = other.@func;
            @loop = other.@loop;
            @destroyWhenDone = other.@destroyWhenDone;
            @t = other.@t;
        }
    }
}
