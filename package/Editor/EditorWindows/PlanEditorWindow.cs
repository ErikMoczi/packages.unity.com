using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.AI.Planner.UI;
using UnityEngine;
using UnityEngine.AI.Planner.DomainLanguage.TraitBased;
using UnityEngine.UIElements;

namespace UnityEditor.AI.Planner.DomainLanguage.TraitBased
{
    class PlanEditorWindow : EditorWindow
    {
        enum ItemType
        {
            Action,
            Termination
        }

        [SerializeField]
        PlanDefinition m_PlanDefinition;

        Board m_Board;
        BoardSection m_Actions;
        BoardSection m_Terminations;

        // We can't affix user data to BoardFields, so it is necessary to keep a lookup (e.g. in order to delete an item)
        Dictionary<BoardField, INamedData> m_ItemLookup;

        // We use SerializedObject and SerializedProperty for UI rendering purposes and for keeping the DomainDefinition
        // in sync with modified data from the UI
        SerializedObject m_Target;
        SerializedProperty m_SelectedItem;

        VisualElement m_ContentHeader;

        Vector2 m_ContentScrollPosition;

        public static PlanEditorWindow ShowWindow(PlanDefinition planDefinition)
        {
            if (!planDefinition.DomainDefinition)
            {
                Debug.LogError(PlanDefinitionInspector.MissingDomainDefinitionMessage, planDefinition);
                return null;
            }

            foreach (var w in Resources.FindObjectsOfTypeAll<PlanEditorWindow>())
                if (w.m_PlanDefinition == planDefinition)
                {
                    w.Focus();
                    if (focusedWindow != w)
                        w.Show();
                    return w;
                }

            var window = CreateInstance<PlanEditorWindow>();
            window.Initialize(planDefinition);
            window.Show();

            return window;
        }

        void OnEnable()
        {
            if (m_PlanDefinition)
                Initialize(m_PlanDefinition);
        }

        void Initialize(PlanDefinition planDefinition)
        {
            m_ItemLookup = new Dictionary<BoardField, INamedData>();

            m_PlanDefinition = planDefinition;
            m_PlanDefinition.definitionChanged += OnDefinitionChanged;

            m_Target = new SerializedObject(m_PlanDefinition);
            m_SelectedItem = null;

            var title = "AI Plan Definition";
            titleContent = new GUIContent(title);
            var root = rootVisualElement;

            root.Clear();

            var container = new VisualElement();
            container.StretchToParentSize();
            container.style.flexDirection = FlexDirection.Row;

            m_Board = new Board();
            m_Board.scrollable = true;
            m_Board.title = m_PlanDefinition.name;
            m_Board.subTitle = title;
            m_Board.addItemRequested = OnAddItemRequested;
            m_Board.editTextRequested = OnEditTextRequested;
            m_Board.deleteItemRequested = OnDeleteItemRequested;

            var editDomainButton = new Button(() => DomainEditorWindow.ShowWindow(m_PlanDefinition.DomainDefinition));
            editDomainButton.text = "Edit Domain Definition";
            m_Board.Add(editDomainButton);

            var exportButton = new Button(() => m_PlanDefinition.GenerateClasses());
            exportButton.text = "Generate Classes";
            m_Board.Add(exportButton);

            titleContent = new GUIContent(title);

            m_Board.title = planDefinition.name;
            m_Board.subTitle = title;
            m_Board.addItemRequested = OnAddItemRequested;
            m_Board.editTextRequested = OnEditTextRequested;
            m_Board.deleteItemRequested = OnDeleteItemRequested;

            m_Actions = new BoardSection { title = "ACTIONS" };
            m_Board.Add(m_Actions);

            m_Terminations = new BoardSection { title = "TERMINATION"};
            m_Board.Add(m_Terminations);

            container.Add(m_Board);

            var boardResources = ScriptableSingleton<BoardResources>.instance;
            var boardStyleSheet = boardResources.BoardStyleSheet;

            var contentPane = new VisualElement();
            m_ContentHeader = new VisualElement();
            m_ContentHeader.styleSheets.Add(boardStyleSheet);
            m_ContentHeader.AddToClassList("boardContentHeader");
            var headerItem = new Label { name = "item" };
            m_ContentHeader.Add(headerItem);
            var headerSection = new Label { name = "section" };
            headerSection.AddToClassList("boardContentHeaderText");
            m_ContentHeader.Add(headerSection);

            var imgui = new IMGUIContainer(ItemOnGUI);
            imgui.styleSheets.Add(boardStyleSheet);
            imgui.style.flexGrow = 1f;

            contentPane.Add(m_ContentHeader);
            contentPane.Add(imgui);
            contentPane.style.flexGrow = 1f;
            container.Add(contentPane);

            root.Add(container);

            foreach (var item in m_PlanDefinition.ActionDefinitions)
            {
                var field = new BoardField(item.Name, null, boardField => SelectItem(item));
                field.AddToClassList("action");

                m_ItemLookup[field] = item;
                m_Actions.Add(field);
            }

            foreach (var item in m_PlanDefinition.StateTerminationDefinitions)
            {
                var field = new BoardField(item.Name, null, boardField => SelectItem(item));
                field.AddToClassList("termination");

                m_ItemLookup[field] = item;
                m_Terminations.Add(field);
            }
        }

        void OnDefinitionChanged()
        {
            string propertyPath = null;
            if (m_SelectedItem != null)
                propertyPath = m_SelectedItem.propertyPath;

            m_Target.Update();

            if (!string.IsNullOrEmpty(propertyPath))
                m_SelectedItem = m_Target.FindProperty(propertyPath);
        }

        void ItemOnGUI()
        {
            if (m_SelectedItem != null)
            {
                m_SelectedItem.isExpanded = true;

                m_ContentScrollPosition = EditorGUILayout.BeginScrollView(m_ContentScrollPosition);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                EditorGUILayout.BeginVertical();
                GUILayout.Space(20f);

                // For some reason with a custom property drawer it is necessary to send the label. The Inspector does
                // not have this problem. Unfortunately, we can't strip the foldout/label either without losing the
                // custom property drawer.
                EditorGUILayout.PropertyField(m_SelectedItem, new GUIContent(m_SelectedItem.displayName), true);

                if (m_Target.hasModifiedProperties)
                {
                    m_Target.ApplyModifiedProperties();

                    // Update the name in case that field changed
                    var item = GetItemForSerializedProperty(m_SelectedItem);
                    var boardField = GetBoardFieldForItem(item);
                    boardField.text = item.Name;
                }

                GUILayout.Space(20f);
                EditorGUILayout.EndVertical();
                GUILayout.Space(20f);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView();
            }
        }

        void SelectItem(INamedData item)
        {
            var headerItem = m_ContentHeader.Q<Label>("item");
            var headerSection = m_ContentHeader.Q<Label>("section");

            if (item == null)
            {
                headerItem.text = string.Empty;
                headerItem.ClearClassList();
                headerSection.text = String.Empty;
                m_SelectedItem = null;
                return;
            }

            string section = string.Empty;
            switch (item.GetType().Name)
            {
                case nameof(ActionDefinition):
                    section = "Action";
                    break;

                case nameof(StateTerminationDefinition):
                    section = "Termination";
                    break;
            }

            headerItem.text = item.Name;
            headerItem.ClearClassList();
            headerItem.AddToClassList("boardContentHeaderText");
            headerItem.AddToClassList(section.ToLower());
            headerSection.text = string.Format(" : {0}", section);

            m_SelectedItem = GetSerializedPropertyForItem(item);
        }

        SerializedProperty GetSerializedPropertyForItem(INamedData item)
        {
            var propertyPath = string.Empty;
            switch (item.GetType().Name)
            {
                case nameof(ActionDefinition):
                {
                    var index = m_PlanDefinition.ActionDefinitions.ToList().IndexOf((ActionDefinition)item);
                    propertyPath = $"m_{nameof(PlanDefinition.ActionDefinitions)}.Array.data[{index}]";
                    break;
                }

                case nameof(StateTerminationDefinition):
                {
                    var index = m_PlanDefinition.StateTerminationDefinitions.ToList().IndexOf((StateTerminationDefinition)item);
                    propertyPath = $"m_{nameof(PlanDefinition.StateTerminationDefinitions)}.Array.data[{index}]";
                    break;
                }
            }

            return m_Target.FindProperty(propertyPath);
        }

        INamedData GetItemForSerializedProperty(SerializedProperty property)
        {
            var propertyPath = property.propertyPath;
            var match = Regex.Match(propertyPath, @"\d+");
            if (match.Success)
            {
                if (int.TryParse(match.Value, out var index))
                {
                    if (propertyPath.Contains(nameof(ActionDefinition)))
                        return m_PlanDefinition.ActionDefinitions.ElementAt(index);

                    if (propertyPath.Contains(nameof(StateTerminationDefinition)))
                        return m_PlanDefinition.StateTerminationDefinitions.ElementAt(index);
                }
            }

            return null;
        }

        BoardField GetBoardFieldForItem(INamedData item)
        {
            foreach (var kvp in m_ItemLookup)
            {
                if (kvp.Value == item)
                    return kvp.Key;
            }

            return null;
        }

        void OnAddItemRequested(Board board)
        {
            var menu = new GenericMenu();

            menu.AddItem(EditorGUIUtility.TrTextContent("Add Action"), false, OnMenuItemTriggered, ItemType.Action);
            menu.AddItem(EditorGUIUtility.TrTextContent("Add Termination"), false, OnMenuItemTriggered, ItemType.Termination);

            menu.ShowAsContext();
        }

        void OnMenuItemTriggered(object userData)
        {
            var itemType = (ItemType)userData;
            INamedData item = null;
            var field = new BoardField(itemType.ToString(), null, boardField => SelectItem(item));
            field.AddToClassList(itemType.ToString().ToLower());

            switch (itemType)
            {
                case ItemType.Action:
                    var actions = m_PlanDefinition.ActionDefinitions.ToList();
                    item = new ActionDefinition { Name = field.text };
                    actions.Add((ActionDefinition)item);
                    m_PlanDefinition.ActionDefinitions = actions;
                    m_Actions.Add(field);
                    break;

                case ItemType.Termination:
                    var terminations = m_PlanDefinition.StateTerminationDefinitions.ToList();
                    item = new StateTerminationDefinition { Name = field.text };
                    terminations.Add((StateTerminationDefinition)item);
                    m_PlanDefinition.StateTerminationDefinitions = terminations;
                    m_Terminations.Add(field);
                    break;
            }

            m_ItemLookup[field] = item;

            m_Target.Update();
            SelectItem(item);

            field.OpenTextEditor();
        }

        void OnEditTextRequested(Board board, VisualElement element, string text)
        {
            var field = (BoardField)element;
            field.text = text;

            var nameProperty = m_SelectedItem.FindPropertyRelative("m_Name");
            nameProperty.stringValue = text;
            m_Target.ApplyModifiedProperties();

            SelectItem(m_ItemLookup[field]);
        }

        void OnDeleteItemRequested(Board board, VisualElement element)
        {
            var key = (BoardField)element;
            if (m_ItemLookup.TryGetValue(key, out var item))
            {
                if (item is ActionDefinition action)
                {
                    var actions = m_PlanDefinition.ActionDefinitions.ToList();
                    actions.Remove(action);
                    m_PlanDefinition.ActionDefinitions = actions;

                    m_Actions.Remove(key);
                }

                if (item is StateTerminationDefinition termination)
                {
                    var terminations = m_PlanDefinition.StateTerminationDefinitions.ToList();
                    terminations.Remove(termination);
                    m_PlanDefinition.StateTerminationDefinitions = terminations;

                    m_Terminations.Remove(key);
                }

                var namedData = item as INamedData;
                if (namedData != null)
                {
                    AssetDatabase.DeleteAsset($"{m_PlanDefinition.GeneratedClassDirectory}{namedData.Name}.cs");
                    AssetDatabase.DeleteAsset($"{m_PlanDefinition.GeneratedClassDirectory}{namedData.Name}.Extra.cs");
                }

                SelectItem(null);
                m_ItemLookup.Remove(key);
                m_Target.Update();
            }
        }
    }
}
