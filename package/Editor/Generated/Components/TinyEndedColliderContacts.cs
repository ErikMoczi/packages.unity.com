// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinyEndedColliderContacts : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEndedColliderContacts>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEndedColliderContacts Construct(TinyObject tiny) => new TinyEndedColliderContacts(tiny);
        private static TinyId s_Id = CoreIds.Physics2D.EndedColliderContacts;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.EndedColliderContacts;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEndedColliderContacts(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEndedColliderContacts(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TinyList @contacts
        {
            get => Tiny[nameof(@contacts)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinyEndedColliderContacts other)
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
