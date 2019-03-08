using UnityEditor.SettingsManagement;

namespace UnityEditor.SettingsManagement.Examples
{
    class MySetting<T> : UserSetting<T>
    {
        public MySetting(string key, T value, SettingsScope scope = SettingsScope.Project)
            : base(MySettingsManager.instance, key, value, scope)
        {}

        MySetting(Settings settings, string key, T value, SettingsScope scope = SettingsScope.Project)
            : base(settings, key, value, scope) { }
    }
}
