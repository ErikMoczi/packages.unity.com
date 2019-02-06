// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UIControls
{
    internal partial struct TinyButton : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyButton>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyButton Construct(TinyObject tiny) => new TinyButton(tiny);
        private static TinyId s_Id = CoreIds.UIControls.Button;
        private static TinyType.Reference s_Ref = TypeRefs.UIControls.Button;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyButton(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyButton(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
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

        #endregion // Properties

        public void CopyFrom(TinyButton other)
        {
            @sprite2DRenderer = other.@sprite2DRenderer;
            @transition = other.@transition;
        }
    }
}
