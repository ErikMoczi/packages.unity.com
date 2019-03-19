using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AI.Planner.UI
{
    class BoardSection : VisualElement
    {
        VisualElement m_DragIndicator;
        VisualElement m_MainContainer;
        VisualElement m_Header;
        Label m_TitleLabel;
        VisualElement m_RowsContainer;
//        int m_InsertIndex;

        int InsertionIndex(Vector2 pos)
        {
            int index = -1;
            var owner = contentContainer != null ? contentContainer : this;
            var localPos = this.ChangeCoordinatesTo(owner, pos);

            if (owner.ContainsPoint(localPos))
            {
                index = 0;

                foreach (var child in Children())
                {
                    var rect = child.layout;

                    if (localPos.y > rect.y + rect.height / 2)
                        ++index;
                    else
                        break;
                }
            }

            return index;
        }

        VisualElement FindSectionDirectChild(VisualElement element)
        {
            var directChild = element;

            while (directChild != null && directChild != this)
            {
                if (directChild.parent == this)
                {
                    return directChild;
                }
                directChild = directChild.parent;
            }

            return null;
        }

        public BoardSection()
        {
            var resources = ScriptableSingleton<BoardResources>.instance;
            var tpl = resources.BoardSectionUxml;
            styleSheets.Add(resources.BoardStyleSheet);
            m_MainContainer = tpl.CloneTree();
            m_MainContainer.AddToClassList("mainContainer");

            m_Header = m_MainContainer.Q<VisualElement>("sectionHeader");
            m_TitleLabel = m_MainContainer.Q<Label>("sectionTitleLabel");
            m_RowsContainer = m_MainContainer.Q<VisualElement>("rowsContainer");

            hierarchy.Add(m_MainContainer);

            m_DragIndicator = new VisualElement();

            m_DragIndicator.name = "dragIndicator";
            m_DragIndicator.style.position = Position.Absolute;
            hierarchy.Add(m_DragIndicator);

            ClearClassList();
            AddToClassList("blackboardSection");

//            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
//            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
//            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);

//            m_InsertIndex = -1;
        }

        public override VisualElement contentContainer => m_RowsContainer;

        public string title
        {
            get => m_TitleLabel.text;
            set => m_TitleLabel.text = value;
        }

        public bool headerVisible
        {
            get => m_Header.parent != null;
            set
            {
                if (value == (m_Header.parent != null))
                    return;

                if (value)
                    m_MainContainer.Add(m_Header);
                else
                    m_MainContainer.Remove(m_Header);
            }
        }

        void SetDragIndicatorVisible(bool visible)
        {
            if (visible && m_DragIndicator.parent == null)
            {
                hierarchy.Add(m_DragIndicator);
                m_DragIndicator.visible = true;
            }
            else if (visible == false && m_DragIndicator.parent != null)
            {
                hierarchy.Remove(m_DragIndicator);
            }
        }
    }
}
