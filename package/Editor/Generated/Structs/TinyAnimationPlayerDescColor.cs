// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Animation
{
    internal partial struct TinyAnimationPlayerDescColor : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Animation.AnimationPlayerDescColor;
        private static TinyType.Reference s_Ref = TypeRefs.Animation.AnimationPlayerDescColor;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAnimationPlayerDescColor(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyAnimationPlayerDescColor(IRegistry registry) : this(new TinyObject(registry, s_Ref))
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

        public void CopyFrom(TinyAnimationPlayerDescColor other)
        {
            @desc = other.@desc;
        }
    }
}
