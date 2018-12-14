

using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomDrawer(CoreGuids.Math.Matrix3x3)]
    [UsedImplicitly]
    internal class Matrix3x3Drawer : StructDrawer
    {
        public Matrix3x3Drawer(TinyContext context)
            : base(context) { }

        private static string[] s_Matrix3x3Names =
        {
            "m00", "m01", "m02", "m10", "m11", "m12", "m20", "m21", "m22"
        };

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            var showProperties = true;
            if (!string.IsNullOrEmpty(context.Label))
            {
                var folderCache = context.Visitor.FolderCache;
                if (!folderCache.TryGetValue(tinyObject, out showProperties))
                {
                    showProperties = true;
                }
                showProperties = folderCache[tinyObject] = EditorGUILayout.Foldout(showProperties, context.Label, true);
            }

            if (showProperties)
            {
                if (Screen.width < 400)
                {
                    EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth - (400 - Screen.width), 70);
                }

                var indent = EditorGUI.indentLevel;
                try
                {
                    for (var i = 0; i < s_Matrix3x3Names.Length; i += 3)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.indentLevel = 0;
                        GUILayout.Space(30.0f);
                        EditorGUIUtility.labelWidth = 30;
                        EditorGUIUtility.fieldWidth = 15;

                        VisitField(ref context, s_Matrix3x3Names[i]);
                        VisitField(ref context, s_Matrix3x3Names[i + 1]);
                        VisitField(ref context, s_Matrix3x3Names[i + 2]);
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = indent;
                    EditorGUIUtility.fieldWidth = 0;
                    EditorGUIUtility.labelWidth = 0;
                }
            }

            return context.Visitor.StopVisit;
        }
    }
}

