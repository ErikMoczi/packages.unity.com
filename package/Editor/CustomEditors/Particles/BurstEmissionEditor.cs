
using JetBrains.Annotations;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.Particles.BurstEmission)]
    [UsedImplicitly]
    internal class BurstEmissionEditor : ParticleComponentEditor
    {
        public BurstEmissionEditor(TinyContext context)
            : base(context) { }


        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }
    }
}
