using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageVersionLabel : Label, IPackageSelection
    {
        public PackageInfo Version { get; set; }
        private ElementSelection SelectionManager { get; set; }
        private Selection Selection { get; set; }

        public PackageVersionLabel(PackageInfo version, Selection selection)
        {
            Version = version;
            Selection = selection;
            RefreshLabel();
            RefreshSelection();
            SelectionManager = new ElementSelection(this, selection);
            RegisterCallback<MouseDownEvent>(e => selection.SetSelection(version));
        }

        public void RefreshSelection()
        {
            this.EnableClass(ApplicationUtil.SelectedClassName, Selection.IsSelected(Version));
        }

        public PackageInfo TargetVersion { get { return Version; } }
        public VisualElement Element { get { return this; } }

        private void RefreshLabel()
        {
            text = Version.StandardizedLabel();
        }
    }
}
