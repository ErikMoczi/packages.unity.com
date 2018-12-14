using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomDrawer(CoreGuids.TinyEditorExtensions.CurveEntity)]
    [UsedImplicitly]
    internal class CurveDrawer : StructDrawer
    {
        public CurveDrawer(TinyContext context) : base(context)
        {
        }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tiny = context.Value;
            var curve = tiny.As<AnimationCurve>();

            EditorGUI.BeginChangeCheck();
            curve = EditorGUILayout.CurveField(context.Label, curve);

            if (EditorGUI.EndChangeCheck())
            {
                tiny.AssignFrom(curve);
                var changeTracker = context.Visitor.ChangeTracker;
                changeTracker.PushChange(tiny.Properties, tiny.Properties.PropertyBag.FindProperty("keys"));
                changeTracker.PushChange(tiny.Properties, tiny.Properties.PropertyBag.FindProperty("postWrapMode"));
                changeTracker.PushChange(tiny.Properties, tiny.Properties.PropertyBag.FindProperty("preWrapMode"));
            }

            return context.Visitor.StopVisit;
        }
    }
}