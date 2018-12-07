// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Text
{
    internal partial struct TinyText2DStyleBitmapFont : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyText2DStyleBitmapFont>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyText2DStyleBitmapFont Construct(TinyObject tiny) => new TinyText2DStyleBitmapFont(tiny);
        private static TinyId s_Id = CoreIds.Text.Text2DStyleBitmapFont;
        private static TinyType.Reference s_Ref = TypeRefs.Text.Text2DStyleBitmapFont;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyText2DStyleBitmapFont(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyText2DStyleBitmapFont(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TMPro.TMP_FontAsset @font
        {
            get => Tiny.GetProperty<TMPro.TMP_FontAsset>(nameof(@font));
            set => Tiny.AssignIfDifferent(nameof(@font), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyText2DStyleBitmapFont other)
        {
            @font = other.@font;
        }
    }
}
