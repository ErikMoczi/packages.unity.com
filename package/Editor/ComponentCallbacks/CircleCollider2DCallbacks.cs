using JetBrains.Annotations;
using System;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyComponentCallback(CoreGuids.Physics2D.CircleCollider2D)]
    [UsedImplicitly]
    internal class CircleCollider2DCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject component)
        {
            var spriteRenderer = entity.GetComponent(TypeRefs.Core2D.Sprite2DRenderer);
            var sprite = spriteRenderer?.GetProperty<Sprite>("sprite");

            if (null == sprite || !sprite)
            {
                return;
            }

            component.AssignPropertyFrom("radius", Math.Max(sprite.bounds.size.x, sprite.bounds.size.y) / 2f);
        }
    }
}
