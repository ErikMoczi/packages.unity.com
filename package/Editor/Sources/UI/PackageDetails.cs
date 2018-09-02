using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Semver;
using UnityEditor.Experimental.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Experimental.UIElements;

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
        internal new class UxmlFactory : UxmlFactory<PackageDetails> { }
#endif

        public event Action<PackageManager.PackageInfo> OnPackageUpdate = delegate { };

        private readonly VisualElement root;
        private Package package;
        private const string emptyId = "emptyArea";
        private const string emptyDescriptionClass = "empty";
        private List<VersionItem> VersionItems;
        internal PopupField<VersionItem> VersionPopup;
        private PackageInfo DisplayPackage;

        private PackageInfo SelectedPackage
        {
            get { return VersionPopup.value.Version != null ? VersionPopup.value.Version : null; }
        }

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

        private static readonly VersionItem EmptyVersion = new VersionItem {Version = null};
        internal static readonly string[] PackageActionVerbs = { "Install", "Remove", "Update to", "Update to",  "Enable", "Disable", "Up to date", "Current", "Local", "Git", "Embedded" };
        internal static readonly string[] PackageActionInProgressVerbs = { "Installing", "Removing", "Updating to", "Updating to", "Enabling", "Disabling", "Up to date", "Current", "Local", "Git", "Embedded" };

        public PackageDetails()
        {
            root = Resources.GetTemplate("PackageDetails.uxml");
            Add(root);

            foreach (var extension in PackageManagerExtensions.Extensions)
                CustomContainer.Add(extension.CreateExtensionUI());

            root.StretchToParentSize();

            SetUpdateVisibility(false);
            RemoveButton.visible = false;
            UpdateBuiltIn.visible = false;
            root.Q<VisualElement>(emptyId).visible = false;

            UpdateButton.clickable.clicked += UpdateClick;
            UpdateBuiltIn.clickable.clicked += UpdateClick;
            RemoveButton.clickable.clicked += RemoveClick;
            ViewDocButton.clickable.clicked += ViewDocClick;
            ViewChangelogButton.clickable.clicked += ViewChangelogClick;
            ViewLicenses.clickable.clicked += ViewLicensesClick;

            UpdateButton.parent.clippingOptions = ClippingOptions.NoClipping;
            UpdateButton.parent.parent.clippingOptions = ClippingOptions.NoClipping;
            UpdateButton.parent.parent.parent.clippingOptions = ClippingOptions.NoClipping;

            VersionItems = new List<VersionItem> {EmptyVersion};
            VersionPopup = new PopupField<VersionItem>(VersionItems, 0);
            VersionPopup.SetLabelCallback(VersionSelectionSetLabel);
            VersionPopup.AddToClassList("popup");
            VersionPopup.OnValueChanged(VersionSelectionChanged);
            
            if (VersionItems.Count == 1)
                VersionPopup.SetEnabled(false);
                        
            UpdateDropdownContainer.Add(VersionPopup);
            VersionPopup.StretchToParentSize();
            
            PackageCollection.Instance.OnFilterChanged += OnFilterChanged;
            PackageCollection.Instance.OnPackageUpdated += OnPackageUpdated;

            // Fix button on dark skin but overlapping edge pixel perfectly
            if (EditorGUIUtility.isProSkin)
            {
                VersionPopup.style.positionLeft = -1;
                UpdateDropdownContainer.style.sliceLeft = 4;
            }
        }

        private string VersionSelectionSetLabel(VersionItem item)
        {
            return item.Label;
        }

        private void VersionSelectionChanged(ChangeEvent<VersionItem> e)
        {
            RefreshAddButton();
        }

        private void OnPackageUpdated(Package package)
        {
            if (this.package != null && package != null && this.package.Name == package.Name)
            {
                OnPackageUpdate(package.VersionToDisplay.Info);
            }
        }

        private void SetUpdateVisibility(bool value)
        {
            if (UpdateContainer != null)
                UIUtils.SetElementDisplay(UpdateContainer, value);
        }

        private void OnFilterChanged(PackageFilter obj)
        {
            root.Q<VisualElement>(emptyId).visible = false;
        }

        internal void SetDisplayPackage(PackageInfo packageInfo)
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

                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageSelectionChange(null);
            }
            else
            {
                SetUpdateVisibility(true);
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

                if (DisplayPackage.HasTag(PackageTag.preview))
                    UIUtils.SetElementDisplay(GetTag(PackageTag.verified), false);
                else
                {
                    var unityVersionParts = Application.unityVersion.Split('.');
                    var unityVersion = string.Format("{0}.{1}", unityVersionParts[0], unityVersionParts[1]);
                    VerifyLabel.text = unityVersion + " verified";
                    UIUtils.SetElementDisplay(GetTag(PackageTag.verified), DisplayPackage.IsVerified);
                }

                UIUtils.SetElementDisplay(GetTag(PackageTag.preview), DisplayPackage.IsPreview);

                UIUtils.SetElementDisplay(DocumentationContainer, DisplayPackage.Origin != PackageSource.BuiltIn);
                UIUtils.SetElementDisplay(ChangelogContainer, HasChangelog(DisplayPackage));

                root.Q<Label>("detailName").text = DisplayPackage.Name;
                root.Q<ScrollView>("detailView").scrollOffset = new Vector2(0, 0);

                DetailModuleReference.text = "";
                var isModule = PackageInfo.IsModule(DisplayPackage.Name);
                if (PackageInfo.IsModule(DisplayPackage.Name))
                {
                    DetailModuleReference.text = DisplayPackage.Description;
                    if (string.IsNullOrEmpty(DisplayPackage.Description))
                        DetailModuleReference.text = string.Format("This built in package controls the presence of the {0} module.", DisplayPackage.ModuleName);
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

                DetailAuthor.text = string.Format("Author: {0}", DisplayPackage.Author ?? "Unity Technologies Inc.");

                UIUtils.SetElementDisplay(DetailDesc, !isModule);
                UIUtils.SetElementDisplay(DetailVersion, !isModule);
                UIUtils.SetElementDisplayNonEmpty(DetailModuleReference);
                UIUtils.SetElementDisplayNonEmpty(DetailPackageStatus);
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

        private void ResetVersionItems(PackageInfo displayPackage)
        {
            VersionItems.Clear();            
            VersionPopup.SetEnabled(true);

            //
            // Get key versions -- Latest, Verified, LatestPatch, Current.
            var keyVersions = new List<PackageInfo>();
            if (package.LatestRelease != null) keyVersions.Add(package.LatestRelease);
            if (package.Current != null) keyVersions.Add(package.Current);
            if (package.Verified != null && package.Verified != package.Current) keyVersions.Add(package.Verified);
            if (package.LatestPatch != null && package.IsAfterCurrentVersion(package.LatestPatch)) keyVersions.Add(package.LatestPatch);
            if (package.Current == null && package.LatestRelease == null && package.Latest != null) keyVersions.Add(package.Latest);
            if (Package.ShouldProposeLatestVersions && package.Latest != package.LatestRelease && package.Latest != null) keyVersions.Add(package.Latest);
            keyVersions.Add(package.LatestUpdate);        // Make sure LatestUpdate is always in the list.

            foreach (var version in keyVersions.OrderBy(package => package.Version).Reverse())
            {
                var item = new VersionItem {Version = version};
                VersionItems.Add(item);
                
                if (version == package.LatestUpdate)
                    VersionPopup.value = item;
            }

            //
            // Add all versions
            foreach (var version in package.UserVisibleVersions.Reverse())
            {
                var item = new VersionItem {Version = version};
                item.MenuName = "All Versions/";
                VersionItems.Add(item);
            }
            
            if (VersionItems.Count == 0)
            {
                VersionItems.Add(EmptyVersion);
                VersionPopup.value = EmptyVersion;
                VersionPopup.SetEnabled(false);
            }
        }
        
        public void SetPackage(Package package)
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

            this.package = package;
            var displayPackage = package != null ? package.VersionToDisplay : null;
            ResetVersionItems(displayPackage);
            SetDisplayPackage(displayPackage);
        }

        private void SetError(Error error)
        {
            DetailError.AdjustSize(DetailView.verticalScroller.visible);
            DetailError.SetError(error);
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

            PackageCollection.Instance.SetFilter(PackageFilter.Local);
        }

        private void OnRemoveOperation(IRemoveOperation operation)
        {
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
            RefreshRemoveButton();
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

        private void RefreshAddButton()
        {
            var targetVersion = SelectedPackage;
            if (targetVersion == null)
                return;
            
            var enableButton = true;
            var enableVersionButton = true;
            
            var action = PackageAction.Update;
            var inprogress = false;
            var isBuiltIn = package.IsBuiltIn;
            SemVersion version = null;
            
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
                    enableVersionButton = false;
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
                        else if (package.Current.Origin == PackageSource.Local)
                            action = PackageAction.Local;
                        else if (package.Current.Origin == PackageSource.Git)
                            action = PackageAction.Git;
                        
                        enableButton = false;
                        enableVersionButton = false;
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

            var button = isBuiltIn ? UpdateBuiltIn : UpdateButton;
            button.SetEnabled(enableButton);
            VersionPopup.SetEnabled(enableVersionButton);
            button.text = GetButtonText(action, inprogress, version);

            UIUtils.SetElementDisplay(UpdateBuiltIn, isBuiltIn && action != PackageAction.Update);
            UIUtils.SetElementDisplay(UpdateCombo, !isBuiltIn);
            UIUtils.SetElementDisplay(UpdateButton, !isBuiltIn);
        }

        private void RefreshRemoveButton()
        {
            var visibleFlag = false;

            var current = package.Current;
            
            // Show only if there is a current package installed
            if (current != null)
            {
                visibleFlag = true;
                var enableButton = !package.IsPackageManagerUI;

                var action = PackageAction.Remove;
                var inprogress = false;
                
                // Set builtin configuration
                if (current.Origin == PackageSource.BuiltIn)
                {
                    action = PackageAction.Disable;
                }

                // Disable when in progress
                if (package.RemoveSignal.Operation != null)
                {
                    inprogress = true;
                    enableButton = false;
                }

                if (package.Current.IsVersionLocked  && current.Origin != PackageSource.BuiltIn)
                {
                    enableButton = false;
                    visibleFlag = false;
                }

                RemoveButton.SetEnabled(enableButton);
                RemoveButton.text = GetButtonText(action, inprogress);                   
            }

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
                // Let's not allow updating of the UI if there are build errrors, as for now, that will prevent the UI from reloading properly.
                if (EditorUtility.scriptCompilationFailed)
                {
                    EditorUtility.DisplayDialog("", "The Package Manager UI cannot be updated while there are script compilation errors in your project.  Please fix the errors and try again.", "Ok");
                    return;
                }

                if (!EditorUtility.DisplayDialog("", "Updating this package will close the Package Manager window. You will have to re-open it after the update is done. Do you want to continue?", "Yes", "No"))
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
            package.Add(SelectedPackage);
            RefreshAddButton();
            RefreshRemoveButton();
        }

        private void CloseAndUpdate()
        {
            EditorApplication.update -= CloseAndUpdate;

            package.Add(SelectedPackage);

            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
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
            RefreshAddButton();
        }
        
        // Method content must be matched in package-manager-doctools extension
        public string GetPackageUrlRedirect(string packageName)
        {
            var redirectUrl = "";
            if (packageName == "com.unity.ads")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityAds.html";
            else if  (packageName == "com.unity.analytics")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityAnalytics.html";
            else if  (packageName == "com.unity.purchasing")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityIAP.html";
            else if  (packageName == "com.unity.standardevents")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityAnalyticsCustomEvents.html";
            else if  (packageName == "com.unity.xiaomi")
                redirectUrl = "https://unity3d.com/cn/partners/xiaomi/guide";
            else if (packageName == "com.unity.shadergraph")
                redirectUrl = "https://github.com/Unity-Technologies/ShaderGraph/wiki";
            else if (packageName == "com.unity.collab-proxy")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityCollaborate.html";

            return redirectUrl;
        }

        public bool RedirectsToManual(PackageInfo packageInfo)
        {
            return !string.IsNullOrEmpty(GetPackageUrlRedirect(packageInfo.Name));
        }

        public bool HasChangelog(PackageInfo packageInfo)
        {
            // Packages with no docs have no third party notice
            return !RedirectsToManual(packageInfo);
        }

        private void ViewDocClick()
        {
            var url = string.Format("http://docs.unity3d.com/Packages/{0}/index.html", DisplayPackage.ShortVersionId);
            Application.OpenURL(url);
        } 

        private void ViewChangelogClick()
        {
            var url = string.Format("http://docs.unity3d.com/Packages/{0}/changelog/CHANGELOG.html", SelectedPackage.ShortVersionId);
            Application.OpenURL(url);
        }

        private void ViewLicensesClick()
        {
            var url = string.Format("http://docs.unity3d.com/Packages/{0}/license/index.html", DisplayPackage.ShortVersionId);
            if (RedirectsToManual(DisplayPackage))
                url = "https://unity3d.com/legal/licenses/Unity_Companion_License";
            
            Application.OpenURL(url);            
        }

        private Label DetailDesc { get { return root.Q<Label>("detailDesc"); } }
        internal Button UpdateButton { get { return root.Q<Button>("update"); } }
        private Button RemoveButton { get { return root.Q<Button>("remove"); } }
        private Button ViewDocButton { get { return root.Q<Button>("viewDocumentation"); } }
        private VisualElement DocumentationContainer { get { return root.Q<VisualElement>("documentationContainer"); } }
        private Button ViewChangelogButton { get { return root.Q<Button>("viewChangelog"); } }
        private VisualElement ChangelogContainer { get { return root.Q<VisualElement>("changeLogContainer"); } }
        private VisualElement ViewLicensesContainer { get { return root.Q<VisualElement>("viewLicensesContainer"); } }
        private Button ViewLicenses { get { return root.Q<Button>("viewLicenses"); } }
        private VisualElement UpdateContainer { get { return root.Q<VisualElement>("updateContainer"); } }
        private Alert DetailError { get { return root.Q<Alert>("detailError"); } }
        private ScrollView DetailView { get { return root.Q<ScrollView>("detailView"); } }
        private Label DetailPackageStatus { get { return root.Q<Label>("detailPackageStatus"); } }
        private Label DetailModuleReference { get { return root.Q<Label>("detailModuleReference"); } }
        private Label DetailVersion { get { return root.Q<Label>("detailVersion");  }}
        private Label DetailAuthor { get { return root.Q<Label>("detailAuthor");  }}
        private VisualElement VersionContainer { get { return root.Q<Label>("versionContainer");  }}
        private Label VerifyLabel { get { return root.Q<Label>("tagVerify"); } }
        private VisualElement CustomContainer { get { return root.Q<VisualElement>("detailCustomContainer");  }}
        internal VisualElement GetTag(PackageTag tag) {return root.Q<VisualElement>("tag-" + tag.ToString()); }
        private VisualElement UpdateDropdownContainer { get { return root.Q<VisualElement>("updateDropdownContainer"); } }        
        private VisualElement UpdateCombo { get { return root.Q<VisualElement>("updateCombo"); } }
        private Button UpdateBuiltIn { get { return root.Q<Button>("updateBuiltIn"); } }        
    }
}
