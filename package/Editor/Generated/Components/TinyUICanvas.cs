// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UILayout
{
    internal partial struct TinyUICanvas : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyUICanvas>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyUICanvas Construct(TinyObject tiny) => new TinyUICanvas(tiny);
        private static TinyId s_Id = CoreIds.UILayout.UICanvas;
        private static TinyType.Reference s_Ref = TypeRefs.UILayout.UICanvas;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyUICanvas(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyUICanvas(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @camera
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@camera));
            set => Tiny.AssignIfDifferent(nameof(@camera), value);
        }

        public UnityEngine.UI.CanvasScaler.ScaleMode @uiScaleMode
        {
            get => Tiny.GetProperty<UnityEngine.UI.CanvasScaler.ScaleMode>(nameof(@uiScaleMode));
            set => Tiny.AssignIfDifferent(nameof(@uiScaleMode), value);
        }

        public UnityEngine.Vector2 @referenceResolution
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@referenceResolution));
            set => Tiny.AssignIfDifferent(nameof(@referenceResolution), value);
        }

        public float @matchWidthOrHeight
        {
            get => Tiny.GetProperty<float>(nameof(@matchWidthOrHeight));
            set => Tiny.AssignIfDifferent(nameof(@matchWidthOrHeight), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyUICanvas other)
        {
            @camera = other.@camera;
            @uiScaleMode = other.@uiScaleMode;
            @referenceResolution = other.@referenceResolution;
            @matchWidthOrHeight = other.@matchWidthOrHeight;
        }
    }
}
