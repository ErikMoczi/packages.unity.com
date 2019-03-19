using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AI.Planner.UI
{
    class BoardField : VisualElement
    {
        VisualElement m_ContentItem;
        TextField m_TextField;
        VisualElement m_TextInput;
        Label m_TextLabel;
        Label m_TypeLabel;
        bool m_EditCancelled;
        event Action<BoardField> selected;

        public string text
        {
            get => m_TextLabel.text;
            set => m_TextLabel.text = value;
        }

        public string typeText
        {
            get => m_TypeLabel.text;
            set => m_TypeLabel.text = value;
        }

        public BoardField(string text, string typeText, Action<BoardField> selectedCallback)
        {
            var resources = ScriptableSingleton<BoardResources>.instance;
            var tpl = resources.BoardFieldUxml;
            VisualElement mainContainer = tpl.CloneTree();
            styleSheets.Add(resources.BoardStyleSheet);
            mainContainer.AddToClassList("mainContainer");
            mainContainer.pickingMode = PickingMode.Ignore;

            m_ContentItem = mainContainer.Q("contentItem");

            m_TextLabel = mainContainer.Q<Label>("textLabel");
            m_TypeLabel = mainContainer.Q<Label>("typeLabel");

            m_TextField = mainContainer.Q<TextField>("textField");
            m_TextField.delegatesFocus = true;
            m_TextField.visible = false;
            m_TextInput = m_TextField.Q("unity-text-input");
            m_TextInput.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); });
            m_TextInput.RegisterCallback<KeyDownEvent>(OnTextFieldKeyPressed);

            Add(mainContainer);

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            RegisterCallback<MouseUpEvent>(OnMouseUpEvent);

            ClearClassList();
            AddToClassList("blackboardField");

            this.text = text;
            this.typeText = typeText;
            selected += selectedCallback;

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        void OnTextFieldKeyPressed(KeyDownEvent e)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    m_EditCancelled = true;
                    m_TextField.Blur();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    m_TextField.Blur();
                    break;
            }
        }

        void OnEditTextFinished()
        {
            m_ContentItem.visible = true;
            m_TextField.visible = false;

            if (!m_EditCancelled && text != m_TextField.text)
            {
                var blackboard = GetFirstAncestorOfType<Board>();

                if (blackboard.editTextRequested != null)
                    blackboard.editTextRequested(blackboard, this, m_TextField.text);
                else
                    text = m_TextField.text;
            }

            m_EditCancelled = false;
        }

        void OnMouseUpEvent(MouseUpEvent e)
        {
            if (e.clickCount == 1 && e.button == (int)MouseButton.LeftMouse)
                if (selected != null)
                    selected(this);
        }

        void OnMouseDownEvent(MouseDownEvent e)
        {
            if (e.clickCount == 2 && e.button == (int)MouseButton.LeftMouse)
            {
                OpenTextEditor();
                e.PreventDefault();
            }
        }

        public void OpenTextEditor()
        {
            m_TextField.value = text;

            m_ContentItem.visible = false;

            m_TextField.visible = true;
            m_TextInput.visible = true;

            m_TextField.Focus();
            m_TextField.SelectAll();
        }

        void OnDelete(DropdownMenuAction menuAction)
        {
            var board = GetFirstAncestorOfType<Board>();

            if (board.deleteItemRequested != null)
                board.deleteItemRequested(board, this);
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Rename", a => OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Delete", OnDelete, DropdownMenuAction.AlwaysEnabled);
        }
    }
}
