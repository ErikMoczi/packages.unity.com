using UnityEditor;

namespace Unity.Tiny
{
    internal class ParticleComponentEditor : ComponentEditor
    {
        protected ParticleComponentEditor(TinyContext context) : base(context)
        {
        }

        protected void ShowParticleEmitterWarning(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();
            var particleEmitterTypeRef = TypeRefs.Particles.ParticleEmitter;

            if (!target.HasComponent(particleEmitterTypeRef))
            {
                EditorGUILayout.HelpBox("A ParticleEmitter component is needed with the EmitterConeSource.", MessageType.Warning);
                AddComponentToTargetButton(context, particleEmitterTypeRef);
            }
        }
    }
}