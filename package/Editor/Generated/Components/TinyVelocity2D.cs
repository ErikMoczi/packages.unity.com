// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinyVelocity2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyVelocity2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyVelocity2D Construct(TinyObject tiny) => new TinyVelocity2D(tiny);
        private static TinyId s_Id = CoreIds.Physics2D.Velocity2D;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.Velocity2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyVelocity2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyVelocity2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector2 @velocity
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@velocity));
            set => Tiny.AssignIfDifferent(nameof(@velocity), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyVelocity2D other)
        {
            @velocity = other.@velocity;
        }
    }
}
