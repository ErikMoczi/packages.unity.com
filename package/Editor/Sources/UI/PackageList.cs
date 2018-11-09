using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class PackageListFactory : UxmlFactory<PackageList>
    {
        protected override PackageList DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageList();
        }
    }
#endif

    internal class PackageList : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<PackageList> {}
#endif

        public event Action OnLoaded = delegate {};
        public event Action OnFocusChange = delegate {};

        private readonly VisualElement root;
        private List<PackageGroup> Groups;
        private Selection Selection;

        internal PackageSearchFilter searchFilter;

        public PackageItem SelectedItem
        {
            get
            {
                var selected = GetSelectedElement();
                if (selected == null)
                    return null;

                var element = selected.Element;
                return UIUtils.GetParentOfType<PackageItem>(element);
            }
        }

        public PackageList()
        {
            Groups = new List<PackageGroup>();

            root = Resources.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            Cache = new VisualElementCache(root);

            List.contentContainer.AddToClassList("fix-scroll-view");

            UIUtils.SetElementDisplay(Empty, false);
            UIUtils.SetElementDisplay(NoResult, false);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        public void GrabFocus()
        {
            if (SelectedItem == null)
                return;

            SelectedItem.Focus();
        }

        public void ShowResults(PackageItem item)
        {
            NoResultText.text = string.Empty;
            UIUtils.SetElementDisplay(NoResult, false);

            // Only select main element if none of its versions are already selected
            var hasSelection = item.GetSelectionList().Any(i => Selection.IsSelected(i.TargetVersion));
            if (!hasSelection)
                item.SelectMainItem();

            EditorApplication.delayCall += ScrollIfNeededDelayed;

            UpdateGroups();
        }

        public void ShowNoResults()
        {
            NoResultText.text = string.Format("No results for \"{0}\"", searchFilter.SearchText);
            UIUtils.SetElementDisplay(NoResult, true);
            foreach (var group in Groups)
            {
                UIUtils.SetElementDisplay(group, false);
            }
        }

        public void SetSearchFilter(PackageSearchFilter filter)
        {
            searchFilter = filter;
        }

        public void SetSelection(Selection selection)
        {
            Selection = selection;
        }

        private void UpdateGroups()
        {
            foreach (var group in Groups)
            {
                PackageItem firstPackage = null;
                PackageItem lastPackage = null;

                var listGroup = group.Query<PackageItem>().ToList();
                foreach (var item in listGroup)
                {
                    if (!item.visible)
                        continue;

                    if (firstPackage == null) firstPackage = item;
                    lastPackage = item;
                }

                if (firstPackage == null && lastPackage == null)
                {
                    UIUtils.SetElementDisplay(group, false);
                }
                else
                {
                    UIUtils.SetElementDisplay(group, true);
                    group.firstPackage = firstPackage;
                    group.lastPackage = lastPackage;
                }
            }
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void ScrollIfNeededDelayed() {ScrollIfNeeded();}

        private void ScrollIfNeeded(VisualElement target = null)
        {
            EditorApplication.delayCall -= ScrollIfNeededDelayed;
            UIUtils.ScrollIfNeeded(List, target);
        }

        private void SetSelectedExpand(bool value)
        {
            var selected = SelectedItem;
            if (selected == null) return;

            selected.SetExpand(value);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Tab)
            {
                OnFocusChange();
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.RightArrow)
            {
                SetSelectedExpand(true);
                evt.StopPropagation();
            }

            if (evt.keyCode == KeyCode.LeftArrow)
            {
                var selected = SelectedItem;
                SetSelectedExpand(false);

                // Make sure the main element get selected to not lose the selected element
                if (selected != null)
                    selected.SelectMainItem();

                evt.StopPropagation();
            }

            if (evt.keyCode == KeyCode.UpArrow)
            {
                if (SelectBy(-1))
                    evt.StopPropagation();
            }

            if (evt.keyCode == KeyCode.DownArrow)
            {
                if (SelectBy(1))
                    evt.StopPropagation();
            }
        }

        public List<IPackageSelection> GetSelectionList()
        {
            return Groups.SelectMany(g => g.GetSelectionList()).ToList();
        }

        private bool SelectBy(int delta)
        {
            var list = GetSelectionList();
            var selection = GetSelectedElement(list);
            if (selection != null)
            {
                var index = list.IndexOf(selection);
                var nextIndex = index + delta;

                if (nextIndex >= list.Count)
                    return false;
                if (nextIndex < 0)
                    return false;

                var nextElement = list.ElementAt(nextIndex);
                Selection.SetSelection(nextElement.TargetVersion);

                foreach (var scrollView in UIUtils.GetParentsOfType<ScrollView>(nextElement.Element))
                    UIUtils.ScrollIfNeeded(scrollView, nextElement.Element);
            }

            return true;
        }

        private IPackageSelection GetSelectedElement(List<IPackageSelection> list = null)
        {
            list = list ?? GetSelectionList();
            var selection = list.Find(s => Selection.IsSelected(s.TargetVersion));

            return selection;
        }

        private void ClearAll()
        {
            List.Clear();
            Groups.Clear();

            UIUtils.SetElementDisplay(Empty, false);
            UIUtils.SetElementDisplay(NoResult, false);
        }

        public void SetPackages(PackageFilter filter, IEnumerable<Package> packages)
        {
            if (filter == PackageFilter.Modules)
            {
                packages = packages.Where(pkg => pkg.IsBuiltIn);
            }
            else if (filter == PackageFilter.All)
            {
                packages = packages.Where(pkg => !pkg.IsBuiltIn);
            }
            else
            {
                packages = packages.Where(pkg => !pkg.IsBuiltIn);
                packages = packages.Where(pkg => pkg.Current != null);
            }

            OnLoaded();
            ClearAll();

            var packagesGroup = new PackageGroup(PackageGroupOrigins.Packages.ToString(), Selection);
            Groups.Add(packagesGroup);
            List.Add(packagesGroup);
            packagesGroup.previousGroup = null;

            var builtInGroup = new PackageGroup(PackageGroupOrigins.BuiltInPackages.ToString(), Selection);
            Groups.Add(builtInGroup);
            List.Add(builtInGroup);

            if (filter == PackageFilter.Modules)
            {
                packagesGroup.nextGroup = builtInGroup;
                builtInGroup.previousGroup = packagesGroup;
                builtInGroup.nextGroup = null;
            }
            else
            {
                packagesGroup.nextGroup = null;
                UIUtils.SetElementDisplay(builtInGroup, false);
            }

            var items = packages.OrderBy(pkg => pkg.Versions.FirstOrDefault() == null ? pkg.Name : pkg.Versions.FirstOrDefault().DisplayName).ToList();
            foreach (var package in items)
            {
                AddPackage(package);
            }

            if (!Selection.Selected.Any() && items.Any())
                Selection.SetSelection(items.First());

            PackageFiltering.FilterPackageList(this);
        }

        private void AddPackage(Package package)
        {
            var groupName = package.Latest != null ? package.Latest.Group : package.Current.Group;
            var group = GetOrCreateGroup(groupName);
            group.AddPackage(package);
        }

        private PackageGroup GetOrCreateGroup(string groupName)
        {
            foreach (var g in Groups)
            {
                if (g.name == groupName)
                    return g;
            }

            var group = new PackageGroup(groupName, Selection);
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

        private VisualElementCache Cache { get; set; }

        private ScrollView List { get { return Cache.Get<ScrollView>("scrollView"); } }
        private VisualElement Empty { get { return Cache.Get<VisualElement>("emptyArea"); } }
        private VisualElement NoResult { get { return Cache.Get<VisualElement>("noResult"); } }
        private Label NoResultText { get { return Cache.Get<Label>("noResultText"); } }
    }
}
