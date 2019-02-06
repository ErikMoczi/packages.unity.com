using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomEditor(
        CoreGuids.Core2D.Sprite2DRendererOptions)]
    [UsedImplicitly]
    internal class Sprite2DRendererOptionsEditor : ComponentEditor
    {
        public Sprite2DRendererOptionsEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();
            var rt = target.GetComponent<Runtime.UILayout.TinyRectTransform>();
            var oldEnable = GUI.enabled;

            // UI path
            if (rt.IsValid)
            {
                EditorGUILayout.HelpBox("Some values are driven by the RectTransform", MessageType.None);
                GUI.enabled = false;
            }

            VisitField(ref context, "size");

            GUI.enabled = oldEnable;

            VisitField(ref context, "drawMode");
            if (context.Value.GetProperty<DrawMode>("drawMode") == DrawMode.AdaptiveTiling)
            {
                EditorGUILayout.HelpBox("The preview might be different that what will be exported.", MessageType.Warning);
            }
            return true;
        }

    }
}

