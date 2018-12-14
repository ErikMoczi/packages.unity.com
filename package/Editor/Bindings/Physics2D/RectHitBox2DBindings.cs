

using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.HitBox2D.RectHitBox2D,
        CoreGuids.Core2D.TransformNode)]
    [UsedImplicitly]
    internal class RectHitBox2DBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<RectHitBox2D>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<RectHitBox2D>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var hitbox = entity.GetComponent<Runtime.HitBox2D.TinyRectHitBox2D>();
            GetComponent<RectHitBox2D>(entity).Box = hitbox.box;
        }
    }
}

