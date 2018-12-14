using UnityEngine;
using NUnit.Framework;
using UnityEngine.Experimental.Localization;

namespace UnityEditor.Experimental.Localization.Tests
{
    [Category("Localization")]
    public class LocalizationSettingsVerification
    {
        [Test]
        public void RemovingSelectedLocaleRevertsToDefaultLocale()
        {
            var settings = LocalizationSettings.CreateDefault();
            LocalizationSettings.instance = settings;

            var locales = settings.GetAvailableLocales();
            locales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.Arabic)));
            locales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.English)));
            locales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.French)));
            locales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.German)));
            locales.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.Japanese)));
            var englishLocale = locales.GetLocale(SystemLanguage.English);
            var germanLocale = locales.GetLocale(SystemLanguage.German);
            locales.defaultLocale = englishLocale;
            settings.SetStartupLocaleSelector(ScriptableObject.CreateInstance<DefaultLocaleSelector>());

            Assert.AreEqual(englishLocale, settings.GetSelectedLocale(), "Expected English to be the selected locale as it is set as the default.");
            settings.SetSelectedLocale(germanLocale);
            Assert.AreEqual(germanLocale, settings.GetSelectedLocale(), "Expected German to be the selected locale as it just assigned.");
            Assert.IsTrue(locales.RemoveLocale(germanLocale), "Expected the German Locale to be removed but the RemoveLocale returned false.");
            Assert.AreEqual(englishLocale, settings.GetSelectedLocale(), "Expected English to become the selected locale as German was removed.");

            Object.DestroyImmediate(settings);
        }
    }
}