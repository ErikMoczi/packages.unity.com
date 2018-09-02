using UnityEngine.Experimental.UIElements;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageGroupFactory : UxmlFactory<PackageGroup>
    {
        protected override PackageGroup DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageGroup(bag.GetPropertyString("name"));
        }
    }

    internal class PackageGroup : VisualElement
    {
        private readonly VisualElement root;
        private bool collapsed;
        private readonly VisualContainer listElement;

        internal readonly PackageGroupOrigins Origin;

        public PackageGroup(string groupName)
        {            
            root = Resources.Load<VisualTreeAsset>("Templates/PackageGroup").CloneTree(null);
            Add(root);
            listElement = List;

            Header.AddManipulator(new Clickable(ToggleCollapse));

            if (groupName == PackageGroupOrigins.BuiltInPackages.ToString())
            {
                Origin = PackageGroupOrigins.BuiltInPackages;
                SetCollapsed(true);
                HeaderTitle.text = "Built In Packages";
            }
            else
            {
                HeaderTitle.text = "Packages";
                Origin = PackageGroupOrigins.Packages;
                Caret.SetState(false);
            }
        }

        private void SetCollapsed(bool value)
        {
            if (value == collapsed)
                return;
            
            Caret.SetState(value);

            if (value)
                List.RemoveFromHierarchy();
            else
                ListContainer.Add(listElement);

            collapsed = value;
        }

        private void ToggleCollapse()
        {
            SetCollapsed(!collapsed);
        }
        
        internal PackageItem AddPackage(Package package)
        {
            var packageItem = new PackageItem(package);
            listElement.Add(packageItem);
            return packageItem;
        }
        
        private VisualContainer List { get { return root.Q<VisualContainer>("groupContainer"); } }
        private VisualContainer ListContainer { get { return root.Q<VisualContainer>("groupContainerOuter"); } }
        private VisualContainer Header { get { return root.Q<VisualContainer>("headerContainer"); } }        
        private Label HeaderTitle { get { return root.Q<Label>("headerTitle"); } }
        private Caret Caret { get { return root.Q<Caret>("headerExpandState"); } }
    }
}
