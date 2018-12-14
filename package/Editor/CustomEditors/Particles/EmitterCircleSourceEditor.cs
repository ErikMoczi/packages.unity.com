using JetBrains.Annotations;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Particles.EmitterCircleSource)]
    [UsedImplicitly]
    internal class EmitterCircleSourceEditor : ParticleComponentEditor
    {
        public EmitterCircleSourceEditor(TinyContext context) : base(context)
        {
        }
        
        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }

    }
}