using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

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
        internal new class UxmlFactory : UxmlFactory<PackageGroup> {}
#endif

        private readonly VisualElement root;
        internal readonly PackageGroupOrigins Origin;
        private Selection Selection;

        public PackageGroup previousGroup;
        public PackageGroup nextGroup;

        public PackageItem firstPackage;
        public PackageItem lastPackage;

        public PackageGroup() : this(string.Empty, null)
        {
        }

        public PackageGroup(string groupName, Selection selection)
        {
            name = groupName;
            root = Resources.GetTemplate("PackageGroup.uxml");
            Add(root);
            Cache = new VisualElementCache(root);

            Selection = selection;

            if (string.IsNullOrEmpty(groupName) || groupName != PackageGroupOrigins.BuiltInPackages.ToString())
            {
                HeaderTitle.text = "Packages";
                Origin = PackageGroupOrigins.Packages;
            }
            else
            {
                HeaderTitle.text = "Built In Packages";
                Origin = PackageGroupOrigins.BuiltInPackages;
            }
        }

        public IEnumerable<IPackageSelection> GetSelectionList()
        {
            foreach (var item in List.Children().Cast<PackageItem>())
                foreach (var selection in item.GetSelectionList())
                    yield return selection;
        }

        internal PackageItem AddPackage(Package package)
        {
            var packageItem = new PackageItem(package, Selection);

            List.Add(packageItem);

            if (firstPackage == null) firstPackage = packageItem;
            lastPackage = packageItem;

            return packageItem;
        }

        private VisualElementCache Cache { get; set; }
        private VisualElement List { get { return Cache.Get<VisualElement>("groupContainer"); } }
        private Label HeaderTitle { get { return Cache.Get<Label>("headerTitle"); } }
    }
}
