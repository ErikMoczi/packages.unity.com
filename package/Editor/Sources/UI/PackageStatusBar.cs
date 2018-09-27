using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class PackageStatusBarFactory : UxmlFactory<PackageStatusBar>
    {
        protected override PackageStatusBar DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageStatusBar();
        }
    }
#endif
    
    internal class PackageStatusBar : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<PackageStatusBar> { }
#endif

        private readonly VisualElement root;
        private string LastErrorMessage;
        private string LastUpdateTime;

        private List<IBaseOperation> operationsInProgress;

        private enum StatusType {Normal, Loading, Error};  

        public event Action OnCheckInternetReachability = delegate { };

        public PackageStatusBar()
        {
            root = Resources.GetTemplate("PackageStatusBar.uxml");
            Add(root);

            MoreAddOptionsButton.clickable.clicked += OnMoreAddOptionsButtonClick;

            LastErrorMessage = string.Empty;
            operationsInProgress = new List<IBaseOperation>();
        }

        public void SetDefaultMessage(string lastUpdateTime)
        {
            LastUpdateTime = lastUpdateTime;
            if(!string.IsNullOrEmpty(LastUpdateTime))
                SetStatusMessage(StatusType.Normal, "Last update " + LastUpdateTime);
            else
                SetStatusMessage(StatusType.Normal, string.Empty);
        }

        internal void OnListOrSearchOperation(IBaseOperation operation)
        {
            if (operation == null || operation.IsCompleted)
                return;
            operationsInProgress.Add(operation);
            operation.OnOperationFinalized += () => { OnOperationFinalized(operation); };
            operation.OnOperationError += OnOperationError;

            SetStatusMessage(StatusType.Loading, "Loading packages...");
        }

        private void OnOperationFinalized(IBaseOperation operation)
        {
            operationsInProgress.Remove(operation);

            if (operationsInProgress.Any()) return;

            var errorMessage = LastErrorMessage;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                EditorApplication.update -= CheckInternetReachability;
                EditorApplication.update += CheckInternetReachability;

                errorMessage = "You seem to be offline.";
            }

            if (!string.IsNullOrEmpty(errorMessage))
                SetStatusMessage(StatusType.Error, errorMessage);
            else
                SetDefaultMessage(LastUpdateTime);
        }

        private void OnOperationError(Error error)
        {
            LastErrorMessage = "Cannot load packages, see console.";
        }

        private void CheckInternetReachability()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable) 
                return;

            OnCheckInternetReachability();
            EditorApplication.update -= CheckInternetReachability;
        }

        private void SetStatusMessage(StatusType status, string message)
        {
            if (status == StatusType.Loading)
                LoadingSpinner.Start();
            else
                LoadingSpinner.Stop();

            UIUtils.SetElementDisplay(LoadingIcon, status == StatusType.Error);
            if (status == StatusType.Error)
                LoadingText.AddToClassList("icon");
            else
                LoadingText.RemoveFromClassList("icon");

            LoadingText.text = message;
        }

        private void OnMoreAddOptionsButtonClick()
        {
            var menu = new GenericMenu();

            var addPackageFromDiskItem = new GUIContent("Add package from disk...");

            menu.AddItem(addPackageFromDiskItem, false, delegate
            {
                var path = EditorUtility.OpenFilePanelWithFilters("Select package on disk", "", new[] { "package.json file", "json" });
                if (!string.IsNullOrEmpty(path) && !Package.AddRemoveOperationInProgress)
                    Package.AddFromLocalDisk(path);
            });
            var menuPosition = MoreAddOptionsButton.LocalToWorld(new Vector2(MoreAddOptionsButton.layout.width, 0));
            var menuRect = new Rect(menuPosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private LoadingSpinner _loadingSpinner;
        private LoadingSpinner LoadingSpinner { get { return _loadingSpinner ?? (_loadingSpinner = root.Q<LoadingSpinner>("packageSpinner")); }}

        private Label _loadingIcon;
        private Label LoadingIcon { get { return _loadingIcon ?? (_loadingIcon = root.Q<Label>("loadingIcon")); }}

        private Label _loadingText;
        private Label LoadingText { get { return _loadingText ?? (_loadingText = root.Q<Label>("loadingText")); }}

        private Button _moreAddOptionsButton;
        private Button MoreAddOptionsButton{ get { return _moreAddOptionsButton ?? (_moreAddOptionsButton = root.Q<Button>("moreAddOptionsButton")); }}
    }
}