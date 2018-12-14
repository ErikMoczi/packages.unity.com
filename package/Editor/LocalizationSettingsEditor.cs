using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Localization;

namespace UnityEditor.Experimental.Localization
{
    [CustomEditor(typeof(LocalizationSettings))]
    class LocalizationSettingsEditor : Editor
    {
        SerializedProperty m_LocaleSelector;
        SerializedProperty m_AvailableLocales;
        SerializedProperty m_SelectedLocaleChanged;
        Editor m_LocaleSelectorEditor;
        Editor m_AvailableLocalesEditor;

        class Texts
        {
            public GUIContent activeSettings = new GUIContent("Active Settings", "The localization settings that will be used by this project and included into any builds.");
            public GUIContent availableLocales = new GUIContent("Available Locales", "Controls what locales are supported by this application.");
            public GUIContent helpTextNotActive = new GUIContent("This is not the active localization settings and will not be used when localizing the application. Would you like to make it active?");
            public GUIContent helpTextActive = new GUIContent("This is the active localization settings and will be automatically included and loaded in any builds.");
            public GUIContent localeSelector = new GUIContent("Locale Selector", "Determines what locale should be used when the application starts or does not currently have an active locale and needs one.");
            public GUIContent makeActive = new GUIContent("Set Active");
            public GUIContent selectedLocaleChanged = new GUIContent("Selected Locale Changed", "Event sent when the applications selected locale is changed.");
        }
        static Texts s_Texts;

        [MenuItem("Edit/Project Settings/Localization")]
        static void ShowActive()
        {
            if (LocalizationPlayerSettings.activeLocalizationSettings == null)
            {
                if (EditorUtility.DisplayDialog("Localization Settings", "You have no active localization settings. Would you like to create one?", "OK", "Cancel"))
                    LocalizationPlayerSettings.activeLocalizationSettings = LocalizationSettingsMenuItems.CreateLocalizationAsset();
            }
            else
            {
                Selection.activeObject = LocalizationPlayerSettings.activeLocalizationSettings;
            }
        }

        void OnEnable()
        {
            if (s_Texts == null)
                s_Texts = new Texts();

            m_LocaleSelector = serializedObject.FindProperty("m_LocaleSelector");
            m_AvailableLocales = serializedObject.FindProperty("m_AvailableLocales");
            m_SelectedLocaleChanged = serializedObject.FindProperty("m_SelectedLocaleChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawActiveSettings();
            ObjectReferenceWithEditorView(m_AvailableLocales, s_Texts.availableLocales, ref m_AvailableLocalesEditor);

            ObjectReferenceWithEditorView(m_LocaleSelector, s_Texts.localeSelector, ref m_LocaleSelectorEditor);
            EditorGUILayout.PropertyField(m_SelectedLocaleChanged, s_Texts.selectedLocaleChanged);
            serializedObject.ApplyModifiedProperties();
        }

        void DrawActiveSettings()
        {
            EditorGUI.BeginChangeCheck();
            var obj = EditorGUILayout.ObjectField(s_Texts.activeSettings, LocalizationPlayerSettings.activeLocalizationSettings, typeof(LocalizationSettings), false) as LocalizationSettings; 
            if (EditorGUI.EndChangeCheck())
                LocalizationPlayerSettings.activeLocalizationSettings = obj;

            if (LocalizationPlayerSettings.activeLocalizationSettings != target)
            {
                EditorGUILayout.HelpBox(s_Texts.helpTextNotActive.text, MessageType.Warning, true);
                if (GUILayout.Button(s_Texts.makeActive, GUILayout.Width(150)))
                    LocalizationPlayerSettings.activeLocalizationSettings = (LocalizationSettings)target;
            }
            else
            {
                EditorGUILayout.HelpBox(s_Texts.helpTextActive.text, MessageType.Info, true);
            }
            EditorGUILayout.Space();
        }

        static void ObjectReferenceWithEditorView(SerializedProperty property, GUIContent label, ref Editor editor)
        {
            EditorGUILayout.BeginHorizontal();
            if(property.objectReferenceValue != null)
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label);
            else
                GUILayout.Label(label);
            EditorGUILayout.PropertyField(property, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            if (property.isExpanded)
            {
                if (editor == null || editor.target != property.objectReferenceValue)
                {
                    CreateCachedEditor(property.objectReferenceValue, null, ref editor);
                }
                if(editor != null)
                    editor.OnInspectorGUI();
            }
            EditorGUILayout.Space();
        }
    }
}
