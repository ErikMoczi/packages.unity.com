using UnityEngine;
using UnityEngine.Experimental.Localization;
using UnityEditorInternal;

namespace UnityEditor.Experimental.Localization
{
    [CustomEditor(typeof(StartupLocaleSelectorCollection))]
    class StartupLocaleSelectorCollectionEditor : Editor
    {
        SerializedProperty m_StartupSelectors;

        ReorderableList m_List;

        class Texts
        {
            public GUIContent listTitle = new GUIContent("Locale Selection Order(top to bottom)");
        }
        static Texts s_Texts;

        void OnEnable()
        {
            // It can sometimes be null if we are embedded into the LocalizationSettings editor.
            if (target == null)
                return;

            if (s_Texts == null)
                s_Texts = new Texts();

            m_StartupSelectors = serializedObject.FindProperty("m_StartupSelectors");

            m_List = new ReorderableList(serializedObject, m_StartupSelectors);
            m_List.drawElementCallback = DrawListItem;
            m_List.drawHeaderCallback = DrawHeaderCallback;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_List.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        static void DrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, s_Texts.listTitle);
        }

        void DrawListItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            var item = m_StartupSelectors.GetArrayElementAtIndex(index);
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, item, new GUIContent("Selector " + index));
        }
    }
}
