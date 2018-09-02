using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class LoadingSpinnerFactory : UxmlFactory<LoadingSpinner>
    {
        protected override LoadingSpinner DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new LoadingSpinner();
        }
    }

    internal class LoadingSpinner : VisualElement
    {
        public bool InvertColor { get; set; }
        public bool Started { get; private set; }

        private int rotation;

        public LoadingSpinner()
        {
            InvertColor = false;
            Started = false;
            UIUtils.SetElementDisplay(this, false);
        }
        
        void UpdateProgress()
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
