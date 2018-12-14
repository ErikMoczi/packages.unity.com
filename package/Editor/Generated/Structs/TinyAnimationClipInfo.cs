// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Animation
{
    internal partial struct TinyAnimationClipInfo : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Animation.AnimationClipInfo;
        private static TinyType.Reference s_Ref = TypeRefs.Animation.AnimationClipInfo;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAnimationClipInfo(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyAnimationClipInfo(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @startTime
        {
            get => Tiny.GetProperty<float>(nameof(@startTime));
            set => Tiny.AssignIfDifferent(nameof(@startTime), value);
        }

        public float @endTime
        {
            get => Tiny.GetProperty<float>(nameof(@endTime));
            set => Tiny.AssignIfDifferent(nameof(@endTime), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAnimationClipInfo other)
        {
            @startTime = other.@startTime;
            @endTime = other.@endTime;
        }
    }
}
