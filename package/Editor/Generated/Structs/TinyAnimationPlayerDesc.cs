// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Animation
{
    internal partial struct TinyAnimationPlayerDesc : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Animation.AnimationPlayerDesc;
        private static TinyType.Reference s_Ref = TypeRefs.Animation.AnimationPlayerDesc;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAnimationPlayerDesc(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyAnimationPlayerDesc(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public uint @propertyOffset
        {
            get => Tiny.GetProperty<uint>(nameof(@propertyOffset));
            set => Tiny.AssignIfDifferent(nameof(@propertyOffset), value);
        }

        public Unity.Tiny.TinyEntity.Reference @curve
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@curve));
            set => Tiny.AssignIfDifferent(nameof(@curve), value);
        }

        public string @entityPath
        {
            get => Tiny.GetProperty<string>(nameof(@entityPath));
            set => Tiny.AssignIfDifferent(nameof(@entityPath), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAnimationPlayerDesc other)
        {
            @propertyOffset = other.@propertyOffset;
            @curve = other.@curve;
            @entityPath = other.@entityPath;
        }
    }
}
