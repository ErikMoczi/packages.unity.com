// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Audio
{
    internal partial struct TinyAudioConfig : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Audio.AudioConfig;
        private static TinyType.Reference s_Ref = TypeRefs.Audio.AudioConfig;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAudioConfig(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyAudioConfig(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public bool @paused
        {
            get => Tiny.GetProperty<bool>(nameof(@paused));
            set => Tiny.AssignIfDifferent(nameof(@paused), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAudioConfig other)
        {
            @paused = other.@paused;
        }
    }
}
