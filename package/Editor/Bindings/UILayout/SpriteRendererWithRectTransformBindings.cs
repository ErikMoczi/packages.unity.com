

using JetBrains.Annotations;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

using Unity.Tiny.Runtime.Core2D;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.TransformNode,
        CoreGuids.UILayout.RectTransform,
        CoreGuids.Core2D.Sprite2DRenderer)]
    [OptionalComponent(
        CoreGuids.Core2D.Sprite2DRendererOptions)]
    [BindingDependency(
        typeof(RectTransformBindings))]
    [UsedImplicitly]
    internal class SpriteRendererWithRectTransformBindings : BindingProfile
    {
        private static readonly Dictionary<TinyBlendOp, Vector2> k_BlendModes = new Dictionary<TinyBlendOp, Vector2>
        {
            { TinyBlendOp.Alpha, new Vector2 ((float)BlendMode.SrcAlpha, (float) BlendMode.OneMinusSrcAlpha) }, // alpha
            { TinyBlendOp.Add, new Vector2 ((float)BlendMode.SrcAlpha, (float) BlendMode.One) },              // add
            { TinyBlendOp.Multiply, new Vector2 ((float)BlendMode.DstColor, (float) BlendMode.OneMinusSrcAlpha) }  // multiply
        };

        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<CanvasRenderer>(entity);
            AddMissingComponent<Image>(entity, image =>
            {
                var mat = image.material = new Material(Shader.Find("Tiny/Sprite2D"));
                mat.renderQueue = 3000;
            });
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<Image>(entity);
            RemoveComponent<CanvasRenderer>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var image = GetComponent<Image>(entity);

            var tinySprite = entity.GetComponent<TinySprite2DRenderer>();

            var sprite = tinySprite.sprite;
            image.sprite = sprite;

            var color  = tinySprite.color;
            image.color = color;

            var rt = GetComponent<RectTransform>(entity);
            if (null != rt && rt)
            {
                var tinyOptions = entity.GetComponent(CoreIds.Core2D.Sprite2DRendererOptions);
                if (null != tinyOptions)
                {
                    tinyOptions.AssignIfDifferent("size", rt.rect.size);
                    var drawMode = tinyOptions.GetProperty<DrawMode>("drawMode");
                    switch (drawMode)
                    {
                        case DrawMode.Stretch:
                        {
                            image.type = Image.Type.Sliced;
                            break;
                        }
                        case DrawMode.AdaptiveTiling:
                        case DrawMode.ContinuousTiling:
                        {
                            image.type = Image.Type.Tiled;
                            break;
                        }
                    }
                }
            }

            if (k_BlendModes.TryGetValue(tinySprite.blending, out var blendMode))
            {
                var mat = image.material;
                mat.SetFloat("_SrcMode", blendMode.x);
                mat.SetFloat("_DstMode", blendMode.y);
                mat.SetColor("_Color", image.color);
            }
            else
            {
                Debug.Log($"{TinyConstants.ApplicationName}: Unknown blending mode, of value '{tinySprite.blending}'");
            }
        }
    }
}

