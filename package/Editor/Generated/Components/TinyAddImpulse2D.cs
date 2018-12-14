// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinyAddImpulse2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAddImpulse2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAddImpulse2D Construct(TinyObject tiny) => new TinyAddImpulse2D(tiny);
        private static TinyId s_Id = CoreIds.Physics2D.AddImpulse2D;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.AddImpulse2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAddImpulse2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAddImpulse2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector2 @point
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@point));
            set => Tiny.AssignIfDifferent(nameof(@point), value);
        }

        public UnityEngine.Vector2 @impulse
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@impulse));
            set => Tiny.AssignIfDifferent(nameof(@impulse), value);
        }

        public bool @scaleImpulse
        {
            get => Tiny.GetProperty<bool>(nameof(@scaleImpulse));
            set => Tiny.AssignIfDifferent(nameof(@scaleImpulse), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAddImpulse2D other)
        {
            @point = other.@point;
            @impulse = other.@impulse;
            @scaleImpulse = other.@scaleImpulse;
        }
    }
}
