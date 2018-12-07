using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.U2D.Common;

namespace UnityEditor.Experimental.U2D.Animation
{
    public class CopyToolbar : Toolbar
    {
        public class CopyToolbarFactory : UxmlFactory<CopyToolbar, CopyToolbarUxmlTraits> {}
        public class CopyToolbarUxmlTraits : VisualElement.UxmlTraits {}

        public event Action onDoCopy = () => {};
        public event Action onDoPaste = () => {};

        public CopyToolbar()
        {
            AddStyleSheetPath("CopyToolbarStyle");
        }

        public void DoCopy()
        {
            onDoCopy();
        }

        public void DoPaste()
        {
            onDoPaste();
        }

        public void BindElements()
        {
            var copyButton = this.Q<Button>("Copy");
            copyButton.clickable.clicked += DoCopy;

            var pasteButton = this.Q<Button>("Paste");
            pasteButton.clickable.clicked += DoPaste;
        }

        public static CopyToolbar GenerateFromUXML()
        {
            var visualTree = Resources.Load("CopyToolbar") as VisualTreeAsset;
            var clone = visualTree.CloneTree(null).Q<CopyToolbar>("CopyToolbar");
            clone.BindElements();
            return clone;
        }
    }
}
