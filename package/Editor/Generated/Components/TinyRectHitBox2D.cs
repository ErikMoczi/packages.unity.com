// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.HitBox2D
{
    internal partial struct TinyRectHitBox2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyRectHitBox2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyRectHitBox2D Construct(TinyObject tiny) => new TinyRectHitBox2D(tiny);
        private static TinyId s_Id = CoreIds.HitBox2D.RectHitBox2D;
        private static TinyType.Reference s_Ref = TypeRefs.HitBox2D.RectHitBox2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyRectHitBox2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyRectHitBox2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Rect @box
        {
            get => Tiny.GetProperty<UnityEngine.Rect>(nameof(@box));
            set => Tiny.AssignIfDifferent(nameof(@box), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyRectHitBox2D other)
        {
            @box = other.@box;
        }
    }
}
