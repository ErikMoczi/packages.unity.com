using JetBrains.Annotations;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.Particles.EmitterInitialVelocity)]
    [UsedImplicitly]
    internal class EmitterInitialVelocityEditor : ParticleComponentEditor
    {
        public EmitterInitialVelocityEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }
    }
}

