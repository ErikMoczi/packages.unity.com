// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.TinyEditorExtensions
{
    internal partial struct TinyGradientAlphaKey : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.TinyEditorExtensions.GradientAlphaKey;
        private static TinyType.Reference s_Ref = TypeRefs.TinyEditorExtensions.GradientAlphaKey;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyGradientAlphaKey(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyGradientAlphaKey(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @alpha
        {
            get => Tiny.GetProperty<float>(nameof(@alpha));
            set => Tiny.AssignIfDifferent(nameof(@alpha), value);
        }

        public float @time
        {
            get => Tiny.GetProperty<float>(nameof(@time));
            set => Tiny.AssignIfDifferent(nameof(@time), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyGradientAlphaKey other)
        {
            @alpha = other.@alpha;
            @time = other.@time;
        }
    }
}
