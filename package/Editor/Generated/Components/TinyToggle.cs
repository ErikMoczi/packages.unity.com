// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UIControls
{
    internal partial struct TinyToggle : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyToggle>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyToggle Construct(TinyObject tiny) => new TinyToggle(tiny);
        private static TinyId s_Id = CoreIds.UIControls.Toggle;
        private static TinyType.Reference s_Ref = TypeRefs.UIControls.Toggle;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyToggle(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyToggle(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public bool @isOn
        {
            get => Tiny.GetProperty<bool>(nameof(@isOn));
            set => Tiny.AssignIfDifferent(nameof(@isOn), value);
        }

        public Unity.Tiny.TinyEntity.Reference @sprite2DRenderer
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@sprite2DRenderer));
            set => Tiny.AssignIfDifferent(nameof(@sprite2DRenderer), value);
        }

        public UIControlsExtensions.TinyTransitionEntity @transition
        {
            get => new UIControlsExtensions.TinyTransitionEntity(Tiny[nameof(@transition)] as TinyObject);
            set => new UIControlsExtensions.TinyTransitionEntity(Tiny[nameof(@transition)] as TinyObject).CopyFrom(value);
        }

        public UIControlsExtensions.TinyTransitionEntity @transitionChecked
        {
            get => new UIControlsExtensions.TinyTransitionEntity(Tiny[nameof(@transitionChecked)] as TinyObject);
            set => new UIControlsExtensions.TinyTransitionEntity(Tiny[nameof(@transitionChecked)] as TinyObject).CopyFrom(value);
        }

        #endregion // Properties

        public void CopyFrom(TinyToggle other)
        {
            @isOn = other.@isOn;
            @sprite2DRenderer = other.@sprite2DRenderer;
            @transition = other.@transition;
            @transitionChecked = other.@transitionChecked;
        }
    }
}
