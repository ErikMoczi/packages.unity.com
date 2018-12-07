using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Physics2D.CircleCollider2D,
        CoreGuids.Core2D.TransformNode)]
    [UsedImplicitly]
    internal class CircleColliderBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<CircleCollider2D>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<CircleCollider2D>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var tinyCircleCollider = entity.GetComponent<Runtime.Physics2D.TinyCircleCollider2D>();
            var collider = GetComponent<CircleCollider2D>(entity);

            var pivot = tinyCircleCollider.pivot;
            var radius = tinyCircleCollider.radius;
            collider.radius = radius;
            collider.offset = new Vector2(-(pivot.x - 0.5f) * radius, -(pivot.y - 0.5f) * radius);
        }
    }
}
