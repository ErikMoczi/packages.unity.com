
using JetBrains.Annotations;

namespace Unity.Tiny 
{
    [TinyCustomEditor(CoreGuids.Particles.EmitterInitialColor)]
    [UsedImplicitly]
    internal class EmitterInitialColorEditor : ParticleComponentEditor
    {
        public EmitterInitialColorEditor(TinyContext context)
            : base(context) { }


        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }
    }
}


