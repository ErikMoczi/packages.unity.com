

using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyComponentCallback(CoreGuids.Core2D.LayerSorting)]
    [UsedImplicitly]
    internal class LayerSortingCallbacks : ComponentCallback
    {
        protected override void OnRemoveComponent(TinyEntity entity, TinyObject component)
        {
            var renderer = GetComponent<Renderer>(entity);
            if (renderer && null != renderer)
            {
                renderer.sortingLayerID = 0;
                renderer.sortingOrder = 0;
            }

            var canvas = GetComponent<Canvas>(entity);
            if (canvas && null != canvas)
            {
                canvas.overrideSorting = false;
                canvas.sortingLayerID = 0;
                canvas.sortingOrder = 0;
            }
        }
    }
}

