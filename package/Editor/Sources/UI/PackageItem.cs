﻿using UnityEngine.Experimental.UIElements;
using System;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageItemFactory : UxmlFactory<PackageItem>
    {
        protected override PackageItem DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageItem();
        }
    }

    internal class PackageItem : VisualElement
    {
        private const string TemplatePath = PackageManagerWindow.ResourcesPath + "Templates/PackageItem.uxml";

        public static string SelectedClassName = "selected";
        
        public event Action<PackageItem> OnSelected = delegate { };
        
        private const string loadingSpinnerId = "loadingSpinnerContainer";

        private readonly VisualElement root;
        private string currentStateClass;
        public Package package { get; private set; }
        
        public PackageItem previousItem;
        public PackageItem nextItem;

        public PackageGroup packageGroup;

        public PackageItem(Package package = null)
        {
            root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath).CloneTree(null);
            Add(root);
            
            root.Q<VisualElement>(loadingSpinnerId).clippingOptions = ClippingOptions.NoClipping;

            root.AddManipulator(new Clickable(Select));
            if (package != null)
                SetItem(package);
        }

        // Package version to display
        public PackageInfo Display(Package package)
        {
            return PackageCollection.Instance.Filter == PackageFilter.All || package.Current == null ? package.Latest : package.Current;
        }
        
        private void Select()
        {
            OnSelected(this);
        }

        public void SetSelected(bool value)
        {
            if (value)
                PackageContainer.AddToClassList(SelectedClassName);
            else
                PackageContainer.RemoveFromClassList(SelectedClassName);                

            Spinner.InvertColor = value;
        }

        private void SetItem(Package package)
        {
            if (Display(package) == null)
                return;
            
            this.package = package;
            this.package.AddSignal.WhenOperation(OnPackageUpdate);
            
            OnPackageChanged();
        }

        private void OnPackageChanged()
        {
            var displayPackage = Display(package);
            if (displayPackage == null)
                return;

            NameLabel.text = displayPackage.DisplayName;
            VersionLabel.text = displayPackage.Version.ToString();
            
            var stateClass = GetIconStateId(package.Current ?? package.Latest);
            
            StateLabel.RemoveFromClassList(currentStateClass);
            StateLabel.AddToClassList(stateClass);

            if(package.Current == null && PackageCollection.Instance.Filter == PackageFilter.All)
                PackageContainer.AddToClassList("not-installed");
            else
                PackageContainer.RemoveFromClassList("not-installed");

            UIUtils.SetElementDisplay(VersionLabel, !PackageInfo.IsModule(package.Name));
            
            currentStateClass = stateClass;
            if (displayPackage.State != PackageState.InProgress && Spinner.Started)
                Spinner.Stop();
        }

        private void OnPackageUpdate(IAddOperation operation)
        {
            Spinner.Start();
        }

        private Label NameLabel { get { return root.Q<Label>("packageName"); } }
        private Label StateLabel { get { return root.Q<Label>("packageState"); } }
        private Label VersionLabel { get { return root.Q<Label>("packageVersion"); } }
        private VisualElement PackageContainer { get { return root.Q<VisualElement>("packageContainer"); } }
        private LoadingSpinner Spinner { get { return root.Q<LoadingSpinner>("packageSpinner"); } }

        public static string GetIconStateId(PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return "";

            return GetIconStateId(packageInfo.State);
        }

        public static string GetIconStateId(PackageState state)
        {
            return state.ToString().ToLower();
        }
    }
}