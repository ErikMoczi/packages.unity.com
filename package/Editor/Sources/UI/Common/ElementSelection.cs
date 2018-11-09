using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal class ElementSelection
    {
        private IPackageSelection Element { get; set; }
        private Selection Selection { get; set; }

        public ElementSelection(IPackageSelection element, Selection selection)
        {
            Element = element;
            Selection = selection;
            Element.RefreshSelection();

            Selection.OnChanged += OnChanged;
        }

        public void OnChanged(IEnumerable<PackageVersion> selection)
        {
            Selection.OnClear += OnClear;
            Element.RefreshSelection();
        }

        public void OnClear()
        {
            Selection.OnClear -= OnClear;
            Element.RefreshSelection();
        }
    }
}
