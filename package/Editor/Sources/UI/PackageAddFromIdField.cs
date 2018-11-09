using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class PackageAddFromIdFieldFactory : UxmlFactory<PackageAddFromIdField>
    {
        protected override PackageAddFromIdField DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageAddFromIdField();
        }
    }
#endif

    internal class PackageAddFromIdField : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<PackageAddFromIdField> {}
#endif
        private string idText;

        private readonly VisualElement root;

        public PackageAddFromIdField()
        {
            root = Resources.GetTemplate("PackageAddFromIdField.uxml");
            Add(root);

            idTextField.value = idText;

            AddButton.SetEnabled(!string.IsNullOrEmpty(idText));
            AddButton.RegisterCallback<MouseDownEvent>((MouseDownEvent evt) =>
            {
                OnAddButtonClick();
                evt.StopPropagation();
            });

            AddFromIdFieldContainer.RegisterCallback<MouseDownEvent>(OnContainerMouseDown);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        private void OnidTextFieldChange(ChangeEvent<string> evt)
        {
            idText = evt.newValue;
            AddButton.SetEnabled(!string.IsNullOrEmpty(idText));
        }

        private void Refocus()
        {
            Show();
            idTextField.Focus();
            EditorApplication.update -= Refocus;
        }

        private void OnidTextFieldFocusOut(FocusOutEvent evt)
        {
            Hide();
        }

        private void OnContainerMouseDown(MouseDownEvent evt)
        {
            EditorApplication.update -= Refocus;
            EditorApplication.update += Refocus;
        }

        private void OnEnterPanel(AttachToPanelEvent evt)
        {
            idTextField.RegisterCallback<FocusOutEvent>(OnidTextFieldFocusOut);
            idTextField.RegisterCallback<ChangeEvent<string>>(OnidTextFieldChange);
            idTextField.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            Hide();
        }

        private void OnLeavePanel(DetachFromPanelEvent evt)
        {
            idTextField.UnregisterCallback<FocusOutEvent>(OnidTextFieldFocusOut);
            idTextField.UnregisterCallback<ChangeEvent<string>>(OnidTextFieldChange);
            idTextField.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Hide();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    OnAddButtonClick();
                    break;
            }
        }

        private void OnAddButtonClick()
        {
            if (!string.IsNullOrEmpty(idText) && !Package.AddRemoveOperationInProgress)
            {
                Package.AddFromId(idText);
                Hide();
                EditorApplication.update -= Refocus;
            }
        }

        internal void Hide()
        {
            UIUtils.SetElementDisplay(this, false);
        }

        internal void Show(bool reset = false)
        {
            if (reset)
                Reset();
            UIUtils.SetElementDisplay(this, true);
        }

        private void Reset()
        {
            idTextField.value = string.Empty;
            idText = string.Empty;
            AddButton.SetEnabled(false);
            idTextField.Focus();
        }

        private VisualElement _addFromIdFieldContainer;
        private VisualElement AddFromIdFieldContainer { get { return _addFromIdFieldContainer ?? (_addFromIdFieldContainer = root.Q<VisualElement>("addFromIdFieldContainer")); } }

        private TextField _idTextField;
        private TextField idTextField { get { return _idTextField ?? (_idTextField = root.Q<TextField>("idTextField")); } }

        private Label _addButton;
        private Label AddButton { get { return _addButton ?? (_addButton = root.Q<Label>("addFromIdButton")); } }
    }
}
