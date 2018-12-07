// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.TinyEditorExtensions
{
    internal partial struct TinyGradientColorKey : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.TinyEditorExtensions.GradientColorKey;
        private static TinyType.Reference s_Ref = TypeRefs.TinyEditorExtensions.GradientColorKey;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyGradientColorKey(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyGradientColorKey(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Color @color
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@color));
            set => Tiny.AssignIfDifferent(nameof(@color), value);
        }

        public float @time
        {
            get => Tiny.GetProperty<float>(nameof(@time));
            set => Tiny.AssignIfDifferent(nameof(@time), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyGradientColorKey other)
        {
            @color = other.@color;
            @time = other.@time;
        }
    }
}
