// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinyPhysics2DConfig : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Physics2D.Physics2DConfig;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.Physics2DConfig;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyPhysics2DConfig(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyPhysics2DConfig(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector2 @gravity
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@gravity));
            set => Tiny.AssignIfDifferent(nameof(@gravity), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyPhysics2DConfig other)
        {
            @gravity = other.@gravity;
        }
    }
}
