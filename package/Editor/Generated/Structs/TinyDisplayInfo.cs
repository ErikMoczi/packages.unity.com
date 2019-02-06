// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyDisplayInfo : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Core2D.DisplayInfo;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.DisplayInfo;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyDisplayInfo(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyDisplayInfo(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public int @width
        {
            get => Tiny.GetProperty<int>(nameof(@width));
            set => Tiny.AssignIfDifferent(nameof(@width), value);
        }

        public int @height
        {
            get => Tiny.GetProperty<int>(nameof(@height));
            set => Tiny.AssignIfDifferent(nameof(@height), value);
        }

        public bool @autoSizeToFrame
        {
            get => Tiny.GetProperty<bool>(nameof(@autoSizeToFrame));
            set => Tiny.AssignIfDifferent(nameof(@autoSizeToFrame), value);
        }

        public Unity.Tiny.RenderingMode @renderMode
        {
            get => Tiny.GetProperty<Unity.Tiny.RenderingMode>(nameof(@renderMode));
            set => Tiny.AssignIfDifferent(nameof(@renderMode), value);
        }

        public bool @focused
        {
            get => Tiny.GetProperty<bool>(nameof(@focused));
            set => Tiny.AssignIfDifferent(nameof(@focused), value);
        }

        public bool @visible
        {
            get => Tiny.GetProperty<bool>(nameof(@visible));
            set => Tiny.AssignIfDifferent(nameof(@visible), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyDisplayInfo other)
        {
            @width = other.@width;
            @height = other.@height;
            @autoSizeToFrame = other.@autoSizeToFrame;
            @renderMode = other.@renderMode;
            @focused = other.@focused;
            @visible = other.@visible;
        }
    }
}
