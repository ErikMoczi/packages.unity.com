using System.Globalization;

namespace UnityEngine.Experimental.Localization.Samples
{
    /// <summary>
    /// This example shows how a simple language selection menu can be implemented.
    /// </summary>
    public class LanguageSelectionMenuExample : MonoBehaviour
    {
        public Rect windowRect = new Rect(0, 0, 200, 300);
        public Color selectColor = Color.yellow;
        public Color defaultColor = Color.gray;
        Vector2 m_ScrollPos;

        [Tooltip("Use the current active settings if possible or create a new one for the example")]
        public bool useActiveLocalizationSettings = false;

        void Start()
        {
            var localizationSettings = LocalizationSettings.GetInstanceDontCreateDefault();

            // Use included settings if one is available.
            if (useActiveLocalizationSettings && localizationSettings != null)
            {
                Debug.Log("Using included localization data");
                return;
            }

            Debug.Log("Creating default localization data");

            // Create our localization settings. If a LocalizationSettings asset has been created and configured in the editor then we can leave this step out.
            localizationSettings = LocalizationSettings.CreateDefault();
            var supportedLocales = localizationSettings.GetAvailableLocales();

            // Add the locales we support
            supportedLocales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.Arabic)));
            supportedLocales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.English)));
            supportedLocales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.French)));
            supportedLocales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.German)));
            supportedLocales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.Japanese)));
            supportedLocales.AddLocale(Locale.CreateLocale(CultureInfo.InvariantCulture));

            // Set English to be our default
            supportedLocales.defaultLocale = supportedLocales.GetLocale(SystemLanguage.English);
            localizationSettings.GetSelectedLocaleChangedEvent().AddListener(OnSelectedLocaleChanged);
            LocalizationSettings.instance = localizationSettings;
        }

        static void OnSelectedLocaleChanged(Locale newLocale)
        {
            Debug.Log("OnSelectedLocaleChanged: The locale just changed to " + newLocale);
        }

        void OnGUI()
        {
            windowRect = GUI.Window(GetHashCode(), windowRect, DrawWindowContents, "Select Language");
        }

        void DrawWindowContents(int id)
        {
            var supported = LocalizationSettings.availableLocales;

            if (supported.locales.Count == 0)
            {
                GUILayout.Label("No Locales included in the active Localization Settings.");
            }
            else
            {
                var originalColor = GUI.contentColor;

                m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);
                for (int i = 0; i < supported.locales.Count; ++i)
                {
                    var locale = supported.locales[i];

                    GUI.contentColor = LocalizationSettings.selectedLocale == locale ? selectColor : defaultColor;

                    if (GUILayout.Button(locale.name))
                    {
                        LocalizationSettings.selectedLocale = locale;
                    }
                }
                GUILayout.EndScrollView();

                GUI.contentColor = originalColor;
            }

            GUI.DragWindow();
        }
    }
}