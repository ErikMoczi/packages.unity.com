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

            LastErrorMessage = string.Empty;
            operationsInProgress = new List<IBaseOperation>();
        }

        public void Setup(string lastUpdateTime)
        {
            LastUpdateTime = lastUpdateTime;
            UpdateStatusMessage();
        }

        public void SetUpdateTimeMessage(string lastUpdateTime)
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
            UpdateStatusMessage();
        }

        private void UpdateStatusMessage()
        {
            var errorMessage = LastErrorMessage;
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                EditorApplication.update -= CheckInternetReachability;
                EditorApplication.update += CheckInternetReachability;
                errorMessage = "You seem to be offline";
            }

            if (!string.IsNullOrEmpty(errorMessage))
                SetStatusMessage(StatusType.Error, errorMessage);
            else
                SetUpdateTimeMessage(LastUpdateTime);
        }

        private void OnOperationError(Error error)
        {
            LastErrorMessage = "Cannot load packages, see console";
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
            {
                LoadingSpinnerContainer.AddToClassList("loading");
                LoadingSpinner.Start();
            }
            else
            {
                LoadingSpinner.Stop();
                LoadingSpinnerContainer.RemoveFromClassList("loading");
            }

            UIUtils.SetElementDisplay(ErrorIcon, status == StatusType.Error);
            StatusLabel.text = message;
        }

        private VisualElement _loadingSpinnerContainer;
        private VisualElement LoadingSpinnerContainer { get { return _loadingSpinnerContainer ?? (_loadingSpinnerContainer = root.Q<VisualElement>("loadingSpinnerContainer")); }}

        private LoadingSpinner _loadingSpinner;
        private LoadingSpinner LoadingSpinner { get { return _loadingSpinner ?? (_loadingSpinner = root.Q<LoadingSpinner>("packageSpinner")); }}

        private Label _errorIcon;
        private Label ErrorIcon { get { return _errorIcon ?? (_errorIcon = root.Q<Label>("errorIcon")); }}

        private Label _statusLabel;
        private Label StatusLabel { get { return _statusLabel ?? (_statusLabel = root.Q<Label>("statusLabel")); }}
    }
}