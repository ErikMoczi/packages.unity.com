using System;
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
        internal static PackageTag[] SupportedTags()
        {
            return new PackageTag[] { PackageTag.preview };
        }

        private readonly VisualElement root;
        private Package package;
        private PackageFilter filter;
        private const string emptyId = "emptyArea";
        private const string emptyDescriptionClass = "empty";

        private enum PackageAction
        {
            Add,
            Remove,
            Update,
            Downgrade,
            Enable,
            Disable
        }
        
        private static readonly string[] PackageActionVerbs = { "Install", "Remove", "Update to", "Go back to",  "Enable", "Disable" };
        private static readonly string[] PackageActionInProgressVerbs = { "Installing", "Removing", "Updating to", "Going back to", "Enabling", "Disabling" };

        public PackageDetails()
        {
            root = Resources.Load<VisualTreeAsset>("Templates/PackageDetails").CloneTree(null);
            Add(root);
            root.StretchToParentSize();

            SetUpdateVisibility(false);
            RemoveButton.visible = false;
            root.Q<VisualElement>(emptyId).visible = false;

            UpdateButton.clickable.clicked += UpdateClick;
            RemoveButton.clickable.clicked += RemoveClick;
            ViewDocButton.clickable.clicked += ViewDocClick;
            ViewChangelogButton.clickable.clicked += ViewChangelogClick;
            ViewChangelogButton.parent.clippingOptions = ClippingOptions.NoClipping;
            ViewChangelogButton.parent.parent.clippingOptions = ClippingOptions.NoClipping;

            PackageCollection.Instance.OnFilterChanged += OnFilterChanged;
        }

        private void SetUpdateVisibility(bool value)
        {
            if (UpdateContainer != null)
                UIUtils.SetElementDisplay(UpdateContainer, value);
        }

        // Package version to display
        public PackageInfo Display(Package package)
        {
            return PackageCollection.Instance.Filter == PackageFilter.All || package.Current == null ? package.Latest : package.Current;
        }

        private void OnFilterChanged(PackageFilter obj)
        {
            root.Q<VisualElement>(emptyId).visible = false;
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
            Error error = null;

            if (package == null || Display(package) == null)
            {
                detailVisible = false;
                UIUtils.SetElementDisplay(ViewChangelogButton, false);
                UIUtils.SetElementDisplay(ViewDocButton, false);
            }
            else
            {
                SetUpdateVisibility(true);
                RemoveButton.visible = true;

                var displayPackage = Display(package);
                
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
                DetailVersion.text = "Version " + displayPackage.VersionWithoutTag;

                if (displayPackage.HasTag(PackageTag.preview))
                    UIUtils.SetElementDisplay(GetTag(PackageTag.verified), false);
                else
                {
                    var unityVersionParts = Application.unityVersion.Split('.');
                    var unityVersion = string.Format("{0}.{1}", unityVersionParts[0], unityVersionParts[1]);
                    VerifyLabel.text = unityVersion + " Verified";
                    UIUtils.SetElementDisplay(GetTag(PackageTag.verified), displayPackage.IsVerified);
                }

                foreach (var tag in SupportedTags())
                    UIUtils.SetElementDisplay(GetTag(tag), displayPackage.HasTag(tag));
                                
                if (Display(package).Origin == PackageOrigin.Builtin)
                {
                    UIUtils.SetElementDisplay(ViewChangelogButton, false);
                    UIUtils.SetElementDisplay(ViewDocButton, false);
                }
                else
                {
                    var currentVersion = package.Current;
                    var hasUpdate = currentVersion != null && displayPackage.Version.CompareByPrecedence(currentVersion.Version) > 0;
                    UIUtils.SetElementDisplay(ViewChangelogButton, displayPackage.IsCurrent || hasUpdate);
                    UIUtils.SetElementDisplay(ViewDocButton, true);
                }

                root.Q<Label>("detailName").text = displayPackage.Name;
                root.Q<ScrollView>("detailView").scrollOffset = new Vector2(0, 0);

                DetailModuleReference.text = "";
                var isModule = PackageInfo.IsModule(displayPackage.Name);
                if (PackageInfo.IsModule(displayPackage.Name))
                {
                    DetailModuleReference.text = displayPackage.Description;
                    if (string.IsNullOrEmpty(displayPackage.Description))
                        DetailModuleReference.text = string.Format("This built in package controls the presence of the {0} module.", displayPackage.ModuleName);
                }
                
                // Show Status string on package if need be
                DetailPackageStatus.text = string.Empty;
                if (!isModule)
                {
                    var displayPackageList = package.Current ?? package.Latest;
                    if (displayPackageList.State == PackageState.Outdated)
                    {
                        DetailPackageStatus.text =
                            "This package is installed for your project and has an available update.";
                    }
                    else if (displayPackageList.State == PackageState.InProgress)
                    {
                        DetailPackageStatus.text =
                            "This package is being updated or installed.";
                    }
                    else if (displayPackageList.State == PackageState.Error)
                    {
                        DetailPackageStatus.text =
                            "This package is in error, please check console logs for more details.";
                    }
                    else if (displayPackageList.IsCurrent)
                    {
                        DetailPackageStatus.text =
                            "This package is installed for your project.";
                    }
                    else
                    {
                        DetailPackageStatus.text =
                            "This package is not installed for your project.";
                    }
                }

                DetailAuthor.text = string.Format("Author: {0}", displayPackage.Author ?? "Unity Technologies Inc.");

                UIUtils.SetElementDisplay(DetailDesc, !isModule);
                UIUtils.SetElementDisplay(DetailVersion, !isModule);
                UIUtils.SetElementDisplayNonEmpty(DetailModuleReference);
                UIUtils.SetElementDisplayNonEmpty(DetailPackageStatus);
                UIUtils.SetElementDisplayNonEmpty(DetailAuthor);


                if (displayPackage.Errors.Count > 0)
                    error = displayPackage.Errors.First();

                RefreshAddButton();
                RefreshRemoveButton();

                this.package.AddSignal.OnOperation += OnAddOperation;
                this.package.RemoveSignal.OnOperation += OnRemoveOperation;
            }

            // Set visibility
            root.Q<VisualElement>("detail").visible = detailVisible;
            root.Q<VisualElement>(emptyId).visible = !detailVisible;

            if (error != null)
            {
                Debug.LogError("Error with package details: " + error.message);
                SetError(error);
            }
            else
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
            
            SetError(error);
            RefreshAddButton();
        }

        private void SetError(Error error)
        {
            DetailError.AdjustSize(DetailView.verticalScroller.visible);
            DetailError.SetError(error);            
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
            
            SetError(error);
            RefreshRemoveButton();
        }

        private void RefreshAddButton()
        {
            var displayPackage = Display(package);
            var visibleFlag = false;
            var actionLabel = "";
            var enableButton = true;

            if (package.AddSignal.Operation != null && displayPackage.Origin == PackageOrigin.Builtin)
            {
                actionLabel = GetButtonText(PackageAction.Enable, true);
                enableButton = false;
                visibleFlag = true;
            } 
            else if (package.AddSignal.Operation != null && displayPackage.Origin != PackageOrigin.Builtin)
            {
                var version = package.AddSignal.Operation.PackageInfo.Version;
                if (!displayPackage.IsCurrent)
                {
                    actionLabel = GetButtonText(PackageAction.Add, true, version);
                }
                else
                {
                    var currentVersion = package.Current.Version;
                    var action = version.CompareByPrecedence(currentVersion) > 0
                        ? PackageAction.Update
                        : PackageAction.Downgrade;

                    actionLabel = GetButtonText(action, true, version);
                }
                
                enableButton = false;
                visibleFlag = true;
            }
            else if (package.Current != null && package.Latest != null && package.Latest.Version != package.Current.Version)
            {
                var version = package.Latest.Version;
                var currentVersion = package.Current.Version;
                var action = version.CompareByPrecedence(currentVersion) > 0
                    ? PackageAction.Update
                    : PackageAction.Downgrade;
                actionLabel = GetButtonText(action, false, version);
                visibleFlag = true;
            }
            else if (package.Current == null && package.Versions.Any())
            {
                var version = package.Latest.Version;
                actionLabel = displayPackage.Origin == PackageOrigin.Builtin ?
                    GetButtonText(PackageAction.Enable) :
                    GetButtonText(PackageAction.Add, false, version);
                visibleFlag = true;
            }

            UpdateButton.SetEnabled(enableButton);
            UpdateButton.text = actionLabel;   
            SetUpdateVisibility(visibleFlag);
        }

        private void RefreshRemoveButton()
        {
            var displayPackage = Display(package);
            var visibleFlag = false;
            var actionLabel = displayPackage.Origin == PackageOrigin.Builtin ?
                GetButtonText(PackageAction.Disable) :
                GetButtonText(PackageAction.Remove, false, displayPackage.Version);
            var enableButton = false;

            if (filter != PackageFilter.All)
            {
                visibleFlag = !package.IsPackageManagerUI;
                enableButton = !package.IsPackageManagerUI;
                if (package.RemoveSignal.Operation != null)
                {
                    actionLabel = displayPackage.Origin == PackageOrigin.Builtin ?
                        GetButtonText(PackageAction.Disable, true) :
                        GetButtonText(PackageAction.Remove, true, displayPackage.Version);;
                    enableButton = true;
                }
            }
            
            RemoveButton.SetEnabled(enableButton);
            RemoveButton.text = actionLabel;   
            UIUtils.SetElementDisplay(RemoveButton, visibleFlag);
        }

        private static string GetButtonText(PackageAction action, bool inProgress = false, SemVersion version = null)
        {
            return version == null ? 
                string.Format("{0}", inProgress ? PackageActionInProgressVerbs[(int) action] : PackageActionVerbs[(int) action]) : 
                string.Format("{0} {1}", inProgress ? PackageActionInProgressVerbs[(int) action] : PackageActionVerbs[(int) action], version);
        }

        private void UpdateClick()
        {
            if (package.IsPackageManagerUI)
            {
                if (!EditorUtility.DisplayDialog("", "Updating this package will temporarily close the Package Manager window. Do you want to continue?", "Yes", "No")) 
                    return;
                
                if (package.AddSignal.Operation != null)
                {
                    package.AddSignal.Operation.OnOperationSuccess -= OnAddOperationSuccess;
                    package.AddSignal.Operation.OnOperationError -= OnAddOperationError;
                    package.AddSignal.ResetEvents();
                    package.AddSignal.Operation = null;
                }

                DetailError.ClearError();
                EditorApplication.update += CloseAndUpdate;

                return;
            }
           
            DetailError.ClearError();
            package.Update();
            RefreshAddButton();
        }

        private void CloseAndUpdate()
        {
            EditorApplication.update -= CloseAndUpdate;

            // Registered on callback
            AssemblyReloadEvents.beforeAssemblyReload += PackageManagerWindow.ShowPackageManagerWindow;

            package.Update();

            var windows = Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
            if (windows.Length > 0)
            {
                windows[0].Close();
            }
        }
        

        private void RemoveClick()
        {
            DetailError.ClearError();
            package.Remove();
            RefreshRemoveButton();
        }

        private void ViewDocClick()
        {
            var packageInfo = Display(package);
            var url = string.Format("http://docs.unity3d.com/Packages/{0}/index.html", packageInfo.ShortVersionId);
            Application.OpenURL(url);
        } 

        private void ViewChangelogClick()
        {
            var packageInfo = package.Latest;
            var url = string.Format("http://docs.unity3d.com/Packages/{0}/changelog/CHANGELOG.html", packageInfo.ShortVersionId);
            Application.OpenURL(url);
        }

        private Label DetailDesc { get { return root.Q<Label>("detailDesc"); } }
        private Button UpdateButton { get { return root.Q<Button>("update"); } }
        private Button RemoveButton { get { return root.Q<Button>("remove"); } }
        private Button ViewDocButton { get { return root.Q<Button>("viewDocumentation"); } }
        private Button ViewChangelogButton { get { return root.Q<Button>("viewChangelog"); } }
        private VisualElement UpdateContainer { get { return root.Q<VisualElement>("updateContainer"); } }
        private Alert DetailError { get { return root.Q<Alert>("detailError"); } }
        private ScrollView DetailView { get { return root.Q<ScrollView>("detailView"); } }
        private Label DetailPackageStatus { get { return root.Q<Label>("detailPackageStatus"); } }
        private Label DetailModuleReference { get { return root.Q<Label>("detailModuleReference"); } }
        private Label DetailVersion { get { return root.Q<Label>("detailVersion"); } }
        private Label DetailAuthor { get { return root.Q<Label>("detailAuthor"); } }
        internal Label VerifyLabel { get { return root.Q<Label>("tagVerify"); } }
        internal VisualElement GetTag(PackageTag tag) {return root.Q<VisualElement>("tag-" + tag.ToString()); } 
    }
}
