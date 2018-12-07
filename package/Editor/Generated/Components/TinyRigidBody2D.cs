// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinyRigidBody2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyRigidBody2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyRigidBody2D Construct(TinyObject tiny) => new TinyRigidBody2D(tiny);
        private static TinyId s_Id = CoreIds.Physics2D.RigidBody2D;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.RigidBody2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyRigidBody2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyRigidBody2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Physics2D.TinyBodyType @bodyType
        {
            get => Tiny.GetProperty<Physics2D.TinyBodyType>(nameof(@bodyType));
            set => Tiny.AssignIfDifferent(nameof(@bodyType), value);
        }

        public bool @freezeRotation
        {
            get => Tiny.GetProperty<bool>(nameof(@freezeRotation));
            set => Tiny.AssignIfDifferent(nameof(@freezeRotation), value);
        }

        public float @friction
        {
            get => Tiny.GetProperty<float>(nameof(@friction));
            set => Tiny.AssignIfDifferent(nameof(@friction), value);
        }

        public float @restitution
        {
            get => Tiny.GetProperty<float>(nameof(@restitution));
            set => Tiny.AssignIfDifferent(nameof(@restitution), value);
        }

        public float @density
        {
            get => Tiny.GetProperty<float>(nameof(@density));
            set => Tiny.AssignIfDifferent(nameof(@density), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyRigidBody2D other)
        {
            @bodyType = other.@bodyType;
            @freezeRotation = other.@freezeRotation;
            @friction = other.@friction;
            @restitution = other.@restitution;
            @density = other.@density;
        }
    }
}
