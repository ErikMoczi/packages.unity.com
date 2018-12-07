using JetBrains.Annotations;

namespace Unity.Tiny
{
    [ContextManager(ContextUsage.Edit | ContextUsage.LiveLink), UsedImplicitly]
    internal class UnityManager : ContextManager, IUnityManagerInternal
    {
        private TinyProject Project { get; set; }
        
        public UnityManager(TinyContext context) : base(context)
        {
        }

        public override void Load()
        {
            Bridge.EditorApplication.RegisterContextualUpdate(Update, int.MaxValue);
            Project = Registry.AnyByType<TinyProject>();
            Bridge.SceneView.SetTinyMode();
            SetTinySize();
        }

        public override void Unload()
        {
            Bridge.EditorApplication.UnregisterContextualUpdate(Update);
        }

        private static void SetGameViewSize(int width, int height)
        {
            Bridge.GameView.SetSize(width, height);
        }

        private static void SetGameViewFreeAspect()
        {
            Bridge.GameView.SetFreeAspect();
        }

        public bool ForceSize { get; set; } = true;


        private void Update()
        {
            if (ForceSize)
            {
                SetTinySize();
            }
        }

        private void SetTinySize()
        {
            var settings = Project.Settings;
            if (settings.CanvasAutoResize)
            {
                SetGameViewFreeAspect();
            }
            else
            {
                SetGameViewSize(settings.CanvasWidth, settings.CanvasHeight);
            }
        }
    }
}