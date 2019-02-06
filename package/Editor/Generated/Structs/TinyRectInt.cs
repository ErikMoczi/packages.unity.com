// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Math
{
    internal partial struct TinyRectInt : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Math.RectInt;
        private static TinyType.Reference s_Ref = TypeRefs.Math.RectInt;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyRectInt(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyRectInt(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public int @x
        {
            get => Tiny.GetProperty<int>(nameof(@x));
            set => Tiny.AssignIfDifferent(nameof(@x), value);
        }

        public int @y
        {
            get => Tiny.GetProperty<int>(nameof(@y));
            set => Tiny.AssignIfDifferent(nameof(@y), value);
        }

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

        #endregion // Properties

        public void CopyFrom(TinyRectInt other)
        {
            @x = other.@x;
            @y = other.@y;
            @width = other.@width;
            @height = other.@height;
        }
    }
}
