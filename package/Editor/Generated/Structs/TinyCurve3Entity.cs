// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.TinyEditorExtensions
{
    internal partial struct TinyCurve3Entity : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.TinyEditorExtensions.Curve3Entity;
        private static TinyType.Reference s_Ref = TypeRefs.TinyEditorExtensions.Curve3Entity;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCurve3Entity(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyCurve3Entity(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.AnimationCurve @x
        {
            get => Tiny.GetProperty<UnityEngine.AnimationCurve>(nameof(@x));
            set => Tiny.AssignIfDifferent(nameof(@x), value);
        }

        public UnityEngine.AnimationCurve @y
        {
            get => Tiny.GetProperty<UnityEngine.AnimationCurve>(nameof(@y));
            set => Tiny.AssignIfDifferent(nameof(@y), value);
        }

        public UnityEngine.AnimationCurve @z
        {
            get => Tiny.GetProperty<UnityEngine.AnimationCurve>(nameof(@z));
            set => Tiny.AssignIfDifferent(nameof(@z), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyCurve3Entity other)
        {
            @x = other.@x;
            @y = other.@y;
            @z = other.@z;
        }
    }
}
