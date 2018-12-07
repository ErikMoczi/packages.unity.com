// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Text
{
    internal partial struct TinyNativeFont : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyNativeFont>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyNativeFont Construct(TinyObject tiny) => new TinyNativeFont(tiny);
        private static TinyId s_Id = CoreIds.Text.NativeFont;
        private static TinyType.Reference s_Ref = TypeRefs.Text.NativeFont;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyNativeFont(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyNativeFont(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Text.TinyFontName @name
        {
            get => Tiny.GetProperty<Text.TinyFontName>(nameof(@name));
            set => Tiny.AssignIfDifferent(nameof(@name), value);
        }

        public float @worldUnitsToPt
        {
            get => Tiny.GetProperty<float>(nameof(@worldUnitsToPt));
            set => Tiny.AssignIfDifferent(nameof(@worldUnitsToPt), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyNativeFont other)
        {
            @name = other.@name;
            @worldUnitsToPt = other.@worldUnitsToPt;
        }
    }
}
