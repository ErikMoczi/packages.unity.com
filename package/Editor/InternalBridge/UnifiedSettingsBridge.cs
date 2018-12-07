
using UnityEditor;

namespace Unity.Tiny
{
    internal class UnifiedSettingsBridge
    {
        public static void OpenAndFocusTinySettings()
        {
            var window = SettingsWindow.Show(SettingsScope.Project, "Project/Tiny");
            window?.Focus();
        }
    }
}
