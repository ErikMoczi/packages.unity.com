using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class LoadingSpinnerFactory : UxmlFactory<LoadingSpinner>
    {
        protected override LoadingSpinner DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new LoadingSpinner();
        }
    }
#endif

    internal class LoadingSpinner : VisualElement
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<LoadingSpinner, UxmlTraits> { }

        internal new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
#endif

        public bool InvertColor { get; set; }
        public bool Started { get; private set; }

        private int rotation;

        public LoadingSpinner()
        {
            InvertColor = false;
            Started = false;
            clippingOptions = ClippingOptions.NoClipping;
            UIUtils.SetElementDisplay(this, false);
        }

        private void UpdateProgress()
        {
            if (parent == null)
                return;

            parent.transform.rotation = Quaternion.Euler(0, 0, rotation);
            rotation += 3;
            if (rotation > 360)
                rotation -= 360;
        }

        public void Start()
        {
            if (Started)
                return;

            // Weird hack to make sure loading spinner doesn't generate an error every frame.
            // Cannot put in constructor as it give really strange result.
            if (parent != null)
                parent.clippingOptions = ClippingOptions.NoClipping;
            if (parent != null && parent.parent != null)
                parent.parent.clippingOptions = ClippingOptions.ClipAndCacheContents;

            rotation = 0;

            EditorApplication.update += UpdateProgress;

            Started = true;
            UIUtils.SetElementDisplay(this, true);
        }

        public void Stop()
        {
            if (!Started)
                return;

            EditorApplication.update -= UpdateProgress;

            Started = false;
            UIUtils.SetElementDisplay(this, false);
        }
    }
}
