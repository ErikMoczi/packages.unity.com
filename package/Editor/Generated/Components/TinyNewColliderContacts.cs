// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinyNewColliderContacts : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyNewColliderContacts>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyNewColliderContacts Construct(TinyObject tiny) => new TinyNewColliderContacts(tiny);
        private static TinyId s_Id = CoreIds.Physics2D.NewColliderContacts;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.NewColliderContacts;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyNewColliderContacts(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyNewColliderContacts(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TinyList @contacts
        {
            get => Tiny[nameof(@contacts)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinyNewColliderContacts other)
        {
            CopyList(@contacts, other.@contacts);
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
