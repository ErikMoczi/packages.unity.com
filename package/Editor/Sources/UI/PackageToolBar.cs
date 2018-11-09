using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageToolbar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageToolbar> {}

        private readonly VisualElement root;
        private Selection Selection;

        public PackageToolbar()
        {
            root = Resources.GetTemplate("PackageToolbar.uxml");
            Add(root);
            Cache = new VisualElementCache(root);
        }

        public void SetSelection(Selection selection)
        {
            Selection = selection;
        }

        private VisualElementCache Cache { get; set; }
        private Label ContextInformation { get { return Cache.Get<Label>("contextInformation"); } }
    }
}
