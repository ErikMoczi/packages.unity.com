using System;
using System.Linq;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class PackageGroupFactory : UxmlFactory<PackageGroup>
    {
        protected override PackageGroup DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageGroup(bag.GetPropertyString("name"));
        }
    }
#endif

    internal class PackageGroup : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<PackageGroup> { }
#endif

        private readonly VisualElement root;
        private bool collapsed;
        private readonly VisualElement listElement;

        internal readonly PackageGroupOrigins Origin;

        public PackageGroup previousGroup;
        public PackageGroup nextGroup;

        public PackageItem firstPackage;
        public PackageItem lastPackage;

        public PackageGroup() : this(String.Empty)
        {
        }

        public PackageGroup(string groupName)
        {
            name = groupName;
            root = Resources.GetTemplate("PackageGroup.uxml");
            Add(root);
            listElement = List;

#if UNITY_2018_2_OR_NEWER
            Header.AddManipulator(new Clickable(ToggleCollapse));
#else
            List.style.marginLeft = 0;
            Header.style.height = 0;
#endif

            if (string.IsNullOrEmpty(groupName) || groupName != PackageGroupOrigins.BuiltInPackages.ToString())
            {
                HeaderTitle.text = "Packages";
                Origin = PackageGroupOrigins.Packages;
                SetCollapsed(false);
            }
            else
            {
                HeaderTitle.text = "Built In Packages";
                Origin = PackageGroupOrigins.BuiltInPackages;
                SetCollapsed(true);
            }
        }

        public void SetCollapsed(bool value)
        {
            Caret.text = value ? "\u25B6" : "\u25BC";

            if (value == collapsed)
                return;

            if (value)
                List.RemoveFromHierarchy();
            else
                ListContainer.Add(listElement);

            collapsed = value;
        }

        private void ToggleCollapse()
        {
            SetCollapsed(!Collapsed);
        }

        internal PackageItem AddPackage(Package package)
        {
            var packageItem = new PackageItem(package) {packageGroup = this};
            var lastItem = listElement.Children().LastOrDefault() as PackageItem;
            if (lastItem != null)
            {
                lastItem.nextItem = packageItem;
                packageItem.previousItem = lastItem;
                packageItem.nextItem = null;
            }

            listElement.Add(packageItem);

            if (firstPackage == null) firstPackage = packageItem;
            lastPackage = packageItem;

            return packageItem;
        }

        private VisualElement List { get { return root.Q<VisualElement>("groupContainer"); } }
        private VisualElement ListContainer { get { return root.Q<VisualElement>("groupContainerOuter"); } }
        private VisualElement Header { get { return root.Q<VisualElement>("headerContainer"); } }
        private Label HeaderTitle { get { return root.Q<Label>("headerTitle"); } }
        private Label Caret { get { return root.Q<Label>("headerCaret"); } }
        internal bool Collapsed { get { return collapsed; } set { SetCollapsed(value); } }
    }
}
