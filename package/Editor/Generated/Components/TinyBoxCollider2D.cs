// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinyBoxCollider2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyBoxCollider2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyBoxCollider2D Construct(TinyObject tiny) => new TinyBoxCollider2D(tiny);
        private static TinyId s_Id = CoreIds.Physics2D.BoxCollider2D;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.BoxCollider2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyBoxCollider2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyBoxCollider2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector2 @size
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@size));
            set => Tiny.AssignIfDifferent(nameof(@size), value);
        }

        public UnityEngine.Vector2 @pivot
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@pivot));
            set => Tiny.AssignIfDifferent(nameof(@pivot), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyBoxCollider2D other)
        {
            @size = other.@size;
            @pivot = other.@pivot;
        }
    }
}
