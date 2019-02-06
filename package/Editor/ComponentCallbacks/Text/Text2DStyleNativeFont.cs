
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyComponentCallback(
        CoreGuids.Text.Text2DStyleNativeFont)]
    [UsedImplicitly]
    internal class Text2DStyleNativeFontCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject text2DRenderer)
        {
            text2DRenderer.AssignPropertyFrom("font", entity.Ref);
        }

        protected override void OnValidateComponent(TinyEntity entity, TinyObject text2DRenderer)
        {
            text2DRenderer.AssignPropertyFrom("font", entity.Ref);
        }
    }
}

