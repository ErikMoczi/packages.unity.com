// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Watchers
{
    internal partial struct TinyEntityMask : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Watchers.EntityMask;
        private static TinyType.Reference s_Ref = TypeRefs.Watchers.EntityMask;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEntityMask(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyEntityMask(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TinyList @all
        {
            get => Tiny[nameof(@all)] as TinyList;
        }

        public TinyList @any
        {
            get => Tiny[nameof(@any)] as TinyList;
        }

        public TinyList @sub
        {
            get => Tiny[nameof(@sub)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinyEntityMask other)
        {
            CopyList(@all, other.@all);
            CopyList(@any, other.@any);
            CopyList(@sub, other.@sub);
        }
        private void CopyList(TinyList lhs, TinyList rhs)
        {
            lhs.Clear();
            foreach (var item in rhs)
            {
                lhs.Add(item);
            }
        }
    }
}
