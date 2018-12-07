// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Math
{
    internal partial struct TinyMatrix3x3 : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Math.Matrix3x3;
        private static TinyType.Reference s_Ref = TypeRefs.Math.Matrix3x3;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyMatrix3x3(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyMatrix3x3(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @m00
        {
            get => Tiny.GetProperty<float>(nameof(@m00));
            set => Tiny.AssignIfDifferent(nameof(@m00), value);
        }

        public float @m01
        {
            get => Tiny.GetProperty<float>(nameof(@m01));
            set => Tiny.AssignIfDifferent(nameof(@m01), value);
        }

        public float @m02
        {
            get => Tiny.GetProperty<float>(nameof(@m02));
            set => Tiny.AssignIfDifferent(nameof(@m02), value);
        }

        public float @m10
        {
            get => Tiny.GetProperty<float>(nameof(@m10));
            set => Tiny.AssignIfDifferent(nameof(@m10), value);
        }

        public float @m11
        {
            get => Tiny.GetProperty<float>(nameof(@m11));
            set => Tiny.AssignIfDifferent(nameof(@m11), value);
        }

        public float @m12
        {
            get => Tiny.GetProperty<float>(nameof(@m12));
            set => Tiny.AssignIfDifferent(nameof(@m12), value);
        }

        public float @m20
        {
            get => Tiny.GetProperty<float>(nameof(@m20));
            set => Tiny.AssignIfDifferent(nameof(@m20), value);
        }

        public float @m21
        {
            get => Tiny.GetProperty<float>(nameof(@m21));
            set => Tiny.AssignIfDifferent(nameof(@m21), value);
        }

        public float @m22
        {
            get => Tiny.GetProperty<float>(nameof(@m22));
            set => Tiny.AssignIfDifferent(nameof(@m22), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyMatrix3x3 other)
        {
            @m00 = other.@m00;
            @m01 = other.@m01;
            @m02 = other.@m02;
            @m10 = other.@m10;
            @m11 = other.@m11;
            @m12 = other.@m12;
            @m20 = other.@m20;
            @m21 = other.@m21;
            @m22 = other.@m22;
        }
    }
}
