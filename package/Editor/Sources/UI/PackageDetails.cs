using System.Linq;
using Semver;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageDetailsFactory : UxmlFactory<PackageDetails>
    {
        protected override PackageDetails DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageDetails();
        }
    }

    internal class PackageDetails : VisualElement
    {
        private readonly VisualElement root;
        private Package package;
        private PackageFilter filter;
        private const string emptyId = "emptyArea";
        private const string emptyDescriptionClass = "empty";

        public PackageDetails()
        {
            root = Resources.Load<VisualTreeAsset>("Templates/PackageDetails").CloneTree(null);
            Add(root);
            root.StretchToParentSize();

            UpdateButton.clickable.clicked += UpdateClick;
            RemoveButton.clickable.clicked += RemoveClick;
            if (ViewDocButton != null) 
                ViewDocButton.clickable.clicked += ViewDocClick;
        }

        public void SetPackage(Package package, PackageFilter filter)
        {
            if (this.package != null)
            {
                if (this.package.AddSignal.Operation != null)
                {
                    this.package.AddSignal.Operation.OnOperationError -= OnAddOperationError;
                    this.package.AddSignal.Operation.OnOperationSuccess -= OnAddOperationSuccess;
                }
                this.package.AddSignal.ResetEvents();

                if (this.package.RemoveSignal.Operation != null)
                {
                    this.package.RemoveSignal.Operation.OnOperationError -= OnRemoveOperationError;
                }
                this.package.RemoveSignal.ResetEvents();
            }
            
            this.filter = filter;
            this.package = package;
            var detailVisible = true;

            if (package == null || package.Display == null)
            {
                detailVisible = false;
            }
            else
            {
                var displayPackage = package.Display;
                
                if (string.IsNullOrEmpty(displayPackage.Description))
                {
                    DetailDesc.text = "There is no description for this package.";
                    DetailDesc.AddToClassList(emptyDescriptionClass);
                }
                else
                {
                    DetailDesc.text = displayPackage.Description;                    
                    DetailDesc.RemoveFromClassList(emptyDescriptionClass);
                }

                root.Q<Label>("detailTitle").text = displayPackage.DisplayName;
                root.Q<Label>("detailVersion").text = "Version " + displayPackage.VersionWithoutTag;
                
                if(displayPackage.IsInPreview)
                    root.Q<Label>("inPreview").RemoveFromClassList("display-none");
                else
                    root.Q<Label>("inPreview").AddToClassList("display-none");
                root.Q<Label>("detailName").text = displayPackage.Name;
                root.Q<ScrollView>("detailView").scrollOffset = new Vector2(0, 0);

                RefreshAddButton();
                RefreshRemoveButton();

                this.package.AddSignal.OnOperation += OnAddOperation;
                this.package.RemoveSignal.OnOperation += OnRemoveOperation;
            }

            // Set visibility
            root.Q<VisualContainer>("detail").visible = detailVisible;
            root.Q<VisualContainer>(emptyId).visible = !detailVisible;
            
            DetailError.ClearError();
        }

        private void OnAddOperation(IAddOperation operation)
        {
            operation.OnOperationError += OnAddOperationError;
            operation.OnOperationSuccess += OnAddOperationSuccess;
        }

        private void OnAddOperationError(Error error)
        {
            if (package != null && package.AddSignal.Operation != null)
            {
                package.AddSignal.Operation.OnOperationSuccess -= OnAddOperationSuccess;
                package.AddSignal.Operation.OnOperationError -= OnAddOperationError;
                package.AddSignal.Operation = null;
            }
            
            DetailError.AdjustSize(DetailView.verticalScroller.visible);
            DetailError.SetError(error);
            RefreshAddButton();
        }

        private void OnAddOperationSuccess(PackageInfo packageInfo)
        {
            if (package != null && package.AddSignal.Operation != null)
            {
                package.AddSignal.Operation.OnOperationSuccess -= OnAddOperationSuccess;
                package.AddSignal.Operation.OnOperationError -= OnAddOperationError;
            }
            
            PackageCollection.Instance.SetFilter(PackageFilter.Local);
        }

        private void OnRemoveOperation(IRemoveOperation operation)
        {
            operation.OnOperationError += OnRemoveOperationError;
        }

        private void OnRemoveOperationError(Error error)
        {
            package.RemoveSignal.Operation.OnOperationError -= OnRemoveOperationError;
            package.RemoveSignal.Operation = null;
            
            DetailError.AdjustSize(DetailView.verticalScroller.visible);
            DetailError.SetError(error);
            RefreshRemoveButton();
        }

        private void RefreshAddButton()
        {
            var displayPackage = package.Display;
            var visibleFlag = false;
            var actionLabel = "";
            SemVersion version;
            var enableButton = true;

            if (package.AddSignal.Operation != null)
            {
                version = package.AddSignal.Operation.PackageInfo.Version;
                actionLabel = GetUpdateButtonText(filter == PackageFilter.All ? "Adding" : "Updating to", version);
                enableButton = false;
                visibleFlag = true;
            }
            else if (displayPackage.IsCurrent && package.Latest.Version != package.Current.Version)
            {
                version = package.Latest.Version;
                actionLabel = GetUpdateButtonText("Update to", version);
                visibleFlag = true;
            }
            else if (package.Current == null && package.Versions.Any())
            {
                visibleFlag = true;
                version = package.Latest.Version;
                actionLabel = GetUpdateButtonText("Add", version);
            }

            UpdateButton.SetEnabled(enableButton);
            UpdateButton.text = actionLabel;   
            UIUtils.SetElementDisplay(UpdateButton, visibleFlag);
        }

        private void RefreshRemoveButton()
        {
            var visibleFlag = false;
            var actionLabel = "Remove";
            var enableButton = false;

            if (filter != PackageFilter.All)
            {
                enableButton = package.CanBeRemoved;
                
                visibleFlag = true;
                if (package.RemoveSignal.Operation != null)
                {
                    actionLabel = "Removing";
                    enableButton = false;
                }
            }
            
            RemoveButton.SetEnabled(enableButton);
            RemoveButton.text = actionLabel;   
            UIUtils.SetElementDisplay(RemoveButton, visibleFlag);
        }

        private static string GetUpdateButtonText(string action, SemVersion version)
        {
            return string.Format("{0} {1}", action, version);
        }

        private void UpdateClick()
        {
            DetailError.ClearError();
            package.Update();
            RefreshAddButton();
        }

        private void RemoveClick()
        {
            DetailError.ClearError();
            package.Remove();
            RefreshRemoveButton();
        }

        private void ViewDocClick()
        {
            Application.OpenURL(package.DocumentationLink);
        }

        private Label DetailDesc { get { return root.Q<Label>("detailDesc"); } }
        private Button UpdateButton { get { return root.Q<Button>("update"); } }
        private Button RemoveButton { get { return root.Q<Button>("remove"); } }
        private Button ViewDocButton { get { return root.Q<Button>("viewDocumentation"); } }
        private Alert DetailError { get { return root.Q<Alert>("detailError"); } }
        private ScrollView DetailView { get { return root.Q<ScrollView>("detailView"); } }
    }
}
