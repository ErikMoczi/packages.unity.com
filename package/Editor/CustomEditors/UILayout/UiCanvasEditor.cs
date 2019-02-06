using JetBrains.Annotations;

using UnityEditor;
using UnityEngine.UI;

using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomEditor(
        CoreGuids.UILayout.UICanvas)]
    [UsedImplicitly]
    internal class UiCanvasEditor : ComponentEditor
    {
        public UiCanvasEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var target = context.MainTarget<TinyEntity>();
            var enabled = GUI.enabled;

            // Not inspecting a default value.
            if (null != target)
            {
                if (target.HasTransformNode() && !target.Parent().Equals(TinyEntity.Reference.None))
                {
                    EditorGUILayout.HelpBox("Non-Root UICanvases are not supported at the moment.", MessageType.Warning);
                    return true;
                }

                var rectTransformTypeRef = TypeRefs.UILayout.RectTransform;
                if (!target.HasComponent(rectTransformTypeRef))
                {
                    EditorGUILayout.HelpBox("A RectTransform component is needed with the UICanvas.", MessageType.Warning);
                    AddComponentToTargetButton(context, rectTransformTypeRef, TypeRefs.Core2D.TransformNode);
                    GUI.enabled = false;
                }
            }

            VisitField(ref context, "camera");
            VisitField(ref context, "uiScaleMode");

            if (context.Value.GetProperty<CanvasScaler.ScaleMode>("uiScaleMode") == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                VisitField(ref context, "referenceResolution");
                VisitField(ref context, "matchWidthOrHeight");
            }

            GUI.enabled = enabled;
            return true;
        }

    }
}

