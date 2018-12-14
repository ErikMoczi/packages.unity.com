using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomDrawer(CoreGuids.TinyEditorExtensions.GradientEntity)]
    [UsedImplicitly]
    internal class GradientDrawer : StructDrawer
    {
        public GradientDrawer(TinyContext context) : base(context)
        {
        }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tiny = context.Value;
            var gradient = tiny.As<Gradient>();

            EditorGUI.BeginChangeCheck();
            gradient = EditorGUILayoutBridge.GradientField(context.Label, gradient);

            if (EditorGUI.EndChangeCheck())
            {
                tiny.AssignFrom(gradient);
                var changeTracker = context.Visitor.ChangeTracker;
                changeTracker.PushChange(tiny.Properties, tiny.Properties.PropertyBag.FindProperty("alphaKeys"));
                changeTracker.PushChange(tiny.Properties, tiny.Properties.PropertyBag.FindProperty("colorKeys"));
                changeTracker.PushChange(tiny.Properties, tiny.Properties.PropertyBag.FindProperty("times"));
            }

            return context.Visitor.StopVisit;
        }
    }
}