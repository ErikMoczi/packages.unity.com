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
        public event Action OnLoaded = delegate { };

        private readonly VisualElement root;
        private const string emptyId = "emptyArea";
        private const string loadingId = "loadingAreaContainer";
        private const string loadingSpinnerId = "loadingSpinnerContainer";
        private PackageItem selectedItem;
        private List<PackageGroup> Groups;

        public PackageList()
        {
            Groups = new List<PackageGroup>();
            
            root = Resources.Load<VisualTreeAsset>("Templates/PackageList").CloneTree(null);
            Add(root);
            root.StretchToParentSize();

            root.Q<VisualElement>(emptyId).visible = false;
            root.Q<VisualElement>(loadingId).visible = true;
            root.Q<VisualElement>(loadingId).StretchToParentSize();
            root.Q<VisualElement>(loadingSpinnerId).clippingOptions = ClippingOptions.NoClipping;
            LoadingSpinner.Start();

            PackageCollection.Instance.OnPackagesChanged += SetPackages;
            PackageCollection.Instance.OnFilterChanged += OnFilterChanged;
            
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            
            Reload();
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void ScrollIfNeeded()
        {
            if (selectedItem == null)
                return;
            
            var minY = List.worldBound.yMin;
            var maxY = List.worldBound.yMax;
            var itemMinY = selectedItem.worldBound.yMin;
            var itemMaxY = selectedItem.worldBound.yMax;
            var scroll = List.scrollOffset;

            if (itemMinY < minY)
            {
                scroll.y -= minY - itemMinY;
                List.scrollOffset = scroll;
            }
            else if (itemMaxY > maxY)
            {
                scroll.y += itemMaxY - maxY;
                List.scrollOffset = scroll;
            }
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (selectedItem == null)
                return;
            
            if (evt.keyCode == KeyCode.UpArrow)
            {
                if (selectedItem.previousItem != null)
                {
                    Select(selectedItem.previousItem);
                    ScrollIfNeeded();
                }
                else if (selectedItem.packageGroup != null && selectedItem.packageGroup.previousGroup != null)
                {
                    selectedItem.packageGroup.previousGroup.SetCollapsed(false);
                    Select(selectedItem.packageGroup.previousGroup.lastPackage);
                    ScrollIfNeeded();
                }
                evt.StopPropagation();
                return;
            } 
            
            if (evt.keyCode == KeyCode.DownArrow)
            {
                if (selectedItem.nextItem != null)
                {
                    Select(selectedItem.nextItem);
                    ScrollIfNeeded();
                }
                else if (selectedItem.packageGroup != null && selectedItem.packageGroup.nextGroup != null)
                {
                    selectedItem.packageGroup.nextGroup.SetCollapsed(false);
                    Select(selectedItem.packageGroup.nextGroup.firstPackage);
                    ScrollIfNeeded();
                }
                evt.StopPropagation();
                return;
            }
            
#if UNITY_2018_2_OR_NEWER
            if (evt.keyCode == KeyCode.LeftArrow)
            {
                if (selectedItem.packageGroup != null)
                {
                    if (!selectedItem.packageGroup.Collapsed)
                    {
                        selectedItem.packageGroup.SetCollapsed(true);
                        if (selectedItem.packageGroup.nextGroup != null)
                        {
                            selectedItem.packageGroup.nextGroup.SetCollapsed(false);
                            Select(selectedItem.packageGroup.nextGroup.firstPackage);
                            ScrollIfNeeded();
                        } 
                        else if (selectedItem.packageGroup.previousGroup != null)
                        {
                            selectedItem.packageGroup.previousGroup.SetCollapsed(false);
                            Select(selectedItem.packageGroup.previousGroup.lastPackage);
                            ScrollIfNeeded();
                        }
                    }
                }
                evt.StopPropagation();
                return;
            }
#endif

            if (evt.keyCode == KeyCode.PageUp)
            {
                if (selectedItem.packageGroup != null)
                {
                    if (selectedItem == selectedItem.packageGroup.lastPackage && selectedItem != selectedItem.packageGroup.firstPackage)
                    {
                        Select(selectedItem.packageGroup.firstPackage);
                        ScrollIfNeeded();
                    }
                    else if (selectedItem == selectedItem.packageGroup.firstPackage && selectedItem.packageGroup.previousGroup != null)
                    {
                        if (selectedItem.packageGroup.previousGroup.Collapsed)
                            selectedItem.packageGroup.previousGroup.SetCollapsed(false);

                        Select(selectedItem.packageGroup.previousGroup.lastPackage);
                        ScrollIfNeeded();
                    }
                    else if (selectedItem != selectedItem.packageGroup.lastPackage && selectedItem != selectedItem.packageGroup.firstPackage)
                    {
                        Select(selectedItem.packageGroup.firstPackage);
                        ScrollIfNeeded();
                    }
                }
                evt.StopPropagation();
                return;
            }
            
            if (evt.keyCode == KeyCode.PageDown)
            {
                if (selectedItem.packageGroup != null)
                {
                    if (selectedItem == selectedItem.packageGroup.firstPackage && selectedItem != selectedItem.packageGroup.lastPackage)
                    {
                        Select(selectedItem.packageGroup.lastPackage);
                        ScrollIfNeeded();
                    }
                    else if (selectedItem == selectedItem.packageGroup.lastPackage && selectedItem.packageGroup.nextGroup != null)
                    {
                        if (selectedItem.packageGroup.nextGroup.Collapsed)
                            selectedItem.packageGroup.nextGroup.SetCollapsed(false);
                        
                        Select(selectedItem.packageGroup.nextGroup.firstPackage);
                        ScrollIfNeeded();
                    }
                    else if (selectedItem != selectedItem.packageGroup.firstPackage && selectedItem != selectedItem.packageGroup.lastPackage)
                    {
                        Select(selectedItem.packageGroup.lastPackage);
                        ScrollIfNeeded();
                    }
                }
                evt.StopPropagation();
            }
        }


        private void OnFilterChanged(PackageFilter filter)
        {
            ClearAll();
            if (!LoadingSpinner.Started)
            {
                root.Q<VisualElement>(loadingId).visible = true;
                LoadingSpinner.Start();
            }
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

            root.Q<VisualElement>(emptyId).visible = false;
            if (LoadingSpinner.Started)
            {
                root.Q<VisualElement>(loadingId).visible = false;
                LoadingSpinner.Stop();
            }
        }
        
        private void SetPackages(IEnumerable<Package> packages)
        {
            OnLoaded();
            ClearAll();

            var packagesGroup = new PackageGroup(PackageGroupOrigins.Packages.ToString());
            Groups.Add(packagesGroup);
            List.Add(packagesGroup);

            var builtInGroup = new PackageGroup(PackageGroupOrigins.BuiltInPackages.ToString());
            Groups.Add(builtInGroup);
            List.Add(builtInGroup);

            packagesGroup.previousGroup = null;
#if UNITY_2018_2_OR_NEWER
            packagesGroup.nextGroup = builtInGroup;
            builtInGroup.previousGroup = packagesGroup;
            builtInGroup.nextGroup = null;
#else
            packagesGroup.nextGroup = null;
            UIUtils.SetElementDisplay(builtInGroup, false);
#endif

            root.Q<VisualElement>(loadingId).visible = false;
            LoadingSpinner.Stop();

            foreach (var package in packages)
            {
                AddPackage(package);                
            }

            root.Q<VisualElement>(emptyId).visible = !packages.Any();
        }

        private void AddPackage(Package package)
        {
            var groupName = package.Latest.Group;
            var group = GetOrCreateGroup(groupName);
            var packageItem = group.AddPackage(package);

            if (selectedItem == null && !group.Collapsed)
                Select(packageItem);

            packageItem.OnSelected += Select;
        }

        private PackageGroup GetOrCreateGroup(string groupName)
        {
            foreach (var g in Groups)
            {
                if (g.name == groupName)
                    return g;
            }

            var group = new PackageGroup(groupName);
            var latestGroup = Groups.LastOrDefault();
            Groups.Add(group);
            List.Add(group);

            group.previousGroup = null;
            if (latestGroup != null)
            {
                latestGroup.nextGroup = group;
                group.previousGroup = latestGroup;
                group.nextGroup = null;
            }
            return group;
        }

        private void ClearSelection()
        {
            Select(null);            
        }
        
        private void Select(PackageItem packageItem)
        {
            if (packageItem == selectedItem)
                return;

            if (selectedItem != null)
                selectedItem.SetSelected(false);        // Clear Previous selection
            
            selectedItem = packageItem;
            if (selectedItem == null) 
                return;
            
            selectedItem.SetSelected(true);
            OnSelected(selectedItem.package);
        }

        private ScrollView List { get { return root.Q<ScrollView>("scrollView"); } }
        private LoadingSpinner LoadingSpinner { get { return root.Q<LoadingSpinner>("loadingSpinner"); } }
    }
}