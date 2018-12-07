namespace UnityEditor.TestTools.TestRunner
{
    internal class SwitchPlatformSetup : IPlatformSetup
    {
        public void Setup()
        {
            EditorUserBuildSettings.switchCreateRomFile = true;
            EditorUserBuildSettings.switchNVNGraphicsDebugger = false;
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.switchRedirectWritesToHostMount = true;
        }

        public void PostBuildAction()
        {
        }

        public void PostSuccessfulBuildAction()
        {
        }

        public void CleanUp()
        {
        }
    }
}
