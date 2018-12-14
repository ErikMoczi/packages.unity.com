

using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyComponentCallback(CoreGuids.HitBox2D.RectHitBox2D)]
    [UsedImplicitly]
    internal class RectHitBox2DCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject component)
        {
            var spriteRenderer = entity.GetComponent(TypeRefs.Core2D.Sprite2DRendererOptions);
            var sprite = spriteRenderer?.GetProperty<Sprite>("sprite");

            if (null == sprite || !sprite)
            {
                return;
            }

            var rect = new Rect
            {
                min = sprite.bounds.min,
                max = sprite.bounds.max
            };
            
            component.AssignPropertyFrom("box", rect);
        }
    }
}

