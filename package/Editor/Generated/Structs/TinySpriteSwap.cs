// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UIControlsExtensions
{
    internal partial struct TinySpriteSwap : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.UIControlsExtensions.SpriteSwap;
        private static TinyType.Reference s_Ref = TypeRefs.UIControlsExtensions.SpriteSwap;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySpriteSwap(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinySpriteSwap(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Sprite @normal
        {
            get => Tiny.GetProperty<UnityEngine.Sprite>(nameof(@normal));
            set => Tiny.AssignIfDifferent(nameof(@normal), value);
        }

        public UnityEngine.Sprite @hover
        {
            get => Tiny.GetProperty<UnityEngine.Sprite>(nameof(@hover));
            set => Tiny.AssignIfDifferent(nameof(@hover), value);
        }

        public UnityEngine.Sprite @pressed
        {
            get => Tiny.GetProperty<UnityEngine.Sprite>(nameof(@pressed));
            set => Tiny.AssignIfDifferent(nameof(@pressed), value);
        }

        public UnityEngine.Sprite @disabled
        {
            get => Tiny.GetProperty<UnityEngine.Sprite>(nameof(@disabled));
            set => Tiny.AssignIfDifferent(nameof(@disabled), value);
        }

        #endregion // Properties

        public void CopyFrom(TinySpriteSwap other)
        {
            @normal = other.@normal;
            @hover = other.@hover;
            @pressed = other.@pressed;
            @disabled = other.@disabled;
        }
    }
}
