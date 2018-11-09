using UnityEngine;
using UnityEngine.UIElements;

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
        internal new class UxmlFactory : UxmlFactory<LoadingSpinner, UxmlTraits>
        {
        }

        // This works around an issue with UXML instantiation
        // See https://fogbugz.unity3d.com/f/cases/1046459/
        internal new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                UIUtils.SetElementDisplay(ve, false);
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
            if (parent != null && parent.parent != null)
            {
                // TODO the "hack" has been upgraded according to UIElements API, but we should make sure it still is necessary
                parent.parent.cacheAsBitmap = true;
                parent.parent.style.overflow = Overflow.Hidden;
            }
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
