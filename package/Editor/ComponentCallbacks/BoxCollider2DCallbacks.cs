using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyComponentCallback(CoreGuids.Physics2D.BoxCollider2D)]
    [UsedImplicitly]
    internal class BoxCollider2DCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject component)
        {
            var spriteRenderer = entity.GetComponent(TypeRefs.Core2D.Sprite2DRenderer);
            var sprite = spriteRenderer?.GetProperty<Sprite>("sprite");

            if (null == sprite || !sprite)
            {
                return;
            }

            component.AssignPropertyFrom("size", new Vector2(sprite.bounds.size.x, sprite.bounds.size.y));
        }
    }
}
