using System;
using System.Collections.Generic;
using System.Linq;
using Semver;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class PackageDetailsFactory : UxmlFactory<PackageDetails>
    {
        protected override PackageDetails DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageDetails();
        }
    }
#endif

    internal class PackageDetails : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<PackageDetails> {}
#endif

        public event Action<Package> OnCloseError = delegate {};
        public event Action<Package, Error> OnOperationError = delegate {};

        private readonly VisualElement root;
        private Package package;
        private const string emptyDescriptionClass = "empty";
        private PackageInfo DisplayPackage;
        private Selection Selection;
        private PackageCollection Collection;    // TODO: Do I need this here?

        internal enum PackageAction
        {
            Add,
            Remove,
            Update,
            Downgrade,
            Enable,
            Disable,
            UpToDate,
            Current,
            Local,
            Git,
            Embedded
        }

        internal static readonly string[] PackageActionVerbs = { "Install", "Remove", "Update to", "Update to",  "Enable", "Disable", "Up to date", "Current", "Local", "Git", "Embedded" };
        private static readonly string[] PackageActionInProgressVerbs = { "Installing", "Removing", "Updating to", "Updating to", "Enabling", "Disabling", "Up to date", "Current", "Local", "Git", "Embedded" };

        public PackageDetails()
        {
            UIUtils.SetElementDisplay(this, false);

            root = Resources.GetTemplate("PackageDetails.uxml");
            Add(root);

            Cache = new VisualElementCache(root);

            foreach (var extension in PackageManagerExtensions.Extensions)
                CustomContainer.Add(extension.CreateExtensionUI());

            root.StretchToParentSize();

            SetUpdateVisibility(false);
            RemoveButton.visible = false;
            UpdateBuiltIn.visible = false;

            UpdateButton.clickable.clicked += UpdateClick;
            UpdateBuiltIn.clickable.clicked += UpdateClick;
            RemoveButton.clickable.clicked += RemoveClick;
            ViewDocButton.clickable.clicked += ViewDocClick;
            ViewChangelogButton.clickable.clicked += ViewChangelogClick;
            ViewLicenses.clickable.clicked += ViewLicensesClick;

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        public void SetCollection(PackageCollection collection)
        {
            Collection = collection;
            Dependencies.SetCollection(collection);
        }

        public void SetSelection(Selection selection)
        {
            if (Selection != null)
                Selection.OnChanged -= OnSelectionChanged;

            Selection = selection;
            Selection.OnChanged += OnSelectionChanged;

            PackageToolbar.SetSelection(selection);

            OnSelectionChanged(Selection.SelectedVersions);
        }

        private void OnSelectionChanged(IEnumerable<PackageVersion> selected)
        {
            if (!Selection.SelectedVersions.Any())
                return;

            var main = selected.FirstOrDefault();
            SetPackage(main.Package, main.Version);
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            PackageManagerToolbar.OnToggleDependenciesChange += ShowDependencies;
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            PackageManagerToolbar.OnToggleDependenciesChange -= ShowDependencies;
        }

        private void ShowDependencies()
        {
            if (DisplayPackage == null || DisplayPackage.Info == null)
                return;

            if (PackageManagerPrefs.ShowPackageDependencies)
                Dependencies.SetDependencies(DisplayPackage.Info.dependencies);

            UIUtils.SetElementDisplay(Dependencies, PackageManagerPrefs.ShowPackageDependencies);
        }

        private void SetUpdateVisibility(bool value)
        {
            UIUtils.SetElementDisplay(UpdateButton, value);
        }

        private void SetDisplayPackage(PackageInfo packageInfo, Error packageError = null)
        {
            DisplayPackage = packageInfo;

            var detailVisible = true;
            Error error = null;

            if (package == null || DisplayPackage == null)
            {
                detailVisible = false;
                UIUtils.SetElementDisplay(DocumentationContainer, false);
                UIUtils.SetElementDisplay(CustomContainer, false);
                UIUtils.SetElementDisplay(UpdateBuiltIn, false);
                UIUtils.SetElementDisplay(SampleList, false);
                UIUtils.SetElementDisplay(Dependencies, false);

                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageSelectionChange(null);
            }
            else
            {
                SetUpdateVisibility(true);
                var isBuiltIn = DisplayPackage.IsBuiltIn;
                UIUtils.SetElementDisplay(ViewDocButton, true);
                UIUtils.SetElementDisplay(ViewLicensesContainer, !isBuiltIn);
                RemoveButton.visible = true;

                if (string.IsNullOrEmpty(DisplayPackage.Description))
                {
                    DetailDesc.text = "There is no description for this package.";
                    DetailDesc.AddToClassList(emptyDescriptionClass);
                }
                else
                {
                    DetailDesc.text = DisplayPackage.Description;
                    DetailDesc.RemoveFromClassList(emptyDescriptionClass);
                }

                root.Q<Label>("detailTitle").text = DisplayPackage.DisplayName;
                DetailVersion.text = "Version " + DisplayPackage.VersionWithoutTag;

                if (DisplayPackage.IsInDevelopment || DisplayPackage.HasVersionTag(PackageTag.preview))
                    UIUtils.SetElementDisplay(GetTag(PackageTag.verified), false);
                else 
                {
                    var unityVersionParts = Application.unityVersion.Split('.');
                    var unityVersion = string.Format("{0}.{1}", unityVersionParts[0], unityVersionParts[1]);
                    VerifyLabel.text = unityVersion + " verified";
                    UIUtils.SetElementDisplay(GetTag(PackageTag.verified), DisplayPackage.IsVerified);
                }

                UIUtils.SetElementDisplay(GetTag(PackageTag.inDevelopment), DisplayPackage.IsInDevelopment);
                UIUtils.SetElementDisplay(GetTag(PackageTag.local), DisplayPackage.IsLocal);
                UIUtils.SetElementDisplay(GetTag(PackageTag.git), DisplayPackage.IsGit);
                UIUtils.SetElementDisplay(GetTag(PackageTag.preview), DisplayPackage.IsPreview);

                UIUtils.SetElementDisplay(DocumentationContainer, true);
                UIUtils.SetElementDisplay(ChangelogContainer, DisplayPackage.HasChangelog(DisplayPackage));

                SampleList.SetPackage(DisplayPackage);

                root.Q<Label>("detailName").text = DisplayPackage.Name;
                root.Q<ScrollView>("detailView").scrollOffset = new Vector2(0, 0);

                DetailModuleReference.text = "";
                if (isBuiltIn)
                    DetailModuleReference.text = DisplayPackage.BuiltInDescription;

                DetailAuthor.text = "";
                if (!string.IsNullOrEmpty(DisplayPackage.Author))
                    DetailAuthor.text = string.Format("Author: {0}", DisplayPackage.Author);

                UIUtils.SetElementDisplay(DetailDesc, !isBuiltIn);
                UIUtils.SetElementDisplay(DetailVersion, (!isBuiltIn && !DisplayPackage.IsCore));
                UIUtils.SetElementDisplayNonEmpty(DetailModuleReference);
                UIUtils.SetElementDisplayNonEmpty(DetailAuthor);

                if (DisplayPackage.Errors.Count > 0)
                    error = DisplayPackage.Errors.First();

                RefreshAddButton();
                RefreshRemoveButton();
                UIUtils.SetElementDisplay(CustomContainer, true);

                package.AddSignal.OnOperation += OnAddOperation;
                package.RemoveSignal.OnOperation += OnRemoveOperation;
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageSelectionChange(DisplayPackage.Info);

                ShowDependencies();
            }

            // Set visibility
            root.Q<VisualElement>("detail").visible = detailVisible;

            if (packageError == null && package != null && package.Error != null)
                packageError = package.Error;
            if (null == error)
                error = packageError;

            if (error != null)
                SetError(error);
            else
                DetailError.ClearError();
        }

        public void SetPackage(Package package, PackageInfo displayPackage = null)
        {
            if (package == this.package && displayPackage == DisplayPackage)
                return;

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
                    this.package.RemoveSignal.Operation.OnOperationSuccess -= OnRemoveOperationSuccess;
                    this.package.RemoveSignal.Operation.OnOperationError -= OnRemoveOperationError;
                }
                this.package.RemoveSignal.ResetEvents();
            }

            UIUtils.SetElementDisplay(this, true);

            this.package = package;
            if (displayPackage == null)
                displayPackage = package != null ? package.VersionToDisplay : null;

            ShowDisplayPackage(displayPackage);
        }

        private void ShowDisplayPackage(PackageInfo displayPackage)
        {
            Collection.CancelFetchLatest();

            SetEnabled(true);
            SetDisplayPackage(displayPackage);

            if (Collection.NeedsFetchLatest(displayPackage))
            {
                // Only reload if the display version is different then the hero one. This is to avoid issue where a user click on a main version,
                // sees the package information, and soon after see the information change. This is temporary until
                // upm fixes displayName for packages by giving all the correct PackageInfo information for each version.
                // Afterward, the condition below can be removed.
                if (displayPackage != package.VersionToDisplay)
                {
                    SetEnabled(false);
                    Collection.FetchLatestPackageInfo(displayPackage, packageInfo =>
                    {
                        SetDisplayPackage(packageInfo);
                        SetEnabled(true);
                    });
                }
            }
        }

        private void SetError(Error error)
        {
            DetailError.AdjustSize(DetailView.verticalScroller.visible);
            DetailError.SetError(error);
            DetailError.OnCloseError = () =>
            {
                OnCloseError(package);
            };
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
            OnOperationError(package, error);
        }

        private void OnAddOperationSuccess(PackageInfo packageInfo)
        {
            if (package != null && package.AddSignal.Operation != null)
            {
                package.AddSignal.Operation.OnOperationSuccess -= OnAddOperationSuccess;
                package.AddSignal.Operation.OnOperationError -= OnAddOperationError;
                package.AddSignal.Operation = null;
            }

            foreach (var extension in PackageManagerExtensions.Extensions)
                extension.OnPackageAddedOrUpdated(packageInfo.Info);

            Selection.SetSelection(packageInfo);
            foreach (var itemState in Selection.States.Where(p => p.PackageId == packageInfo.PackageId))
                itemState.Expanded = false;
        }

        private void OnRemoveOperation(IRemoveOperation operation)
        {
            // Make sure we are not already registered
            operation.OnOperationError -= OnRemoveOperationError;
            operation.OnOperationSuccess -= OnRemoveOperationSuccess;

            operation.OnOperationError += OnRemoveOperationError;
            operation.OnOperationSuccess += OnRemoveOperationSuccess;
        }

        private void OnRemoveOperationError(Error error)
        {
            if (package != null && package.RemoveSignal.Operation != null)
            {
                package.RemoveSignal.Operation.OnOperationSuccess -= OnRemoveOperationSuccess;
                package.RemoveSignal.Operation.OnOperationError -= OnRemoveOperationError;
                package.RemoveSignal.Operation = null;
            }

            SetError(error);
            OnOperationError(package, error);
        }

        private void OnRemoveOperationSuccess(PackageInfo packageInfo)
        {
            if (package != null && package.RemoveSignal.Operation != null)
            {
                package.RemoveSignal.Operation.OnOperationSuccess -= OnRemoveOperationSuccess;
                package.RemoveSignal.Operation.OnOperationError -= OnRemoveOperationError;
                package.RemoveSignal.Operation = null;
            }

            foreach (var extension in PackageManagerExtensions.Extensions)
                extension.OnPackageRemoved(packageInfo.Info);
        }

        private PackageInfo TargetVersion
        {
            get
            {
                var targetVersion = DisplayPackage;
                if (targetVersion == null)
                    return null;

                if (package.Current != null && package.Current.PackageId == targetVersion.PackageId)
                    targetVersion = package.LatestUpdate;

                return targetVersion;
            }
        }

        private void RefreshAddButton()
        {
            if (package.Current != null && package.Current.IsInDevelopment)
            {
                UIUtils.SetElementDisplay(UpdateBuiltIn, false);
                UIUtils.SetElementDisplay(UpdateButton, false);
                return;
            }

            var targetVersion = TargetVersion;
            if (targetVersion == null)
                return;

            var enableButton = !Package.AddRemoveOperationInProgress;

            var action = PackageAction.Update;
            var inprogress = false;
            var isBuiltIn = package.IsBuiltIn;

            if (package.AddSignal.Operation != null)
            {
                if (isBuiltIn)
                {
                    action = PackageAction.Enable;
                    inprogress = true;
                    enableButton = false;
                }
                else
                {
                    var addOperationVersion = package.AddSignal.Operation.PackageInfo.Version;
                    if (package.Current == null)
                    {
                        action = PackageAction.Add;
                        inprogress = true;
                    }
                    else
                    {
                        action = addOperationVersion.CompareByPrecedence(package.Current.Version) >= 0
                            ? PackageAction.Update : PackageAction.Downgrade;
                        inprogress = true;
                    }

                    enableButton = false;
                }
            }
            else
            {
                if (package.Current != null)
                {
                    // Installed
                    if (package.Current.IsVersionLocked)
                    {
                        if (package.Current.Origin == PackageSource.Embedded)
                            action = PackageAction.Embedded;
                        else if (package.Current.Origin == PackageSource.Git)
                            action = PackageAction.Git;

                        enableButton = false;
                    }
                    else
                    {
                        if (targetVersion.IsCurrent)
                        {
                            if (targetVersion == package.LatestUpdate)
                                action = PackageAction.UpToDate;
                            else
                                action = PackageAction.Current;

                            enableButton = false;
                        }
                        else
                        {
                            action = targetVersion.Version.CompareByPrecedence(package.Current.Version) >= 0
                                ? PackageAction.Update : PackageAction.Downgrade;
                        }
                    }
                }
                else
                {
                    // Not Installed
                    if (package.Versions.Any())
                    {
                        if (isBuiltIn)
                            action = PackageAction.Enable;
                        else
                            action = PackageAction.Add;
                    }
                }
            }

            if (package.RemoveSignal.Operation != null)
                enableButton = false;

            if (EditorApplication.isCompiling)
            {
                enableButton = false;

                EditorApplication.update -= CheckCompilationStatus;
                EditorApplication.update += CheckCompilationStatus;
            }

            var version = action == PackageAction.Update || action == PackageAction.Downgrade ? targetVersion.Version : null;

            var button = isBuiltIn ? UpdateBuiltIn : UpdateButton;
            button.SetEnabled(enableButton);
            button.text = GetButtonText(action, inprogress, version);

            var visibleFlag = !(package.Current != null && package.Current.IsVersionLocked);
            UIUtils.SetElementDisplay(UpdateBuiltIn, isBuiltIn && visibleFlag);
            UIUtils.SetElementDisplay(UpdateButton, !isBuiltIn && visibleFlag);
        }

        private void RefreshRemoveButton()
        {
            var visibleFlag = false;

            var current = package.Current;

            // Show only if there is a current package installed
            if (current != null)
            {
                visibleFlag = current.CanBeRemoved && !package.IsPackageManagerUI;

                var action = current.IsBuiltIn ? PackageAction.Disable : PackageAction.Remove;
                var inprogress = package.RemoveSignal.Operation != null;

                var enableButton = visibleFlag && !EditorApplication.isCompiling && !inprogress && !Package.AddRemoveOperationInProgress
                    && current == DisplayPackage;

                if (EditorApplication.isCompiling)
                {
                    EditorApplication.update -= CheckCompilationStatus;
                    EditorApplication.update += CheckCompilationStatus;
                }

                RemoveButton.SetEnabled(enableButton);
                RemoveButton.text = GetButtonText(action, inprogress);
            }

            UIUtils.SetElementDisplay(RemoveButton, visibleFlag);
        }

        private void CheckCompilationStatus()
        {
            if (EditorApplication.isCompiling)
                return;

            RefreshAddButton();
            RefreshRemoveButton();
            EditorApplication.update -= CheckCompilationStatus;
        }

        private string GetButtonText(PackageAction action, bool inProgress = false, SemVersion version = null)
        {
            var actionText = inProgress ? PackageActionInProgressVerbs[(int)action] : PackageActionVerbs[(int)action];
            return version == null ?
                string.Format("{0}",        actionText) :
                string.Format("{0} {1}", actionText, version);
        }

        private void UpdateClick()
        {
            if (package.IsPackageManagerUI)
            {
                // Let's not allow updating of the UI if there are build errrors, as for now, that will prevent the UI from reloading properly.
                if (EditorUtility.scriptCompilationFailed)
                {
                    EditorUtility.DisplayDialog("Unity Package Manager", "The Package Manager UI cannot be updated while there are script compilation errors in your project.  Please fix the errors and try again.", "Ok");
                    return;
                }

                if (!EditorUtility.DisplayDialog("Unity Package Manager", "Updating this package will close the Package Manager window. You will have to re-open it after the update is done. Do you want to continue?", "Yes", "No"))
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
            package.Add(TargetVersion);
            RefreshAddButton();
            RefreshRemoveButton();
        }

        private void CloseAndUpdate()
        {
            EditorApplication.update -= CloseAndUpdate;
            package.Add(TargetVersion);

            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
            if (windows.Length > 0)
            {
                windows[0].Close();
            }
        }

        private void RemoveClick()
        {
            var result = 1;    // Cancel
            if (!PackageManagerPrefs.SkipRemoveConfirmation)
            {
                result = EditorUtility.DisplayDialogComplex("Removing Package",
                    "Are you sure you wanted to remove this package?",
                    "Remove", "Cancel", "Remove and do not ask again");
            }
            else
                result = 0;

            // Cancel
            if (result == 1)
                return;

            // Do not ask again
            if (result == 2)
                PackageManagerPrefs.SkipRemoveConfirmation = true;

            // Remove
            DetailError.ClearError();
            package.Remove();
            RefreshRemoveButton();
            RefreshAddButton();
        }

        private void ViewDocClick()
        {
            Application.OpenURL(DisplayPackage.GetDocumentationUrl());
        }

        private void ViewChangelogClick()
        {
            Application.OpenURL(DisplayPackage.GetChangelogUrl());
        }

        private void ViewLicensesClick()
        {
            Application.OpenURL(DisplayPackage.GetLicensesUrl());
        }

        private VisualElementCache Cache { get; set; }

        private Label DetailDesc { get { return Cache.Get<Label>("detailDesc"); } }
        internal Button UpdateButton { get { return Cache.Get<Button>("update"); } }
        private Button RemoveButton { get { return Cache.Get<Button>("remove"); } }
        private Button ViewDocButton { get { return Cache.Get<Button>("viewDocumentation"); } }
        private VisualElement DocumentationContainer { get { return Cache.Get<VisualElement>("documentationContainer"); } }
        private Button ViewChangelogButton { get { return Cache.Get<Button>("viewChangelog"); } }
        private VisualElement ChangelogContainer { get { return Cache.Get<VisualElement>("changeLogContainer"); } }
        private Button ViewLicenses { get { return Cache.Get<Button>("viewLicenses"); } }
        private VisualElement ViewLicensesContainer { get { return Cache.Get<VisualElement>("viewLicensesContainer"); } }
        private Alert DetailError { get { return Cache.Get<Alert>("detailError"); } }
        private ScrollView DetailView { get { return Cache.Get<ScrollView>("detailView"); } }
        private Label DetailModuleReference { get { return Cache.Get<Label>("detailModuleReference"); } }
        private Label DetailVersion { get { return Cache.Get<Label>("detailVersion"); } }
        private Label DetailAuthor { get { return Cache.Get<Label>("detailAuthor"); } }
        private Label VerifyLabel { get { return Cache.Get<Label>("tagVerify"); } }
        private VisualElement CustomContainer { get { return Cache.Get<VisualElement>("detailCustomContainer"); } }
        private PackageSampleList SampleList { get { return Cache.Get<PackageSampleList>("detailSampleList"); } }
        internal VisualElement GetTag(PackageTag tag) {return Cache.Get<VisualElement>("tag-" + tag); }
        private PackageDependencies Dependencies { get {return Cache.Get<PackageDependencies>("detailDependencies");} }
        internal Button UpdateBuiltIn { get { return Cache.Get<Button>("updateBuiltIn"); } }
        internal PackageToolbar PackageToolbar { get { return Cache.Get<PackageToolbar>("packageToolBar"); } }
    }
}
