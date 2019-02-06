

using JetBrains.Annotations;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Tiny.Runtime.Core2D;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.TransformNode,
        CoreGuids.Core2D.Sprite2DRenderer
        )]
    [WithoutComponent(
        CoreGuids.UILayout.RectTransform)]
    [OptionalComponent(
        CoreGuids.Core2D.Sprite2DRendererOptions)]
    [BindingDependency(
        typeof(TransformBinding))]
    [UsedImplicitly]
    internal class Sprite2DRendererBinding : BindingProfile
    {
        private static readonly Dictionary<TinyBlendOp, Vector2> k_BlendModes = new Dictionary<TinyBlendOp, Vector2>
        {
            { TinyBlendOp.Alpha, new Vector2 ((float)BlendMode.SrcAlpha, (float) BlendMode.OneMinusSrcAlpha) }, // alpha
            { TinyBlendOp.Add, new Vector2 ((float)BlendMode.SrcAlpha, (float) BlendMode.One) },              // add
            { TinyBlendOp.Multiply, new Vector2 ((float)BlendMode.DstColor, (float) BlendMode.OneMinusSrcAlpha) }  // multiply
        };

        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<SpriteRenderer>(entity, r =>
            {
                var mat = r.sharedMaterial = new Material(Shader.Find("Tiny/Sprite2D"));
                mat.renderQueue = 3000;
            });
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<SpriteRenderer>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            try
            {
                var sprite2DRenderer = entity.GetComponent<TinySprite2DRenderer>();
                var sprite = sprite2DRenderer.sprite;
                var spriteRenderer = GetComponent<SpriteRenderer>(entity);

                var block = new MaterialPropertyBlock();
                spriteRenderer.GetPropertyBlock(block);
                block.Clear();

                block.SetColor("_Color", sprite2DRenderer.color);

                if (sprite)
                {
                    spriteRenderer.sprite = sprite;
                    var blending = sprite2DRenderer.blending;
                    if (k_BlendModes.TryGetValue(blending, out var blendMode))
                    {
                        spriteRenderer.sharedMaterial.SetFloat("_SrcMode", blendMode.x);
                        spriteRenderer.sharedMaterial.SetFloat("_DstMode", blendMode.y);
                    }
                    else
                    {
                        Debug.Log($"{TinyConstants.ApplicationName}: Unknown blending mode, of value '{blending}'");
                    }

                    block.SetTexture("_MainTex", sprite.texture);
                }
                else
                {
                    spriteRenderer.sprite = TinySprites.WhiteSprite;
                    if (!entity.HasComponent<TinySprite2DRendererOptions>())
                    {
                        spriteRenderer.size = Vector2.one;
                        
                    }
                }

                spriteRenderer.SetPropertyBlock(block);

                var options = entity.GetComponent<TinySprite2DRendererOptions>();
                if (options.IsValid)
                {
                    SetDrawMode(spriteRenderer, options.drawMode);
                    spriteRenderer.size = options.size;
                }
                else
                {
                    spriteRenderer.drawMode = SpriteDrawMode.Simple;
                }
            }
            finally
            {
                UnityEditor.SceneView.RepaintAll();
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

