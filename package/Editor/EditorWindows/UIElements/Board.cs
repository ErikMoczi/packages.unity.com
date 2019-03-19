using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AI.Planner.UI
{
    class Board : VisualElement
    {
        VisualElement m_MainContainer;
        VisualElement m_Root;
        Label m_TitleLabel;
        Label m_SubTitleLabel;
        ScrollView m_ScrollView;
        VisualElement m_ContentContainer;
        VisualElement m_HeaderItem;
        Button m_AddButton;
        bool m_Scrollable = true;

        public Action<Board> addItemRequested { get; set; }
        public Action<Board, int, VisualElement> moveItemRequested { get; set; }
        public Action<Board, VisualElement, string> editTextRequested { get; set; }
        public Action<Board, VisualElement> deleteItemRequested { get; set; }

        public string title
        {
            get => m_TitleLabel.text;
            set => m_TitleLabel.text = value;
        }

        public string subTitle
        {
            get => m_SubTitleLabel.text;
            set => m_SubTitleLabel.text = value;
        }

        public override VisualElement contentContainer => m_ContentContainer;

        public bool scrollable
        {
            get => m_Scrollable;
            set
            {
                if (m_Scrollable == value)
                    return;

                m_Scrollable = value;

                if (m_Scrollable)
                {
                    if (m_ScrollView == null)
                        m_ScrollView = new ScrollView();

                    // Remove the sections container from the content item and add it to the scrollview
                    m_ContentContainer.RemoveFromHierarchy();
                    m_Root.Add(m_ScrollView);
                    m_ScrollView.Add(m_ContentContainer);

                    AddToClassList("scrollable");
                }
                else
                {
                    if (m_ScrollView != null)
                    {
                        m_ScrollView.RemoveFromHierarchy();
                        m_ContentContainer.RemoveFromHierarchy();
                        m_Root.Add(m_ContentContainer);
                    }
                    RemoveFromClassList("scrollable");
                }
            }
        }

        public Vector2 scrollPosition
        {
            get => m_ScrollView != null ? m_ScrollView.scrollOffset : Vector2.zero;
            set
            {
                if (m_ScrollView != null)
                {
                    var slider = m_ScrollView.horizontalScroller.slider;
                    if (slider.highValue < value.x)
                        slider.highValue = value.x;
                    slider = m_ScrollView.verticalScroller.slider;
                    if (slider.highValue < value.y)
                        slider.highValue = value.y;

                    m_ScrollView.scrollOffset = value;
                }
            }
        }

        public Board()
        {
            var resources = ScriptableSingleton<BoardResources>.instance;
            var tpl = resources.BoardUxml;
            styleSheets.Add(resources.BoardStyleSheet);

            m_MainContainer = tpl.CloneTree();
            m_MainContainer.AddToClassList("mainContainer");

            m_Root = m_MainContainer.Q("content");

            m_HeaderItem = m_MainContainer.Q("header");
            m_HeaderItem.AddToClassList("blackboardHeader");

            m_AddButton = m_MainContainer.Q("addButton") as Button;
            m_AddButton.clickable.clicked += () =>
            {
                if (addItemRequested != null)
                    addItemRequested(this);
            };

            m_TitleLabel = m_MainContainer.Q<Label>("titleLabel");
            m_SubTitleLabel = m_MainContainer.Q<Label>("subTitleLabel");
            m_ContentContainer = m_MainContainer.Q<VisualElement>("contentContainer");

            hierarchy.Add(m_MainContainer);

            ClearClassList();
            AddToClassList("blackboard");

            scrollable = false;

            RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
            });
        }
    }
}
