// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinyColliderContacts : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyColliderContacts>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyColliderContacts Construct(TinyObject tiny) => new TinyColliderContacts(tiny);
        private static TinyId s_Id = CoreIds.Physics2D.ColliderContacts;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.ColliderContacts;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyColliderContacts(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyColliderContacts(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TinyList @contacts
        {
            get => Tiny[nameof(@contacts)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinyColliderContacts other)
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
