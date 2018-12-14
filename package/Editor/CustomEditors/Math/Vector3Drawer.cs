using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using Unity.Properties;

namespace Unity.Tiny
{
    [TinyCustomDrawer(CoreGuids.Math.Vector3)]
    [UsedImplicitly]
    internal class Vector3Drawer : StructDrawer
    {
        public Vector3Drawer(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            EditorGUILayout.BeginHorizontal();

            if (Screen.width < 400)
            {
                EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth - (400 - Screen.width), 70);
            }

            if (!string.IsNullOrEmpty(context.Label))
            {
                EditorGUILayout.PrefixLabel(context.Label);
            }

            var indent = EditorGUI.indentLevel;
            try
            {
                EditorGUIUtility.labelWidth = 15;
                EditorGUIUtility.fieldWidth = 30;
                EditorGUI.indentLevel = 0;

                tinyObject.Properties.Visit(context.Visitor);
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel = indent;
                EditorGUIUtility.fieldWidth = 0;
                EditorGUIUtility.labelWidth = 0;
            }
            return true;
        }
    }
}

