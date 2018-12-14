using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace UnityEngine.Experimental.Localization
{
    /// <summary>
    /// The localization settings is the core component to the localization system.
    /// It provides the entry point to all player based localization features.
    /// </summary>
    [CreateAssetMenu(menuName = "Localization/Empty Localization Settings", order = 0)]
    public class LocalizationSettings : ScriptableObject
    {
        #if UNITY_EDITOR
        /// <summary>
        /// In the editor the active localization settings asset is tracked by using the CustomObject API using this key.
        /// </summary>
        public const string ConfigName = "LocalizationSettings";
        #endif

        [SerializeField]
        LocaleEvent m_SelectedLocaleChanged = new LocaleEvent();

        [SerializeField]
        StartupLocaleSelector m_LocaleSelector;

        [SerializeField]
        AvailableLocales m_AvailableLocales;

        Locale m_SelectedLocale;

        static LocalizationSettings s_Instance;

        /// <summary>
        /// Singleton instance for the Localization Settings.
        /// </summary>
        public static LocalizationSettings instance
        {
            get { return s_Instance ?? (s_Instance = GetOrCreateSettings()); }
            set{ s_Instance = value; }
        }

        /// <summary>
        /// <inheritdoc cref="StartupLocaleSelector"/>
        /// </summary>
        public static StartupLocaleSelector startupLocaleSelector
        {
            get { return instance.GetStartupLocaleSelector(); }
            set { instance.SetStartupLocaleSelector(value); }
        }

        /// <summary>
        /// <inheritdoc cref="AvailableLocales"/>
        /// </summary>
        public static AvailableLocales availableLocales
        {
            get { return instance.GetAvailableLocales(); }
            set { instance.SetAvailableLocales(value); }
        }

        /// <summary>
        /// The current selected locale. This is the locale that will be used when localizing assets.
        /// </summary>
        public static Locale selectedLocale
        {
            get { return instance.GetSelectedLocale(); }
            set { instance.SetSelectedLocale(value); }
        }

        /// <summary>
        /// Event that is sent when the <see cref="selectedLocale"/> is changed.
        /// </summary>
        public static LocaleEvent selectedLocaleChanged
        {
            get { return instance.GetSelectedLocaleChangedEvent(); }
            set { instance.SetSelectedLocaleChangedEvent(value); }
        }

        void OnEnable()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
            }
        }

        /// <summary>
        /// <inheritdoc cref="startupLocaleSelector"/>
        /// </summary>
        public void SetStartupLocaleSelector(StartupLocaleSelector selector)
        {
            m_LocaleSelector = selector;
        }

        /// <summary>
        /// <inheritdoc cref="startupLocaleSelector"/>
        /// </summary>
        public StartupLocaleSelector GetStartupLocaleSelector()
        {
            return m_LocaleSelector;
        }

        /// <summary>
        /// <inheritdoc cref="availableLocales"/>
        /// </summary>
        public void SetAvailableLocales(AvailableLocales available)
        {
            m_AvailableLocales = available;
        }

        /// <summary>
        /// <inheritdoc cref="availableLocales"/>
        /// </summary>
        public AvailableLocales GetAvailableLocales()
        {
            return m_AvailableLocales;
        }

        /// <summary>
        /// <inheritdoc cref="selectedLocale"/>
        /// </summary>
        public void SetSelectedLocale(Locale locale)
        {
            if (!ReferenceEquals(m_SelectedLocale, locale))
            {
                m_SelectedLocale = locale;
                m_SelectedLocaleChanged.Invoke(locale);
            }
        }

        /// <summary>
        /// <inheritdoc cref="selectedLocale"/>
        /// </summary>
        public Locale GetSelectedLocale()
        {
            if (m_SelectedLocale == null)
            {
                Debug.Assert(m_AvailableLocales != null, "Available locales is null, can not pick a selected locale.");
                m_SelectedLocale = startupLocaleSelector.GetStartupLocale(m_AvailableLocales) ?? m_AvailableLocales.defaultLocale;
            }
            return m_SelectedLocale;
        }

        /// <summary>
        /// <inheritdoc cref="selectedLocaleChanged"/>
        /// </summary>
        public LocaleEvent GetSelectedLocaleChangedEvent()
        {
            return m_SelectedLocaleChanged;
        }

        /// <summary>
        /// <inheritdoc cref="selectedLocaleChanged"/>
        /// </summary>
        public void SetSelectedLocaleChangedEvent(LocaleEvent localeEvent)
        {
            m_SelectedLocaleChanged = localeEvent;
        }

        public void OnLocaleRemoved(Locale locale)
        {
            Debug.Log("Locale removed");
            if (locale == GetSelectedLocale())
                SetSelectedLocale(null);
        }

        /// <summary>
        /// Returns the singleton of the LocalizationSettings but does not create a default one if no active settings are found.
        /// </summary>
        /// <returns></returns>
        public static LocalizationSettings GetInstanceDontCreateDefault()
        {
            if(s_Instance != null)
                return s_Instance;

            LocalizationSettings settings;
            #if UNITY_EDITOR
            UnityEditor.EditorBuildSettings.TryGetConfigObject(ConfigName, out settings);
            #else
            settings = FindObjectOfType<LocalizationSettings>();
            #endif
            return settings;
        }

        static LocalizationSettings GetOrCreateSettings()
        {
            var settings = GetInstanceDontCreateDefault();
            if (settings == null)
            {
                Debug.LogWarning("Could not find localization settings. Default will be used.");
                settings = CreateDefault();
                settings.name = "Default Localization Settings";
            }

            return settings;
        }

        /// <summary>
        /// Creates a <see cref="LocalizationSettings"/> setup with all the default delegates needed to localize your application.
        /// </summary>
        /// <param name="createdDependencies">Optional list which will be populated with additional scriptable objects that are created for the localization settings.</param>
        /// <returns>The new localization settings asset.</returns>
        public static LocalizationSettings CreateDefault(List<ScriptableObject> createdDependencies = null)
        {
            var localizationSettings = CreateInstance<LocalizationSettings>();
            localizationSettings.name = "Localization Settings";

            // Selector
            var localeSelectorCollection = CreateInstance<StartupLocaleSelectorCollection>();
            localizationSettings.m_LocaleSelector = localeSelectorCollection;
            localeSelectorCollection.name = "Locale Selector Collection";
            var commandLine = CreateInstance<CommandLineLocaleSelector>();
            commandLine.name = "Command Line Locale Selector";
            var systemLocale = CreateInstance<SystemLocaleSelector>();
            systemLocale.name = "System Locale Selector";
            var defaultLocale = CreateInstance<DefaultLocaleSelector>();
            defaultLocale.name = "Default Locale Selector";
            localeSelectorCollection.startupSelectors = new List<StartupLocaleSelector>{ commandLine, systemLocale, defaultLocale };

            // Locales
            var localesCollection = CreateInstance<LocalesCollection>();
            localesCollection.name = "Available Locales";
            localizationSettings.m_AvailableLocales = localesCollection;

            if (createdDependencies != null)
            {
                createdDependencies.Add(localeSelectorCollection);
                createdDependencies.Add(localesCollection);
                createdDependencies.AddRange(localeSelectorCollection.startupSelectors.Cast<ScriptableObject>());
            }
            return localizationSettings;
        }
    }
}