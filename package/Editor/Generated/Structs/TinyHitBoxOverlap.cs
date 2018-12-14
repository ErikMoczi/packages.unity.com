// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.HitBox2D
{
    internal partial struct TinyHitBoxOverlap : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.HitBox2D.HitBoxOverlap;
        private static TinyType.Reference s_Ref = TypeRefs.HitBox2D.HitBoxOverlap;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyHitBoxOverlap(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyHitBoxOverlap(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyHitBoxOverlap other)
        {
        }
    }
}
