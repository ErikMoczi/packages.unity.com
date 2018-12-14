// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Text
{
    internal partial struct TinyCharacterInfo : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Text.CharacterInfo;
        private static TinyType.Reference s_Ref = TypeRefs.Text.CharacterInfo;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCharacterInfo(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyCharacterInfo(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public uint @value
        {
            get => Tiny.GetProperty<uint>(nameof(@value));
            set => Tiny.AssignIfDifferent(nameof(@value), value);
        }

        public float @advance
        {
            get => Tiny.GetProperty<float>(nameof(@advance));
            set => Tiny.AssignIfDifferent(nameof(@advance), value);
        }

        public float @bearingX
        {
            get => Tiny.GetProperty<float>(nameof(@bearingX));
            set => Tiny.AssignIfDifferent(nameof(@bearingX), value);
        }

        public float @bearingY
        {
            get => Tiny.GetProperty<float>(nameof(@bearingY));
            set => Tiny.AssignIfDifferent(nameof(@bearingY), value);
        }

        public float @width
        {
            get => Tiny.GetProperty<float>(nameof(@width));
            set => Tiny.AssignIfDifferent(nameof(@width), value);
        }

        public float @height
        {
            get => Tiny.GetProperty<float>(nameof(@height));
            set => Tiny.AssignIfDifferent(nameof(@height), value);
        }

        public UnityEngine.Rect @characterRegion
        {
            get => Tiny.GetProperty<UnityEngine.Rect>(nameof(@characterRegion));
            set => Tiny.AssignIfDifferent(nameof(@characterRegion), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyCharacterInfo other)
        {
            @value = other.@value;
            @advance = other.@advance;
            @bearingX = other.@bearingX;
            @bearingY = other.@bearingY;
            @width = other.@width;
            @height = other.@height;
            @characterRegion = other.@characterRegion;
        }
    }
}
