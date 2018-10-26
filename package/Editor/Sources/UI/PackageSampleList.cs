using System.Linq;
using System.Collections.Generic;
using Semver;
using UnityEngine.Experimental.UIElements;

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
        internal new class UxmlFactory : UxmlFactory<PackageSampleList> { }
#endif

        private readonly VisualElement root;

        public PackageSampleList()
        {
            root = Resources.GetTemplate("PackageSampleList.uxml");
            Add(root);
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
            foreach(var sample in package.Samples)
            {
                var sampleItem = new PackageSampleItem(sample);
                ImportStatusContainer.Add(sampleItem.ImportStatus);
                NameLabelContainer.Add(sampleItem.NameLabel);
                SizeLabelContainer.Add(sampleItem.SizeLabel);
                ImportButtonContainer.Add(sampleItem.ImportButton);
                sampleItem.ImportButton.SetEnabled(package.IsCurrent);
            }
        }

        private VisualElement _importStatusContainer;
        internal VisualElement ImportStatusContainer { get { return _importStatusContainer ?? (_importStatusContainer = root.Q<VisualElement>("importStatusContainer")); } }
        private VisualElement _nameLabelContainer;
        internal VisualElement NameLabelContainer { get { return _nameLabelContainer ?? (_nameLabelContainer = root.Q<VisualElement>("nameLabelContainer")); } }
        private VisualElement _sizeLabelContainer;
        internal VisualElement SizeLabelContainer { get { return _sizeLabelContainer ?? (_sizeLabelContainer = root.Q<VisualElement>("sizeLabelContainer")); } }
        private VisualElement _importButtonContainer;
        internal VisualElement ImportButtonContainer { get { return _importButtonContainer ?? (_importButtonContainer = root.Q<VisualElement>("importButtonContainer")); } }
    }
}
