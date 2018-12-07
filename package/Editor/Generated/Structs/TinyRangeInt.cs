// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Math
{
    internal partial struct TinyRangeInt : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Math.RangeInt;
        private static TinyType.Reference s_Ref = TypeRefs.Math.RangeInt;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyRangeInt(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyRangeInt(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public int @start
        {
            get => Tiny.GetProperty<int>(nameof(@start));
            set => Tiny.AssignIfDifferent(nameof(@start), value);
        }

        public int @end
        {
            get => Tiny.GetProperty<int>(nameof(@end));
            set => Tiny.AssignIfDifferent(nameof(@end), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyRangeInt other)
        {
            @start = other.@start;
            @end = other.@end;
        }
    }
}
