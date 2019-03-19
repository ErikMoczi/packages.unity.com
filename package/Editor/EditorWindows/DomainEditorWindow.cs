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
    class DomainEditorWindow : EditorWindow
    {
        enum ItemType
        {
            Enumeration,
            Trait,
            Alias
        }

        [SerializeField]
        DomainDefinition m_Domain;

        Board m_Board;
        BoardSection m_Enumerations;
        BoardSection m_Traits;
        BoardSection m_Aliases;

        // We can't affix user data to BoardFields, so it is necessary to keep a lookup (e.g. in order to delete an item)
        Dictionary<BoardField, INamedData> m_ItemLookup;

        // We use SerializedObject and SerializedProperty for UI rendering purposes and for keeping the DomainDefinition
        // in sync with modified data from the UI
        SerializedObject m_Target;
        SerializedProperty m_SelectedItem;

        VisualElement m_ContentHeader;

        Vector2 m_ContentScrollPosition;

        public static DomainEditorWindow ShowWindow(DomainDefinition domain)
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<DomainEditorWindow>())
                if (w.m_Domain == domain)
                {
                    w.Focus();
                    if (focusedWindow != w)
                        w.Show();
                    return w;
                }

            var window = CreateInstance<DomainEditorWindow>();
            window.Initialize(domain);
            window.Show();

            return window;
        }

        void OnEnable()
        {
            if (m_Domain)
                Initialize(m_Domain);
        }

        void Initialize(DomainDefinition domain)
        {
            m_ItemLookup = new Dictionary<BoardField, INamedData>();

            m_Domain = domain;
            m_Domain.definitionChanged += OnDefinitionChanged;

            m_Target = new SerializedObject(m_Domain);
            m_SelectedItem = null;

            var title = "AI Domain Definition";
            titleContent = new GUIContent(title);
            var root = rootVisualElement;

            root.Clear();

            var container = new VisualElement();
            container.StretchToParentSize();
            container.style.flexDirection = FlexDirection.Row;

            m_Board = new Board();
            m_Board.scrollable = true;
            m_Board.title = domain.name;
            m_Board.subTitle = title;
            m_Board.addItemRequested = OnAddItemRequested;
            m_Board.editTextRequested = OnEditTextRequested;
            m_Board.deleteItemRequested = OnDeleteItemRequested;

            var exportButton = new Button(() => m_Domain.GenerateClasses());
            exportButton.text = "Generate Classes";
            m_Board.Add(exportButton);

            m_Enumerations = new BoardSection { title = "ENUMERATIONS" };
            m_Board.Add(m_Enumerations);

            m_Traits = new BoardSection { title = "TRAITS" };
            m_Board.Add(m_Traits);

            m_Aliases = new BoardSection { title = "ALIASES" };
            m_Board.Add(m_Aliases);

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

            foreach (var item in m_Domain.EnumDefinitions)
            {
                var field = new BoardField(item.Name, null, boardField => SelectItem(item));
                field.AddToClassList("enum");

                m_ItemLookup[field] = item;
                m_Enumerations.Add(field);
            }

            foreach (var item in m_Domain.TraitDefinitions)
            {
                var field = new BoardField(item.Name, null, boardField => SelectItem(item));
                field.AddToClassList("trait");

                m_ItemLookup[field] = item;
                m_Traits.Add(field);
            }

            foreach (var item in m_Domain.AliasDefinitions)
            {
                var field = new BoardField(item.Name, null, boardField => SelectItem(item));
                field.AddToClassList("alias");

                m_ItemLookup[field] = item;
                m_Aliases.Add(field);
            }
        }

        void OnDefinitionChanged()
        {
            m_Target.Update();
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
                case nameof(EnumDefinition):
                    section = "Enumeration";
                    break;

                case nameof(TraitDefinition):
                    section = "Trait";
                    break;

                case nameof(AliasDefinition):
                    section = "Alias";
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
                case nameof(EnumDefinition):
                {
                    var index = m_Domain.EnumDefinitions.ToList().IndexOf((EnumDefinition)item);
                    propertyPath = $"m_{nameof(DomainDefinition.EnumDefinitions)}.Array.data[{index}]";
                    break;
                }

                case nameof(TraitDefinition):
                {
                    var index = m_Domain.TraitDefinitions.ToList().IndexOf((TraitDefinition)item);
                    propertyPath = $"m_{nameof(DomainDefinition.TraitDefinitions)}.Array.data[{index}]";
                    break;
                }

                case nameof(AliasDefinition):
                {
                    var index = m_Domain.AliasDefinitions.ToList().IndexOf((AliasDefinition)item);
                    propertyPath = $"m_{nameof(DomainDefinition.AliasDefinitions)}.Array.data[{index}]";
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
                    if (propertyPath.Contains(nameof(EnumDefinition)))
                        return m_Domain.EnumDefinitions.ElementAt(index);

                    if (propertyPath.Contains(nameof(TraitDefinition)))
                        return m_Domain.TraitDefinitions.ElementAt(index);

                    if (propertyPath.Contains(nameof(AliasDefinition)))
                        return m_Domain.AliasDefinitions.ElementAt(index);
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

        void OnAddItemRequested(Board board)
        {
            var menu = new GenericMenu();

            menu.AddItem(EditorGUIUtility.TrTextContent("Add Enumeration"), false, OnMenuItemTriggered, ItemType.Enumeration);
            menu.AddItem(EditorGUIUtility.TrTextContent("Add Trait"), false, OnMenuItemTriggered, ItemType.Trait);
            if (m_Domain.TraitDefinitions.Any())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Add Alias"), false, OnMenuItemTriggered, ItemType.Alias);
            }

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
                case ItemType.Enumeration:
                    var enumDefinitions = m_Domain.EnumDefinitions.ToList();
                    item = new EnumDefinition { Name = field.text };
                    enumDefinitions.Add((EnumDefinition)item);
                    m_Domain.EnumDefinitions = enumDefinitions;
                    m_Enumerations.Add(field);

                    break;

                case ItemType.Trait:
                    var traitDefinitions = m_Domain.TraitDefinitions.ToList();
                    item = new TraitDefinition { Name = field.text };
                    traitDefinitions.Add((TraitDefinition)item);
                    m_Domain.TraitDefinitions = traitDefinitions;
                    m_Traits.Add(field);
                    break;

                case ItemType.Alias:
                    var aliasDefinitions = m_Domain.AliasDefinitions.ToList();
                    item = new AliasDefinition { Name = field.text };
                    aliasDefinitions.Add((AliasDefinition)item);
                    m_Domain.AliasDefinitions = aliasDefinitions;
                    m_Aliases.Add(field);
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
                var enumDefinition = item as EnumDefinition;
                var traitDefinition = item as TraitDefinition;
                var aliasDefinition = item as AliasDefinition;

                if (enumDefinition != null)
                {
                    var enumDefinitions = m_Domain.EnumDefinitions.ToList();
                    enumDefinitions.Remove(enumDefinition);
                    m_Domain.EnumDefinitions = enumDefinitions;

                    m_Enumerations.Remove(key);
                }
                else if (traitDefinition != null)
                {
                    var traitDefinitions = m_Domain.TraitDefinitions.ToList();
                    traitDefinitions.Remove(traitDefinition);
                    m_Domain.TraitDefinitions = traitDefinitions;

                    m_Traits.Remove(key);
                }
                else if (aliasDefinition != null)
                {
                    var aliasDefinitions = m_Domain.AliasDefinitions.ToList();
                    aliasDefinitions.Remove(aliasDefinition);
                    m_Domain.AliasDefinitions = aliasDefinitions;

                    m_Aliases.Remove(key);
                }

                var namedData = item as INamedData;
                if (namedData != null)
                {
                    AssetDatabase.DeleteAsset($"{m_Domain.GeneratedClassDirectory}{namedData.Name}.cs");
                    AssetDatabase.DeleteAsset($"{m_Domain.GeneratedClassDirectory}{namedData.Name}.Extra.cs");
                }

                SelectItem(null);
                m_ItemLookup.Remove(key);
                m_Target.Update();
            }
        }
    }
}
