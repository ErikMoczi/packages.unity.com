using JetBrains.Annotations;

namespace Unity.Tiny
{
    [TinyRequiredComponent(CoreGuids.Particles.ParticleEmitter)]
    [UsedImplicitly]
    internal class ParticleEmitterRequirements : TinyComponentRequirement
    {
        public override void AddRequiredComponents(TinyEntity entity)
        {
            if (!HasComponents(entity, TypeRefs.Core2D.TransformNode))
            {
                AddComponentAtIndex(entity, TypeRefs.Core2D.TransformNode, 0);
            }
            
            if (!HasComponents(entity, TypeRefs.Core2D.TransformLocalPosition))
            {
                AddComponentAtIndex(entity, TypeRefs.Core2D.TransformLocalPosition, 0);
            }
        }
    }
}