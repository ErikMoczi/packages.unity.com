using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{    
    internal class PackageSearchFilterTabsFactory : UxmlFactory<PackageSearchFilterTabs>
    {
        protected override PackageSearchFilterTabs DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageSearchFilterTabs();
        }
    }

    internal class PackageSearchFilterTabs : VisualElement
    {
        private readonly VisualElement root;
        private const string SelectedClassName = "selected";

        public PackageFilter CurrentFilter { get; internal set; }

        public PackageSearchFilterTabs()
        {
            root = Resources.Load<VisualTreeAsset>("Templates/PackageSearchFilterTabs").CloneTree(null);
            Add(root);
            root.StretchToParentSize();

            LocalButton.AddManipulator(new Clickable(() => SetFilter(PackageFilter.Local)));
            AllButton.AddManipulator(new Clickable(() => SetFilter(PackageFilter.All)));

            OnFilterChanged();
            PackageCollection.Instance.OnFilterChanged += OnFilterChanged;
        }

        private void SetFilter(PackageFilter filter)
        {
            root.SetEnabled(false);
            if (!PackageCollection.Instance.SetFilter(filter))
            {
                root.SetEnabled(true);
            }
        }
        
        private void OnFilterChanged(PackageFilter filter = PackageFilter.None)
        {
            if (filter == PackageFilter.None)
                filter = PackageCollection.Instance.Filter;
            
            CurrentFilter = filter;

            if (filter == PackageFilter.All)
            {
                AllButton.AddToClassList(SelectedClassName);
                LocalButton.RemoveFromClassList(SelectedClassName);
            } 
            else if (filter == PackageFilter.Local)
            {
                LocalButton.AddToClassList(SelectedClassName);
                AllButton.RemoveFromClassList(SelectedClassName);
            }
            
            root.SetEnabled(true);
        }

        private Label LocalButton { get { return root.Q<Label>("local"); } }
        private Label AllButton { get { return root.Q<Label>("all"); } }
    }
}
