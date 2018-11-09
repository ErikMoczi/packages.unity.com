using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class PackageSearchToolbarFactory : UxmlFactory<PackageSearchToolbar>
    {
        protected override PackageSearchToolbar DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageSearchToolbar();
        }
    }
#endif

    internal class PackageSearchToolbar : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<PackageSearchToolbar> {}
#endif
        private const string kPlaceHolder = "Search by package name, verified, preview or version number...";

        public event Action OnFocusChange = delegate {};
        public event Action<string> OnSearchChange = delegate {};

        private string searchText;
        private bool showingPlaceHolder;

        private readonly VisualElement root;

        public PackageSearchToolbar()
        {
            root = Resources.GetTemplate("PackageSearchToolbar.uxml");
            Add(root);
            root.StretchToParentSize();
            Cache = new VisualElementCache(root);

            SearchTextField.maxLength = 64;
            SearchTextField.name = "";
            SearchCancelButton.clickable.clicked += SearchCancelButtonClick;

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        public void GrabFocus()
        {
            SearchTextField.Focus();
        }

        public new void SetEnabled(bool enable)
        {
            base.SetEnabled(enable);
            SearchTextField.SetEnabled(enable);
            SearchCancelButton.SetEnabled(enable);
        }

        public void SetSearchText(string text)
        {
            searchText = text;
            if (string.IsNullOrEmpty(searchText))
            {
                showingPlaceHolder = true;
                SearchTextField.value = kPlaceHolder;
                SearchTextField.AddToClassList("placeholder");
            }
            else
            {
                showingPlaceHolder = false;
                SearchTextField.value = searchText;
                SearchTextField.RemoveFromClassList("placeholder");
            }
        }

        private void OnSearchTextFieldChange(ChangeEvent<string> evt)
        {
            if (showingPlaceHolder && evt.newValue == kPlaceHolder)
                return;
            if (!string.IsNullOrEmpty(evt.newValue))
                SearchCancelButton.AddToClassList("on");
            else
                SearchCancelButton.RemoveFromClassList("on");

            searchText = evt.newValue;
            OnSearchChange(searchText);
        }

        private void OnSearchTextFieldFocus(FocusEvent evt)
        {
            if (showingPlaceHolder)
            {
                SearchTextField.value = string.Empty;
                SearchTextField.RemoveFromClassList("placeholder");
                showingPlaceHolder = false;
            }
        }

        private void OnSearchTextFieldFocusOut(FocusOutEvent evt)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                showingPlaceHolder = true;
                SearchTextField.AddToClassList("placeholder");
                SearchTextField.value = kPlaceHolder;
            }
        }

        private void SearchCancelButtonClick()
        {
            if (!string.IsNullOrEmpty(SearchTextField.value))
            {
                SearchTextField.value = string.Empty;
            }

            showingPlaceHolder = true;
            SearchTextField.AddToClassList("placeholder");
            SearchTextField.value = kPlaceHolder;
        }

        private void OnEnterPanel(AttachToPanelEvent evt)
        {
            SearchTextField.Q("unity-text-input").RegisterCallback<FocusEvent>(OnSearchTextFieldFocus);
            SearchTextField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(OnSearchTextFieldFocusOut);
            SearchTextField.RegisterCallback<ChangeEvent<string>>(OnSearchTextFieldChange);
            SearchTextField.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnLeavePanel(DetachFromPanelEvent evt)
        {
            SearchTextField.Q("unity-text-input").UnregisterCallback<FocusEvent>(OnSearchTextFieldFocus);
            SearchTextField.Q("unity-text-input").UnregisterCallback<FocusOutEvent>(OnSearchTextFieldFocusOut);
            SearchTextField.UnregisterCallback<ChangeEvent<string>>(OnSearchTextFieldChange);
            SearchTextField.Q("unity-text-input").UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                SearchCancelButtonClick();
                SearchCancelButton.Focus();
                evt.StopImmediatePropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Tab)
            {
                OnFocusChange();
                evt.StopImmediatePropagation();
            }
        }

        private VisualElementCache Cache { get; set; }

        private TextField SearchTextField { get { return Cache.Get<TextField>("searchTextField"); } }
        private Button SearchCancelButton { get { return Cache.Get<Button>("searchCancelButton"); } }
    }
}
