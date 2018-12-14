// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyImage2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyImage2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyImage2D Construct(TinyObject tiny) => new TinyImage2D(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Image2D;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Image2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyImage2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyImage2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @pixelsToWorldUnits
        {
            get => Tiny.GetProperty<float>(nameof(@pixelsToWorldUnits));
            set => Tiny.AssignIfDifferent(nameof(@pixelsToWorldUnits), value);
        }

        public bool @disableSmoothing
        {
            get => Tiny.GetProperty<bool>(nameof(@disableSmoothing));
            set => Tiny.AssignIfDifferent(nameof(@disableSmoothing), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyImage2D other)
        {
            @pixelsToWorldUnits = other.@pixelsToWorldUnits;
            @disableSmoothing = other.@disableSmoothing;
        }
    }
}
