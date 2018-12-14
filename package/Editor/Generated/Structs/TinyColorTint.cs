// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UIControlsExtensions
{
    internal partial struct TinyColorTint : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.UIControlsExtensions.ColorTint;
        private static TinyType.Reference s_Ref = TypeRefs.UIControlsExtensions.ColorTint;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyColorTint(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyColorTint(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Color @normal
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@normal));
            set => Tiny.AssignIfDifferent(nameof(@normal), value);
        }

        public UnityEngine.Color @hover
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@hover));
            set => Tiny.AssignIfDifferent(nameof(@hover), value);
        }

        public UnityEngine.Color @pressed
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@pressed));
            set => Tiny.AssignIfDifferent(nameof(@pressed), value);
        }

        public UnityEngine.Color @disabled
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@disabled));
            set => Tiny.AssignIfDifferent(nameof(@disabled), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyColorTint other)
        {
            @normal = other.@normal;
            @hover = other.@hover;
            @pressed = other.@pressed;
            @disabled = other.@disabled;
        }
    }
}
