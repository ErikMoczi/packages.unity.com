using JetBrains.Annotations;
using Unity.Tiny.Runtime.Particles;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Particles.EmitterBoxSource,
        CoreGuids.Particles.ParticleEmitter)]
    [BindingDependency(
        typeof(ParticleEmitterBindings))]
    [UsedImplicitly]
    internal class EmitterBoxSourceBindings : BindingProfile
    {
        public override void Transfer(TinyEntity entity)
        {
            var emitterBoxSource = entity.GetComponent<TinyEmitterBoxSource>();
            var particleSystem = GetComponent<ParticleSystem>(entity);

            // If this shape component is present on the entity, it takes precedence over the others.
            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.enabled = true;
            
            //Reoriented the box so it fits potential directions given by particle effects
            //this also means switching scaling
            var boxRect = emitterBoxSource.rect;
            var boxScale = shape.scale;
            boxScale.x = 0.0f;
            boxScale.y = boxRect.height;
            boxScale.z = boxRect.width;
            
            shape.scale = boxScale;

            var boxPosition = shape.position;
            boxPosition.x = boxRect.x;
            boxPosition.y = boxRect.y;
            shape.position = boxPosition;

            var boxRotation = shape.rotation;
            boxRotation.y = 90;
            shape.rotation = boxRotation;

            var emission = particleSystem.emission;
            emission.enabled = boxRect.width != 0 || boxRect.height != 0;
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            var particleSystem = GetComponent<ParticleSystem>(entity);
            var shape = particleSystem.shape;
            shape.enabled = false;
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;
            shape.scale = Vector3.one;
            var emission = particleSystem.emission;
            emission.enabled = false;
        }
    }
}
