using System.Linq;
using System.Collections.Generic;
using Semver;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class PackageSampleListFactory : UxmlFactory<PackageSampleList>
    {
        protected override PackageSampleList DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new PackageSampleList();
        }
    }
#endif

    internal class PackageSampleList : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<PackageSampleList> {}
#endif

        private readonly VisualElement root;

        public PackageSampleList()
        {
            root = Resources.GetTemplate("PackageSampleList.uxml");
            Add(root);
            Cache = new VisualElementCache(root);
        }

        public void SetPackage(PackageInfo package)
        {
            ImportStatusContainer.Clear();
            NameLabelContainer.Clear();
            SizeLabelContainer.Clear();
            ImportButtonContainer.Clear();

            if (package == null || package.Samples == null || package.Samples.Count == 0)
            {
                UIUtils.SetElementDisplay(this, false);
                return;
            }
            UIUtils.SetElementDisplay(this, true);
            foreach (var sample in package.Samples)
            {
                var sampleItem = new PackageSampleItem(sample);
                ImportStatusContainer.Add(sampleItem.ImportStatus);
                NameLabelContainer.Add(sampleItem.NameLabel);
                SizeLabelContainer.Add(sampleItem.SizeLabel);
                ImportButtonContainer.Add(sampleItem.ImportButton);
                sampleItem.ImportButton.SetEnabled(package.IsCurrent);
            }
        }

        private VisualElementCache Cache { get; set; }

        internal VisualElement ImportStatusContainer { get { return Cache.Get<VisualElement>("importStatusContainer"); } }
        internal VisualElement NameLabelContainer { get { return Cache.Get<VisualElement>("nameLabelContainer"); } }
        internal VisualElement SizeLabelContainer { get { return Cache.Get<VisualElement>("sizeLabelContainer"); } }
        internal VisualElement ImportButtonContainer { get { return Cache.Get<VisualElement>("importButtonContainer"); } }
    }
}
