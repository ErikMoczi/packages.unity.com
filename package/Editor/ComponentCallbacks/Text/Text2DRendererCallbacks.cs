
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyComponentCallback(
        CoreGuids.Text.Text2DRenderer)]
    [UsedImplicitly]
    internal class Text2DRendererCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject text2DRenderer)
        {
            text2DRenderer.AssignPropertyFrom("style", entity.Ref);
        }

        protected override void OnValidateComponent(TinyEntity entity, TinyObject text2DRenderer)
        {
            text2DRenderer.AssignPropertyFrom("style", entity.Ref);
        }
    }
}

