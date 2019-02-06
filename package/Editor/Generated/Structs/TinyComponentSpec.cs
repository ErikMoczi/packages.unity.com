// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.ut
{
    internal partial struct TinyComponentSpec : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.ut.ComponentSpec;
        private static TinyType.Reference s_Ref = TypeRefs.ut.ComponentSpec;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyComponentSpec(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyComponentSpec(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public ut.TinyAccessMode @access
        {
            get => Tiny.GetProperty<ut.TinyAccessMode>(nameof(@access));
            set => Tiny.AssignIfDifferent(nameof(@access), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyComponentSpec other)
        {
            @access = other.@access;
        }
    }
}
