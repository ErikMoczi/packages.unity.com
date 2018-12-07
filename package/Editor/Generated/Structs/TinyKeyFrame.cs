// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.TinyEditorExtensions
{
    internal partial struct TinyKeyFrame : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.TinyEditorExtensions.KeyFrame;
        private static TinyType.Reference s_Ref = TypeRefs.TinyEditorExtensions.KeyFrame;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyKeyFrame(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyKeyFrame(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @inTangent
        {
            get => Tiny.GetProperty<float>(nameof(@inTangent));
            set => Tiny.AssignIfDifferent(nameof(@inTangent), value);
        }

        public float @inWeight
        {
            get => Tiny.GetProperty<float>(nameof(@inWeight));
            set => Tiny.AssignIfDifferent(nameof(@inWeight), value);
        }

        public float @outTangent
        {
            get => Tiny.GetProperty<float>(nameof(@outTangent));
            set => Tiny.AssignIfDifferent(nameof(@outTangent), value);
        }

        public float @outWeight
        {
            get => Tiny.GetProperty<float>(nameof(@outWeight));
            set => Tiny.AssignIfDifferent(nameof(@outWeight), value);
        }

        public float @time
        {
            get => Tiny.GetProperty<float>(nameof(@time));
            set => Tiny.AssignIfDifferent(nameof(@time), value);
        }

        public float @value
        {
            get => Tiny.GetProperty<float>(nameof(@value));
            set => Tiny.AssignIfDifferent(nameof(@value), value);
        }

        public TinyEditorExtensions.TinyWeightedMode @weightedMode
        {
            get => Tiny.GetProperty<TinyEditorExtensions.TinyWeightedMode>(nameof(@weightedMode));
            set => Tiny.AssignIfDifferent(nameof(@weightedMode), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyKeyFrame other)
        {
            @inTangent = other.@inTangent;
            @inWeight = other.@inWeight;
            @outTangent = other.@outTangent;
            @outWeight = other.@outWeight;
            @time = other.@time;
            @value = other.@value;
            @weightedMode = other.@weightedMode;
        }
    }
}
