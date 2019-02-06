

using JetBrains.Annotations;

using UnityEngine;

using Unity.Tiny.Runtime.UILayout;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Core2D.TransformNode,
        CoreGuids.UILayout.RectTransform)]
    [BindingDependency(
        typeof(TransformBinding),
        typeof(TransformPositionBinding))]
    [UsedImplicitly]
    internal class RectTransformBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<RectTransform>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            // The best thing to do here would be to remove the RectTransform component entirely. However, if you remove
            // it, undo and then redo, Unity will most likely crash, due to a fence operation in native code.
            //RemoveComponent<RectTransform>(entity);
            var rectTransform = GetComponent<RectTransform>(entity);
            if (null == rectTransform)
            {
                return;
            }
            rectTransform.pivot = rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.zero;
        }

        public override void Transfer(TinyEntity entity)
        {
            var rectTransform = GetComponent<RectTransform>(entity);
            var tinyRectTransform = entity.GetComponent<TinyRectTransform>();
            if (tinyRectTransform.IsValid)
            {
                rectTransform.anchoredPosition = tinyRectTransform.anchoredPosition;
                rectTransform.anchorMin = tinyRectTransform.anchorMin;
                rectTransform.anchorMax = tinyRectTransform.anchorMax;
                rectTransform.sizeDelta = tinyRectTransform.sizeDelta;
                rectTransform.pivot = tinyRectTransform.pivot;
            }
        }
    }
}

