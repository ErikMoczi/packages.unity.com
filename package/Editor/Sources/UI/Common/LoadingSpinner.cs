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
        private int framePerTick = 8;
        private int frame = 0;
        private Texture2D originalBackground;
        
        public bool InvertColor { get; set; }
        
        public LoadingSpinner()
        {
            InvertColor = false;
            originalBackground = style.backgroundImage;
        }
        
        void UpdateProgress()
        {
            if (framePerTick++ < 8)
                return;

            if (panel == null)
            {
                Stop();
                return;
            }

            framePerTick = 0;

            var useDark = EditorGUIUtility.isProSkin || InvertColor;
            
            // Logic here can get complex real fast. Perhaps use a png spinner with alpha instead?
            var spinner = "Images/Spinner/Light/frame_{0}_delay-0.03s";
            if (useDark)
                spinner = "Images/Spinner/Dark/frame_{0}_delay-0.03s";
                    
            var frameStr = (frame++ % 30).ToString().PadLeft(2, '0');
            
            var tex = Resources.Load<Texture2D>(string.Format(spinner, frameStr));
            style.backgroundImage = tex;
            DoRepaint();
        }

        public void Start()
        {
            visible = true;
            EditorApplication.update += UpdateProgress;
        }

        public void Stop()
        {
            EditorApplication.update -= UpdateProgress;

            style.backgroundImage = originalBackground;
            visible = false;
        }
    }
}
