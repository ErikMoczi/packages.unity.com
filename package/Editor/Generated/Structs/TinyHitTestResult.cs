// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.HitBox2D
{
    internal partial struct TinyHitTestResult : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.HitBox2D.HitTestResult;
        private static TinyType.Reference s_Ref = TypeRefs.HitBox2D.HitTestResult;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyHitTestResult(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyHitTestResult(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyHitTestResult other)
        {
        }
    }
}
