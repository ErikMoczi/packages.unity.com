using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Core2D.LayerSorting)]
    [UsedImplicitly]
    internal class SortingLayerEditor : ComponentEditor
    {
        public SortingLayerEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            DrawEnum(ref context, "layer");
            VisitField(ref context, "order");
            return true;
        }

        private void DrawEnum(ref UIVisitContext<TinyObject> context, string fieldName)
        {
            var tinyObject = context.Value;
            var layers = SortingLayer.layers.ToList();
            var names = layers.Select(l => new GUIContent(l.name)).ToArray();
            var index = layers.FindIndex(l => l.id == tinyObject.GetProperty<int>(fieldName));
            var label = new GUIContent(fieldName);

            if (names.Length == 0)
            {
                EditorGUILayout.Popup(label, -1, names);
                return;
            }

            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup(label, index, names);
            if (EditorGUI.EndChangeCheck())
            {
                tinyObject.AssignIfDifferent(fieldName, layers[index].id);
                context.Visitor.ChangeTracker.PushChange(tinyObject.Properties, tinyObject.Properties.PropertyBag.FindProperty(fieldName));
            }
        }
    }
}

