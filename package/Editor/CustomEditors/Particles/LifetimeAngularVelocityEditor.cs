
using JetBrains.Annotations;
    
namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Particles.LifetimeAngularVelocity)]
    [UsedImplicitly]
    internal class LifetimeAngularVelocityEditor : ParticleComponentEditor
    {
        public LifetimeAngularVelocityEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            ShowParticleEmitterWarning(ref context);
            return base.Visit(ref context);
        }
    }
}

