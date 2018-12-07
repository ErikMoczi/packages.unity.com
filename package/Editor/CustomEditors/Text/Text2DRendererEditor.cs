
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

using Unity.Properties;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Text.Text2DRenderer)]
    [UsedImplicitly]
    internal class Text2DRendererEditor : ComponentEditor
    {
        public Text2DRendererEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var mainTarget = context.MainTarget<TinyEntity>();
            
            if (!mainTarget.HasComponent<Runtime.Text.TinyText2DStyle>())
            {
                EditorGUILayout.HelpBox("A Text2DStyle component is needed with the Text2DRenderer.", MessageType.Warning);
                AddComponentToTargetButton(context, TypeRefs.Text.Text2DStyle);
            }
            
            if (!mainTarget.HasComponent<Runtime.Text.TinyText2DStyleNativeFont>())
            {
                EditorGUILayout.HelpBox("A Text2DStyleNativeFont component is needed with the Text2DRenderer.", MessageType.Warning);
                AddComponentToTargetButton(context, TypeRefs.Text.Text2DStyleNativeFont);
            }
            
            if (!mainTarget.HasComponent<Runtime.Text.TinyNativeFont>())
            {
                EditorGUILayout.HelpBox("A NativeFont component is needed with the Text2DRenderer.", MessageType.Warning);
                AddComponentToTargetButton(context, TypeRefs.Text.NativeFont);
            }
            
            if (!mainTarget.HasComponent<Runtime.Text.TinyText2DStyle>())
            {
                EditorGUILayout.HelpBox("A Text2DStyle component is needed with the Text2DRenderer.", MessageType.Warning);
                AddComponentToTargetButton(context, TypeRefs.Text.Text2DStyle);
            }
            
            if (mainTarget.HasComponent<Runtime.UILayout.TinyRectTransform>() &&
                !mainTarget.HasComponent<Runtime.Core2D.TinyRectTransformFinalSize>())
            {
                EditorGUILayout.HelpBox("A RectTransformFinalSize component is needed when using UI layouting.", MessageType.Warning);
                AddComponentToTargetButton(context, TypeRefs.Core2D.RectTransformFinalSize);
            }
            
            if (!mainTarget.HasComponent<Runtime.UILayout.TinyRectTransform>())
            {
                if (mainTarget.HasComponent<Runtime.Core2D.TinyRectTransformFinalSize>())
                {
                    EditorGUILayout.HelpBox("A RectTransformFinalSize component should not be used outside of UI layouting.", MessageType.Warning);
                    RemoveComponentToTargetButton(context, TypeRefs.Core2D.RectTransformFinalSize);
                }

                if (mainTarget.HasComponent<Runtime.Text.TinyText2DAutoFit>())
                {
                    EditorGUILayout.HelpBox("A Text2DAutoFit component should not be used outside of UI layouting.", MessageType.Warning);
                    RemoveComponentToTargetButton(context, TypeRefs.Text.Text2DAutoFit);
                }
            }
            
            VisitField(ref context, nameof(Runtime.Text.TinyText2DRenderer.text));
            DrawPivot(ref context);
            return context.Visitor.StopVisit;
        }

        private static void DrawPivot(ref UIVisitContext<TinyObject> context)
        {
            var name = nameof(Runtime.Text.TinyText2DRenderer.pivot);
            var tinyObject = context.Value;
            var container = tinyObject.Properties;
            var pivotProperty = container.PropertyBag.FindProperty(name) as IValueClassProperty;
            
            var pivot = (tinyObject[name] as TinyObject).Properties;
            var pivotXProperty = pivot.PropertyBag.FindProperty("x") as IValueClassProperty;;
            var pivotYProperty = pivot.PropertyBag.FindProperty("y") as IValueClassProperty;
            var anchor = TinyGUIUtility.GetTextAnchorFromPivot(tinyObject.GetProperty<Vector2>(name));
            
            var mixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = context.Visitor.ChangeTracker.HasMixedValues<float>(pivot, pivotXProperty) ||
                                       context.Visitor.ChangeTracker.HasMixedValues<float>(pivot, pivotYProperty) ;
            var isOverriden = (pivotProperty as ITinyValueProperty)?.IsOverridden(tinyObject.Properties) ?? true;
            TinyEditorUtility.SetEditorBoldDefault(isOverriden);
            try
            {
                EditorGUI.BeginChangeCheck();
                anchor = (TextAnchor) EditorGUILayout.EnumPopup("anchor", anchor);
                if (EditorGUI.EndChangeCheck())
                {
                    tinyObject.AssignPropertyFrom(name, TinyGUIUtility.GetPivotFromTextAnchor(anchor));
                    context.Visitor.ChangeTracker.PushChange(pivot, pivotXProperty);
                    context.Visitor.ChangeTracker.PushChange(pivot, pivotYProperty);
                }
            }
            finally
            {
                TinyEditorUtility.SetEditorBoldDefault(false);
                EditorGUI.showMixedValue = mixed;                
            }
        }
    }
}

