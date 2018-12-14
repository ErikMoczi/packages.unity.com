using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomEditor(
        CoreGuids.Core2D.Sprite2DRenderer)]
    [UsedImplicitly]
    internal class Sprite2DRendererEditor : ComponentEditor
    {
        public Sprite2DRendererEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            var target = context.MainTarget<TinyEntity>();
            VisitField(ref context, "sprite");
            if (target.HasComponent(TypeRefs.UILayout.RectTransform))
            {
                var sprite = tinyObject.GetProperty<Sprite>("sprite");

                if (null != sprite && !Approximately(GetNormalizedPivot(sprite), new Vector2(0.5f, 0.5f)))
                {
                    EditorGUILayout.HelpBox("Only sprites with a pivot of (0.5, 0.5) can be used with auto-layouting.", MessageType.Warning);
                }
            }
            VisitField(ref context, "color");
            VisitField(ref context, "blending");

            return true;
        }

        private static Vector2 GetNormalizedPivot(Sprite sprite)
        {
            var bounds = sprite.bounds;
            var pivotX = - bounds.center.x / bounds.extents.x / 2 + 0.5f;
            var pivotY = - bounds.center.y / bounds.extents.y / 2 + 0.5f;
            return new Vector2(pivotX, pivotY);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }

    }
}

