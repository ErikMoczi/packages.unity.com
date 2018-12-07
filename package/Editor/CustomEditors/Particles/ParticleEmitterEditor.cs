
using JetBrains.Annotations;
using UnityEditor;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Particles.ParticleEmitter)]
    [UsedImplicitly]
    internal class ParticleEmitterEditor : ComponentEditor
    {
        public ParticleEmitterEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            try
            {
                return base.Visit(ref context);
            }
            finally
            {
                var target = context.MainTarget<TinyEntity>();

                if (!target.HasComponent(TypeRefs.Particles.EmitterBoxSource) &&
                    !target.HasComponent(TypeRefs.Particles.EmitterCircleSource) &&
                    !target.HasComponent(TypeRefs.Particles.EmitterConeSource))
                {
                    EditorGUILayout.HelpBox("An emission source is needed for preview and runtime emission.", MessageType.Warning);
                    AddComponentToTargetButton(context, TypeRefs.Particles.EmitterBoxSource);
                    AddComponentToTargetButton(context, TypeRefs.Particles.EmitterCircleSource);
                    AddComponentToTargetButton(context, TypeRefs.Particles.EmitterConeSource);
                }
            }
        }
    }
}

