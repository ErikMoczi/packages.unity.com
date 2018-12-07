

using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class RectTransformInvertedBindings : InvertedBindingsBase<RectTransform>
    {
        #region Static
        [TinyInitializeOnLoad(1)]
        [UsedImplicitly]
        private static void Register()
        {
            GameObjectTracker.RegisterForComponentModification<RectTransform>(SyncRectTransform);
        }

        public static void SyncRectTransform(RectTransform from, TinyEntityView view)
        {
            var registry = view.Registry;
            var tinyTransform = view.EntityRef.Dereference(registry).GetComponent(TypeRefs.UILayout.RectTransform);
            if (null != tinyTransform)
            {
                SyncRectTransform(from, tinyTransform);
            }
            else
            {
                from.pivot = from.anchorMin = from.anchorMax = new Vector2(0.5f, 0.5f);
                from.sizeDelta = Vector2.zero;
            }
        }

        public static void SyncRectTransform(RectTransform t, [NotNull] TinyObject tiny)
        {
            tiny.Refresh();
            tiny.AssignIfDifferent("anchorMin", t.anchorMin);
            tiny.AssignIfDifferent("anchorMax", t.anchorMax);
            tiny.AssignIfDifferent("anchoredPosition", t.anchoredPosition);
            tiny.AssignIfDifferent("pivot", t.pivot);
            tiny.AssignIfDifferent("sizeDelta", t.sizeDelta);
        }
        #endregion

        #region InvertedBindingsBase<RectTransform>

        public override void Create(TinyEntityView view, RectTransform t)
        {
            TransformInvertedBindings.CreateNewFromExisting(view, t);

            var rectTransform = new TinyObject(view.Registry, GetMainTinyType());
            SyncRectTransform(t, rectTransform);

            var entity = view.EntityRef.Dereference(view.Registry);
            view.EntityRef = (TinyEntity.Reference)entity;

            var tiny = entity.GetOrAddComponent(GetMainTinyType());
            tiny.CopyFrom(rectTransform);
        }

        public override TinyType.Reference GetMainTinyType()
        {
            return TypeRefs.UILayout.RectTransform;
        }
        #endregion
    }
}

