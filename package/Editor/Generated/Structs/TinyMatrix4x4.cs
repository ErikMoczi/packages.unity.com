// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Math
{
    internal partial struct TinyMatrix4x4 : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Math.Matrix4x4;
        private static TinyType.Reference s_Ref = TypeRefs.Math.Matrix4x4;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyMatrix4x4(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyMatrix4x4(IRegistry registry) : this(new TinyObject(registry, s_Ref))
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

        public float @m03
        {
            get => Tiny.GetProperty<float>(nameof(@m03));
            set => Tiny.AssignIfDifferent(nameof(@m03), value);
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

        public float @m13
        {
            get => Tiny.GetProperty<float>(nameof(@m13));
            set => Tiny.AssignIfDifferent(nameof(@m13), value);
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

        public float @m23
        {
            get => Tiny.GetProperty<float>(nameof(@m23));
            set => Tiny.AssignIfDifferent(nameof(@m23), value);
        }

        public float @m30
        {
            get => Tiny.GetProperty<float>(nameof(@m30));
            set => Tiny.AssignIfDifferent(nameof(@m30), value);
        }

        public float @m31
        {
            get => Tiny.GetProperty<float>(nameof(@m31));
            set => Tiny.AssignIfDifferent(nameof(@m31), value);
        }

        public float @m32
        {
            get => Tiny.GetProperty<float>(nameof(@m32));
            set => Tiny.AssignIfDifferent(nameof(@m32), value);
        }

        public float @m33
        {
            get => Tiny.GetProperty<float>(nameof(@m33));
            set => Tiny.AssignIfDifferent(nameof(@m33), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyMatrix4x4 other)
        {
            @m00 = other.@m00;
            @m01 = other.@m01;
            @m02 = other.@m02;
            @m03 = other.@m03;
            @m10 = other.@m10;
            @m11 = other.@m11;
            @m12 = other.@m12;
            @m13 = other.@m13;
            @m20 = other.@m20;
            @m21 = other.@m21;
            @m22 = other.@m22;
            @m23 = other.@m23;
            @m30 = other.@m30;
            @m31 = other.@m31;
            @m32 = other.@m32;
            @m33 = other.@m33;
        }
    }
}
