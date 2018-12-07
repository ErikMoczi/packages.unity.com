using Unity.Tiny.Runtime.Core2D;
using Unity.Tiny.Runtime.UIControls;
using Unity.Tiny.Runtime.UIControlsExtensions;
using UnityEngine.UI;

namespace Unity.Tiny
{
    internal static class UIControlsBindingsHelper
    {
        public static void TransferTransition(TinyEntity entity, TinySprite2DRenderer renderer, TinyTransitionEntity transition, Image image)
        {
            if (entity.HasComponent<TinyInactiveUIControl>())
            {
                if (transition.type == TinyTransitionType.Sprite)
                {
                    image.sprite = transition.spriteSwap.disabled;
                }
                else
                {
                    image.color = renderer.color * transition.colorTint.disabled;
                    image.material.SetColor("_Color", image.color);
                }
            }
            else
            {
                if (transition.type == TinyTransitionType.Sprite)
                {
                    image.sprite = transition.spriteSwap.normal;
                }
                else
                {
                    image.color = renderer.color * transition.colorTint.normal;
                    image.material.SetColor("_Color", image.color);
                }
            }
        }
    }
}