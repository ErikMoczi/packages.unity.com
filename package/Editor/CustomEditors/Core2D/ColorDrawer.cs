using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomDrawer(CoreGuids.Core2D.Color)]
    [UsedImplicitly]
    internal class ColorDrawer : StructDrawer
    {
        public ColorDrawer(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            var value = tinyObject.As<Color>();
            EditorGUI.BeginChangeCheck();

            TinyEditorUtility.SetEditorBoldDefault(tinyObject.IsOverridden);
            value = EditorGUILayout.ColorField(string.IsNullOrEmpty(context.Label) ? GUIContent.none : new GUIContent(context.Label), value);
            TinyEditorUtility.SetEditorBoldDefault(false);
            if (EditorGUI.EndChangeCheck())
            {
                tinyObject.AssignFrom(value);
                foreach (var prop in tinyObject.Properties.PropertyBag.Properties)
                {
                    context.Visitor.ChangeTracker.PushChange(tinyObject.Properties, prop);
                }
            }
            return true;
        }
    }
}

