// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UIControlsExtensions
{
    internal partial struct TinyTransitionEntity : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.UIControlsExtensions.TransitionEntity;
        private static TinyType.Reference s_Ref = TypeRefs.UIControlsExtensions.TransitionEntity;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTransitionEntity(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyTransitionEntity(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UIControlsExtensions.TinyTransitionType @type
        {
            get => Tiny.GetProperty<UIControlsExtensions.TinyTransitionType>(nameof(@type));
            set => Tiny.AssignIfDifferent(nameof(@type), value);
        }

        public UIControlsExtensions.TinyColorTint @colorTint
        {
            get => new UIControlsExtensions.TinyColorTint(Tiny[nameof(@colorTint)] as TinyObject);
            set => new UIControlsExtensions.TinyColorTint(Tiny[nameof(@colorTint)] as TinyObject).CopyFrom(value);
        }

        public UIControlsExtensions.TinySpriteSwap @spriteSwap
        {
            get => new UIControlsExtensions.TinySpriteSwap(Tiny[nameof(@spriteSwap)] as TinyObject);
            set => new UIControlsExtensions.TinySpriteSwap(Tiny[nameof(@spriteSwap)] as TinyObject).CopyFrom(value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTransitionEntity other)
        {
            @type = other.@type;
            @colorTint = other.@colorTint;
            @spriteSwap = other.@spriteSwap;
        }
    }
}
