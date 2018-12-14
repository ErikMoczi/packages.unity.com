using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class AudioSourceInvertedBindings : InvertedBindingsBase<AudioSource>
    {
        #region Static
        [TinyInitializeOnLoad]
        [UsedImplicitly]
        private static void Register()
        {
            GameObjectTracker.RegisterForComponentModification<AudioSource>(SyncAudioSource);
        }

        private static void SyncAudioSource(AudioSource from, TinyEntityView view)
        {
            var registry = view.Registry;
            var entity = view.EntityRef.Dereference(registry);
            var audioSource = entity.GetComponent<Runtime.Audio.TinyAudioSource>();
            if (audioSource.IsValid)
            {
                SyncAudioSource(from, audioSource);
            }
        }

        private static void SyncAudioSource(AudioSource audioSource, [NotNull] Runtime.Audio.TinyAudioSource component)
        {
            component.clip = audioSource.clip;
        }
        #endregion

        #region InvertedBindingsBase<AudioSource>
        public override void Create(TinyEntityView view, AudioSource from)
        {
            var tinyAudioSource = new Runtime.Audio.TinyAudioSource(view.Registry);
            SyncAudioSource(from, tinyAudioSource);

            var entity = view.EntityRef.Dereference(view.Registry);
            var audioSource = entity.GetOrAddComponent<Runtime.Audio.TinyAudioSource>();
            audioSource.CopyFrom(tinyAudioSource);
        }

        public override TinyType.Reference GetMainTinyType()
        {
            return TypeRefs.Audio.AudioSource;
        }
        #endregion
    }
}
