// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Animation
{
    internal partial struct TinyAnimationPlayerDescQuaternion : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Animation.AnimationPlayerDescQuaternion;
        private static TinyType.Reference s_Ref = TypeRefs.Animation.AnimationPlayerDescQuaternion;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAnimationPlayerDescQuaternion(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyAnimationPlayerDescQuaternion(IRegistry registry) : this(new TinyObject(registry, s_Ref))
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

        public void CopyFrom(TinyAnimationPlayerDescQuaternion other)
        {
            @desc = other.@desc;
        }
    }
}
