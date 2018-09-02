using UnityEngine.Experimental.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageListFactory : UxmlFactory<PackageList>
    {
        protected override PackageList DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageList();
        }
    }

    internal class PackageList : VisualElement
    {
        public event Action<Package> OnSelected = delegate { };

        private readonly VisualElement root;
        private const string emptyId = "emptyArea";
        private Package selected;
        private PackageItem selectedItem;
        private SortedDictionary<string, PackageGroup> Groups;

        public PackageList()
        {
            Groups = new SortedDictionary<string, PackageGroup>();
            
            root = Resources.Load<VisualTreeAsset>("Templates/PackageList").CloneTree(null);
            Add(root);
            root.StretchToParentSize();

            PackageCollection.Instance.OnPackagesChanged += SetPackages;
            PackageCollection.Instance.OnFilterChanged += OnFilterChanged;
            
            Reload();
            
            // Hack -- due to an issue with scrollView not laying out its content properly when using templates
            //            (everything inside has size 0). This forces a re-size after layout.
            List.RegisterCallback<PostLayoutEvent>(postLayoutEvent =>
            {
                List.contentContainer.style.minWidth = List.contentViewport.layout.width;
                List.contentContainer.style.minHeight = List.contentViewport.layout.height;
            });
        }

        private void OnFilterChanged(PackageFilter filter)
        {
            ClearAll();
            Spinner.Start();
        }

        private static void Reload()
        {
            // Force a re-init to initial condition
            PackageCollection.Instance.Reset();
        }

        private void ClearAll()
        {
            List.Clear();
            Groups.Clear();
            ClearSelection();            
            Spinner.Stop();
            root.Q<VisualContainer>(emptyId).visible = false;
        }
        
        private void SetPackages(IEnumerable<Package> packages)
        {
            ClearAll();

            foreach (var package in packages)
            {
                AddPackage(package);                
            }

            if (packages.Any())
                root.Q<VisualContainer>(emptyId).visible = false;
            else
            {
                root.Q<VisualContainer>(emptyId).visible = true;                                
            }
        }

        private void AddPackage(Package package)
        {
            var groupName = package.Display.Group;
            var group = GetOrCreateGroup(groupName);
            var packageItem = group.AddPackage(package);

            if (selected == null)
                Select(package, packageItem);

            packageItem.OnSelected += Select;
        }

        private PackageGroup GetOrCreateGroup(string groupName)
        {
            if (groupName == null)
                groupName = "";

            if (!Groups.ContainsKey(groupName))
            {
                var group = new PackageGroup(groupName);
                Groups[groupName] = group;

                // Need to re-build the package group list in order to keep ordering.
                // An alternative way if efficiency becomes problematic would be to
                // add the group at its proper place in the hierarchy.
                List.Clear();
                foreach (var grp in Groups)
                    List.Add(grp.Value);
            }

            return Groups[groupName];
        }

        private void ClearSelection()
        {
            Select(null, null);            
        }
        
        private void Select(Package package, PackageItem selectedItem)
        {
            if (package == selected)
                return;

            if (this.selectedItem != null)
                this.selectedItem.SetSelected(false);        // Clear Previous selection
            
            selected = package;
            this.selectedItem = selectedItem;
            
            if (this.selectedItem != null)
                this.selectedItem.SetSelected(true);
            
            OnSelected(package);
        }

        private ScrollView List { get { return root.Q<ScrollView>("scrollView"); } }
        private LoadingSpinner Spinner { get { return root.Q<LoadingSpinner>("packageListSpinner"); } }
    }
}
