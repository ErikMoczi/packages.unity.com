using UnityEditor.SettingsManagement;

namespace UnityEditor.SettingsManagement.Examples
{
    static class MySettingsManager
    {
        internal const string k_ProjectSettingsPath = "ProjectSettings/MySettingsExample.json";

        static Settings s_Instance;

        internal static Settings instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new Settings(new ISettingsRepository[]
                    {
                        new ProjectSettingsRepository(k_ProjectSettingsPath),
                        new UserSettingsRepository()
                    });

                return s_Instance;
            }
        }

        public static void Save()
        {
            instance.Save();
        }

        public static T Get<T>(string key, SettingsScope scope = SettingsScope.Project, T fallback = default(T))
        {
            return instance.Get<T>(key, scope, fallback);
        }

        public static void Set<T>(string key, T value, SettingsScope scope = SettingsScope.Project)
        {
            instance.Set<T>(key, value, scope);
        }

        public static bool ContainsKey<T>(string key, SettingsScope scope = SettingsScope.Project)
        {
            return instance.ContainsKey<T>(key, scope);
        }
    }
}
