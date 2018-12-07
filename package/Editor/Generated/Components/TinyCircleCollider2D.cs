// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinyCircleCollider2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyCircleCollider2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyCircleCollider2D Construct(TinyObject tiny) => new TinyCircleCollider2D(tiny);
        private static TinyId s_Id = CoreIds.Physics2D.CircleCollider2D;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.CircleCollider2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCircleCollider2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyCircleCollider2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @radius
        {
            get => Tiny.GetProperty<float>(nameof(@radius));
            set => Tiny.AssignIfDifferent(nameof(@radius), value);
        }

        public UnityEngine.Vector2 @pivot
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@pivot));
            set => Tiny.AssignIfDifferent(nameof(@pivot), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyCircleCollider2D other)
        {
            @radius = other.@radius;
            @pivot = other.@pivot;
        }
    }
}
