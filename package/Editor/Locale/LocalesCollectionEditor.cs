using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Localization;

namespace UnityEditor.Experimental.Localization
{
    [CustomEditor(typeof(LocalesCollection))]
    class LocalesCollectionEditor : Editor
    {
        LocalesCollectionListView m_ListView;
        SerializedProperty m_DefaultLocale;
        SerializedProperty m_Locales;
        SearchField m_SearchField;

        Editor m_SelectedLocaleEditor;
        int m_SelectedLocaleIndex = -1;

        enum ToolBarChoices
        {
            LocaleGeneratorWindow,
            RemoveSelected,
            AddAsset,
            AddAllAssets
        }

        class Texts
        {
            public GUIContent defaultLocale = new GUIContent("Default Locale", "The locale to use when the application starts providing the startup behavior is not set to use the system locale and no command line arguments have been provided to set the locale.");
            public GUIContent localeDetails = new GUIContent("Locale Details");
            public GUIContent[] toolbarButtons = new[]
            {
                new GUIContent("Locale Generator", "Opens the Locale Generator window."),
                new GUIContent("Remove Selected"), 
                new GUIContent("Add", "Add a new Locale asset."),
                new GUIContent("Add All", "Add all Locale assets from the project.")
            };
        }

        static Texts s_Texts;

        const float k_MinListHeight = 200;

        void OnEnable()
        {
            // Can go null when we have an embedded editor.
            if (target == null) 
                return;

            if (s_Texts == null)
                s_Texts = new Texts();

            m_Locales = serializedObject.FindProperty("m_Locales");
            m_ListView = new LocalesCollectionListView(m_Locales);
            m_DefaultLocale = serializedObject.FindProperty("m_DefaultLocale");
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_ListView.SetFocusAndEnsureSelectedItem;
            Undo.undoRedoPerformed += UndoPerformed;
            Undo.postprocessModifications += PostprocessModifications;
        }

        UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            foreach (var undoPropertyModification in modifications)
            {
                // Check for external changes such as when the user clicks the reset popup menu item on the object.
                if (undoPropertyModification.currentValue.propertyPath == "m_Locales.Array.size")
                {
                    serializedObject.Update();
                    m_ListView.Reload();
                }
            }
            return modifications;
        }

        void UndoPerformed()
        {
            serializedObject.Update();
            m_ListView.Reload();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoPerformed;
            Undo.postprocessModifications -= PostprocessModifications;
        }

        public void Reset()
        {
            if (m_ListView != null)
                m_ListView.Reload();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultLocaleField();
            EditorGUILayout.Space();
            DrawLocaleList();
            DrawToolbar();
            serializedObject.ApplyModifiedProperties();
        }
         
        void AddLocale(Locale locale)
        {
            if (locale != null)
            {
                // Add the locale if it is not already in the list
                bool found = false;
                for (int i = 0; i < m_Locales.arraySize; ++i)
                {
                    var item = m_Locales.GetArrayElementAtIndex(i);
                    if (item.objectReferenceValue == locale)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    m_Locales.InsertArrayElementAtIndex(m_Locales.arraySize);
                    var item = m_Locales.GetArrayElementAtIndex(m_Locales.arraySize - 1);
                    item.objectReferenceValue = locale;
                    m_ListView.Reload();
                }
            }
        }

        void DrawDefaultLocaleField()
        {
            var rect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(rect, s_Texts.defaultLocale, m_DefaultLocale);
            EditorGUI.BeginChangeCheck();
            rect = EditorGUI.PrefixLabel(rect, s_Texts.defaultLocale);
            var newSelection = EditorGUI.ObjectField(rect, m_DefaultLocale.objectReferenceValue, typeof(Locale), false) as Locale;
            if (EditorGUI.EndChangeCheck())
            {
                m_DefaultLocale.objectReferenceValue = newSelection;
                AddLocale(newSelection);
            }
            EditorGUI.EndProperty();
        }

        void DrawLocaleList()
        {
            m_ListView.searchString = m_SearchField.OnToolbarGUI(m_ListView.searchString);
            m_ListView.OnGUI(EditorGUILayout.GetControlRect(false, Mathf.Max(k_MinListHeight, m_ListView.totalHeight)));
        }

        void DrawToolbar()
        {
            string commandName = Event.current.commandName;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            var selection = (ToolBarChoices)GUILayout.Toolbar(-1, s_Texts.toolbarButtons, EditorStyles.toolbarButton);
            switch (selection)
            {
                case ToolBarChoices.LocaleGeneratorWindow:
                    LocaleGeneratorWindow.ShowWindow();
                    break;
                case ToolBarChoices.RemoveSelected:
                    {
                        var selectedLocales = m_ListView.GetSelection();

                        for (int i = selectedLocales.Count - 1; i >= 0; --i)
                        {
                            m_Locales.GetArrayElementAtIndex(selectedLocales[i]).objectReferenceValue = null;
                            m_Locales.DeleteArrayElementAtIndex(selectedLocales[i]);
                        }
                        m_ListView.SetSelection(new int[0]);
                        m_ListView.Reload();
                    }
                    break;
                case ToolBarChoices.AddAsset:
                    EditorGUIUtility.ShowObjectPicker<Locale>(null, false, string.Empty, controlID);
                    break;
                case ToolBarChoices.AddAllAssets:
                    {
                        var assets = AssetDatabase.FindAssets("t:Locale");
                        m_Locales.arraySize = assets.Length;
                        for (int i = 0; i < assets.Length; ++i)
                        {
                            var item = m_Locales.GetArrayElementAtIndex(i);
                            item.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Locale>(AssetDatabase.GUIDToAssetPath(assets[i]));
                        }
                        m_ListView.Reload();
                    }
                    break;
            }

            // Selected Locale
            if (m_ListView.GetSelection().Count == 1)
            {
                if (m_SelectedLocaleEditor == null || m_SelectedLocaleIndex != m_ListView.GetSelection()[0])
                {
                    m_SelectedLocaleIndex = m_ListView.GetSelection()[0];
                    var item = m_Locales.GetArrayElementAtIndex(m_SelectedLocaleIndex).objectReferenceValue as Locale;
                    Editor.CreateCachedEditor(item, null, ref m_SelectedLocaleEditor);
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(s_Texts.localeDetails);
                EditorGUI.indentLevel++;
                m_SelectedLocaleEditor.OnInspectorGUI();
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            if (EditorGUIUtility.GetObjectPickerControlID() == controlID && commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerObject() != null)
            {
                AddLocale(EditorGUIUtility.GetObjectPickerObject() as Locale);
            }
        }
    }
}
