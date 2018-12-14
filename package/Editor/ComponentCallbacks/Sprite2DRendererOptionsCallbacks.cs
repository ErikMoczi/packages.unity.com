using JetBrains.Annotations;
using UnityEngine;

using Unity.Tiny.Runtime.Core2D;

namespace Unity.Tiny
{
    internal enum DrawMode
    {
        ContinuousTiling,
        AdaptiveTiling,
        Stretch
    }

    [TinyComponentCallback(CoreGuids.Core2D.Sprite2DRendererOptions)]
    [UsedImplicitly]
    internal class Sprite2DRendererOptionsCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject component)
        {
            var options = entity.GetComponent<TinySprite2DRendererOptions>();
            
            var sprite2DRenderer = entity.GetComponent<TinySprite2DRenderer>();
            if (sprite2DRenderer.IsValid)
            {
                var sprite = sprite2DRenderer.sprite;
                options.size = null != sprite ? (sprite.rect.size / sprite.pixelsPerUnit): Vector2.one;
            }

            options.drawMode = DrawMode.Stretch;
        }
        
        protected override void OnRemoveComponent(TinyEntity entity, TinyObject component)
        {
            var renderer = GetComponent<SpriteRenderer>(entity);
            if (null != renderer && renderer)
            {
                renderer.drawMode = SpriteDrawMode.Simple;
            }
        }

        private void SetDrawMode(SpriteRenderer renderer, DrawMode mode)
        {
            switch (mode)
            {
                case DrawMode.ContinuousTiling:
                    renderer.drawMode = SpriteDrawMode.Tiled;
                    renderer.tileMode = SpriteTileMode.Continuous;
                    break;
                case DrawMode.AdaptiveTiling:
                    renderer.drawMode = SpriteDrawMode.Tiled;
                    renderer.tileMode = SpriteTileMode.Adaptive;
                    break;
                case DrawMode.Stretch:
                    renderer.drawMode = SpriteDrawMode.Sliced;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(mode));
            }
        }
    }
}

