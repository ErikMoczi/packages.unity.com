// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Text
{
    internal partial struct TinyText2DStyle : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyText2DStyle>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyText2DStyle Construct(TinyObject tiny) => new TinyText2DStyle(tiny);
        private static TinyId s_Id = CoreIds.Text.Text2DStyle;
        private static TinyType.Reference s_Ref = TypeRefs.Text.Text2DStyle;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyText2DStyle(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyText2DStyle(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Color @color
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@color));
            set => Tiny.AssignIfDifferent(nameof(@color), value);
        }

        public float @size
        {
            get => Tiny.GetProperty<float>(nameof(@size));
            set => Tiny.AssignIfDifferent(nameof(@size), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyText2DStyle other)
        {
            @color = other.@color;
            @size = other.@size;
        }
    }
}
