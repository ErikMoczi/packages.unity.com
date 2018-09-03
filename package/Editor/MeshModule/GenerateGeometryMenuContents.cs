using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.U2D.Animation
{
    public class GenerateGeometryMenuContents : PopupWindowContent
    {
        private static class Contents
        {
            public static readonly GUIContent outlineDetail = new GUIContent("Outline Detail", "");
            public static readonly GUIContent alphaTolerance = new GUIContent("Alpha Tolerance", "");
            public static readonly GUIContent subdivide = new GUIContent("Subdivide", "");
            public static readonly GUIContent generateButton = new GUIContent("Generate", "");
        }

        public delegate void CallbackDelegate();

        public CallbackDelegate onGenerateGeometry;

        public GenerateGeometrySettings settings
        {
            get { return m_Settings; }
            set { m_Settings = value; }
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 90);
        }

        public override void OnGUI(Rect rect)
        {
            Debug.Assert(m_Settings != null);
            Debug.Assert(onGenerateGeometry != null);

            settings.outlineDetail = EditorGUILayout.Slider(Contents.outlineDetail, settings.outlineDetail, 0f, 1f);
            settings.alphaTolerance = (byte)EditorGUILayout.IntSlider(Contents.alphaTolerance, settings.alphaTolerance, 0, 255);
            settings.subdividePercent = EditorGUILayout.Slider(Contents.subdivide, settings.subdividePercent, 0f, 100f);

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth + 4);
            if (GUILayout.Button(Contents.generateButton))
                onGenerateGeometry.Invoke();
            GUILayout.EndHorizontal();
        }

        public override void OnOpen()
        {
        }

        public override void OnClose()
        {
        }

        GenerateGeometrySettings m_Settings;
    }
}
