using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomDrawer(CoreGuids.Math.Matrix4x4)]
    [UsedImplicitly]
    internal class Matrix4x4Drawer : StructDrawer
    {
        public Matrix4x4Drawer(TinyContext context)
            : base(context) { }

        private static string[] s_Matrix4x4Names = new string[]
        {
            "m00", "m01", "m02", "m03", "m10", "m11", "m12", "m13", "m20", "m21", "m22", "m23", "m30", "m31", "m32", "m33"
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
                    for (var i = 0; i < s_Matrix4x4Names.Length; i += 4)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.indentLevel = 0;
                        GUILayout.Space(30.0f);
                        EditorGUIUtility.labelWidth = 30;
                        EditorGUIUtility.fieldWidth = 15;

                        VisitField(ref context, s_Matrix4x4Names[i]);
                        VisitField(ref context, s_Matrix4x4Names[i + 1]);
                        VisitField(ref context, s_Matrix4x4Names[i + 2]);
                        VisitField(ref context, s_Matrix4x4Names[i + 3]);

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

            return true;
        }
    }
}

