using System.Collections.Generic;

namespace UnityEngine.Experimental.Localization
{
    /// <summary>
    /// The locales that are currently available to this application.
    /// </summary>
    public abstract class AvailableLocales : ScriptableObject
    {
        /// <summary>
        /// The list of all supported locales.
        /// </summary>
        public abstract List<Locale> locales { get; set; }

        /// <summary>
        /// The default Locale to use.
        /// </summary>
        public abstract Locale defaultLocale { get; set; }

        /// <summary>
        /// Attempt to retrieve a Locale using the identifier.
        /// </summary>
        /// <param name="id"><see cref="LocaleIdentifier"/> to find.</param>
        /// <returns>If no Locale can be found then null is returned.</returns>
        public abstract Locale GetLocale(LocaleIdentifier id);

        /// <summary>
        /// Attempt to retrieve a Locale using a code.
        /// </summary>
        /// <param name="code">If no Locale can be found then null is returned.</param>
        public abstract Locale GetLocale(string code);

        /// <summary>
        /// Attempt to retrieve a Locale using an id.
        /// </summary>
        /// <returns>If no Locale can be found then null is returned.</returns>
        public abstract Locale GetLocale(int id);

        /// <summary>
        /// Attempt to retrieve a Locale using a <see cref="UnityEngine.SystemLanguage"/>.
        /// </summary>
        /// <returns>If no Locale can be found then null is returned.</returns>
        public Locale GetLocale(SystemLanguage systemLanguage)
        {
            return GetLocale(SystemLanguageConverter.GetSystemLanguageCultureCode(systemLanguage));
        }

        /// <summary>
        /// Add a Locale to allow support for a specific language.
        /// </summary>
        public abstract void AddLocale(Locale locale);

        /// <summary>
        /// Removes support for a specific Locale.
        /// </summary>
        /// <param name="locale">The locale that should be removed if possible.</param>
        /// <returns>true if the locale was removed or false if the locale did not exist.</returns>
        public virtual bool RemoveLocale(Locale locale)
        {
            var settings = LocalizationSettings.GetInstanceDontCreateDefault();
            if(settings != null)
                settings.OnLocaleRemoved(locale);
            return true;
        }
    }
}