using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
#if !UNITY_2018_3_OR_NEWER
    internal class ArrowToggleFactory : UxmlFactory<ArrowToggle>
    {
        protected override ArrowToggle DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return new ArrowToggle();
        }
    }
#endif

    internal class ArrowToggle : Arrow
    {
#if UNITY_2018_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<ArrowToggle> {}
#endif

        private bool _Expanded;
        public bool Expanded
        {
            get { return _Expanded; }
            set
            {
                _Expanded = value;

                this.EnableClassToggle("expanded", "collapsed", Expanded);

                if (_Expanded)
                    SetDirection(Direction.Down);
                else
                    SetDirection(Direction.Right);
            }
        }

        public ArrowToggle()
        {
            Expanded = false;
        }

        public void Toggle()
        {
            Expanded = !Expanded;
        }
    }
}