// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.PlayableAd
{
    internal partial struct TinyOrientationProperties : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.PlayableAd.OrientationProperties;
        private static TinyType.Reference s_Ref = TypeRefs.PlayableAd.OrientationProperties;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyOrientationProperties(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyOrientationProperties(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public bool @allowOrientationChange
        {
            get => Tiny.GetProperty<bool>(nameof(@allowOrientationChange));
            set => Tiny.AssignIfDifferent(nameof(@allowOrientationChange), value);
        }

        public string @forceOrientation
        {
            get => Tiny.GetProperty<string>(nameof(@forceOrientation));
            set => Tiny.AssignIfDifferent(nameof(@forceOrientation), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyOrientationProperties other)
        {
            @allowOrientationChange = other.@allowOrientationChange;
            @forceOrientation = other.@forceOrientation;
        }
    }
}
