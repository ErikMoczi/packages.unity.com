// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UIControls
{
    internal partial struct TinyColorTintTransition : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyColorTintTransition>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyColorTintTransition Construct(TinyObject tiny) => new TinyColorTintTransition(tiny);
        private static TinyId s_Id = CoreIds.UIControls.ColorTintTransition;
        private static TinyType.Reference s_Ref = TypeRefs.UIControls.ColorTintTransition;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyColorTintTransition(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyColorTintTransition(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Color @normal
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@normal));
            set => Tiny.AssignIfDifferent(nameof(@normal), value);
        }

        public UnityEngine.Color @hover
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@hover));
            set => Tiny.AssignIfDifferent(nameof(@hover), value);
        }

        public UnityEngine.Color @pressed
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@pressed));
            set => Tiny.AssignIfDifferent(nameof(@pressed), value);
        }

        public UnityEngine.Color @disabled
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@disabled));
            set => Tiny.AssignIfDifferent(nameof(@disabled), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyColorTintTransition other)
        {
            @normal = other.@normal;
            @hover = other.@hover;
            @pressed = other.@pressed;
            @disabled = other.@disabled;
        }
    }
}
