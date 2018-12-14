using UnityEngine;

namespace UnityEngine.Experimental.Localization
{
    /// <summary>
    /// Returns the <see cref="AvailableLocales"/> default locale property.
    /// </summary>
    [CreateAssetMenu(menuName = "Localization/Startup Locale Selectors/Default Locale")]
    public class DefaultLocaleSelector : StartupLocaleSelector
    {
        public override Locale GetStartupLocale(AvailableLocales availableLocales)
        {
            return availableLocales.defaultLocale;
        }
    }
}