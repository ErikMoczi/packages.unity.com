// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Text
{
    internal partial struct TinyText2DStyleNativeFont : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyText2DStyleNativeFont>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyText2DStyleNativeFont Construct(TinyObject tiny) => new TinyText2DStyleNativeFont(tiny);
        private static TinyId s_Id = CoreIds.Text.Text2DStyleNativeFont;
        private static TinyType.Reference s_Ref = TypeRefs.Text.Text2DStyleNativeFont;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyText2DStyleNativeFont(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyText2DStyleNativeFont(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @font
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@font));
            set => Tiny.AssignIfDifferent(nameof(@font), value);
        }

        public bool @italic
        {
            get => Tiny.GetProperty<bool>(nameof(@italic));
            set => Tiny.AssignIfDifferent(nameof(@italic), value);
        }

        public int @weight
        {
            get => Tiny.GetProperty<int>(nameof(@weight));
            set => Tiny.AssignIfDifferent(nameof(@weight), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyText2DStyleNativeFont other)
        {
            @font = other.@font;
            @italic = other.@italic;
            @weight = other.@weight;
        }
    }
}
