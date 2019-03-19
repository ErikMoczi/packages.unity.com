using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AI.Planner.UI
{
    class BoardRow : VisualElement
    {
        VisualElement m_Root;
        Button m_ExpandButton;
        VisualElement m_ItemContainer;
        VisualElement m_PropertyViewContainer;
        bool m_Expanded = true;

        public bool expanded
        {
            get => m_Expanded;
            set
            {
                if (m_Expanded == value)
                {
                    return;
                }

                m_Expanded = value;

                if (m_Expanded)
                {
                    m_Root.Add(m_PropertyViewContainer);
                    AddToClassList("expanded");
                }
                else
                {
                    m_Root.Remove(m_PropertyViewContainer);
                    RemoveFromClassList("expanded");
                }
            }
        }

        public BoardRow(VisualElement item, VisualElement propertyView)
        {
            var resources = ScriptableSingleton<BoardResources>.instance;
            var tpl = resources.BoardRowUxml;
            styleSheets.Add(resources.BoardStyleSheet);

            VisualElement mainContainer = tpl.CloneTree();

            mainContainer.AddToClassList("mainContainer");

            m_Root = mainContainer.Q<VisualElement>("root");
            m_ItemContainer = mainContainer.Q<VisualElement>("itemContainer");
            m_PropertyViewContainer = mainContainer.Q<VisualElement>("propertyViewContainer");

            m_ExpandButton = mainContainer.Q<Button>("expandButton");
            m_ExpandButton.clickable.clicked += () => expanded = !expanded;

            Add(mainContainer);

            ClearClassList();
            AddToClassList("blackboardRow");

            m_ItemContainer.Add(item);
            if (propertyView != null)
                m_PropertyViewContainer.Add(propertyView);

            expanded = false;
        }
    }
}
