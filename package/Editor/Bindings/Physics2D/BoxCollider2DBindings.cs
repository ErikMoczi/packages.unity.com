using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Physics2D.BoxCollider2D,
        CoreGuids.Core2D.TransformNode)]
    [UsedImplicitly]
    internal class RectColliderBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<BoxCollider2D>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<BoxCollider2D>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var tinyRectCollider = entity.GetComponent<Runtime.Physics2D.TinyBoxCollider2D>();
            var collider = GetComponent<BoxCollider2D>(entity);

            var pivot = tinyRectCollider.pivot;
            var size = tinyRectCollider.size;
            collider.size = size;
            collider.offset = new Vector2(-(pivot.x - 0.5f) * size.x, -(pivot.y - 0.5f) * size.y);
        }
    }
}
