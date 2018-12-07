using JetBrains.Annotations;
using Unity.Properties;
using UnityEditor;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Text.Text2DStyleNativeFont)]
    [UsedImplicitly]
    internal class Text2DStyleNativeFontEditor : ComponentEditor
    {
        public Text2DStyleNativeFontEditor(TinyContext context) : base(context)
        {
        }
        
        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var mainTarget = context.MainTarget<TinyEntity>();
            
            if (!mainTarget.HasComponent<Runtime.Text.TinyNativeFont>())
            {
                EditorGUILayout.HelpBox("A NativeFont component is needed with the Text2DRenderer.", MessageType.Warning);
                AddComponentToTargetButton(context, TypeRefs.Text.NativeFont);
            }
            
            VisitField(ref context, nameof(Runtime.Text.TinyText2DStyleNativeFont.italic));
            DrawWeight(ref context);
            return context.Visitor.StopVisit;
        }

        private static void DrawWeight(ref UIVisitContext<TinyObject> context)
        {
            const string name = nameof(Runtime.Text.TinyText2DStyleNativeFont.weight);
            var tiny = context.Value;
            var container = tiny.Properties;
            var weightProperty = container.PropertyBag.FindProperty(name) as IValueClassProperty;
            var isOverriden = (weightProperty as ITinyValueProperty)?.IsOverridden(container) ?? true;
            TinyEditorUtility.SetEditorBoldDefault(isOverriden);
            var mixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = context.Visitor.ChangeTracker.HasMixedValues<int>(tiny.Properties, weightProperty);
            try
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUILayout.Toggle("bold", tiny.GetProperty<int>(name) == 700);
                if (EditorGUI.EndChangeCheck())
                {
                    tiny.AssignPropertyFrom(name, value ? 700 : 400);
                    context.Visitor.ChangeTracker.PushChange(container, weightProperty);
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