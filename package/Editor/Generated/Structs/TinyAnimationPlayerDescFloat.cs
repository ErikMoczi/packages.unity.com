// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Animation
{
    internal partial struct TinyAnimationPlayerDescFloat : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Animation.AnimationPlayerDescFloat;
        private static TinyType.Reference s_Ref = TypeRefs.Animation.AnimationPlayerDescFloat;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAnimationPlayerDescFloat(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyAnimationPlayerDescFloat(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Animation.TinyAnimationPlayerDesc @desc
        {
            get => new Animation.TinyAnimationPlayerDesc(Tiny[nameof(@desc)] as TinyObject);
            set => new Animation.TinyAnimationPlayerDesc(Tiny[nameof(@desc)] as TinyObject).CopyFrom(value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAnimationPlayerDescFloat other)
        {
            @desc = other.@desc;
        }
    }
}
