

using JetBrains.Annotations;

using UnityEngine;

using Unity.Tiny.Runtime.Core2D;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.HitBox2D.Sprite2DRendererHitBox2D,
        CoreGuids.Core2D.Sprite2DRenderer,
        CoreGuids.Core2D.TransformNode)]
    [BindingDependency(
        typeof(Sprite2DRendererBinding))]
    [UsedImplicitly]
    internal class Sprite2DRendererHitBox2DBinding : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<Sprite2DRendererHitBox2D>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<Sprite2DRendererHitBox2D>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var spriteRenderer = entity.GetComponent<TinySprite2DRenderer>();
            var sprite = spriteRenderer.sprite;

            var behaviour = GetComponent<Sprite2DRendererHitBox2D>(entity);
            behaviour.Sprite = sprite;
        }
    }
}

