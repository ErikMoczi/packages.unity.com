using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.Experimental.Localization.Tests
{
    [Category("Localization")]
    public class LocalizationSettingsTests
    {
        public static void CreateTestLocalizationSettings()
        {
            var settings = LocalizationSettings.CreateDefault();
            LocalizationSettings.instance = settings;

            var sl = LocalizationSettings.availableLocales;
            sl.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.Arabic)));
            sl.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.English)));
            sl.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.French)));
            sl.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.German)));
            sl.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.Japanese)));
            sl.defaultLocale = sl.GetLocale(SystemLanguage.English);
            LocalizationSettings.startupLocaleSelector = ScriptableObject.CreateInstance<DefaultLocaleSelector>();
        }

        public static void CreateTestLocalizationSettingsWithFallbacks()
        {
            CreateTestLocalizationSettings();

            // Setup some fall backs
            var localeEnUs = Locale.CreateLocale(new LocaleIdentifier("en-US"));
            var localeEnGb = Locale.CreateLocale(new LocaleIdentifier("en-GB"));
            var sl = LocalizationSettings.availableLocales;
            sl.AddLocale(localeEnUs);
            sl.AddLocale(localeEnGb);

            // GB will fall back to US
            localeEnGb.fallbackLocale = localeEnUs;

            // US will fall back to English
            localeEnUs.fallbackLocale = sl.GetLocale(SystemLanguage.English);
        }

        [TearDown]
        public static void Teardown()
        {
            Object.DestroyImmediate(LocalizationSettings.instance);
        }

        [Test]
        public void DefaultLocaleIsSet()
        {
            CreateTestLocalizationSettings();
            var sl = LocalizationSettings.availableLocales;
            Assert.IsNotNull(sl.defaultLocale, "Expected a default locale to be set but it was not.");

            var expected = sl.GetLocale(SystemLanguage.English);
            Assert.AreEqual(expected, sl.defaultLocale, "Expected default locale to be set to English.");
        }

        [Test]
        public void SelectedLocaleUsesDefaultLocale_WhenStartupBehaviorIsDefaultLocale()
        {
            CreateTestLocalizationSettings();
            var sl = LocalizationSettings.availableLocales;

            Assert.IsNotNull(sl.defaultLocale, "Expected a default locale to be set but it was not.");

            var expected = LocalizationSettings.availableLocales.GetLocale(SystemLanguage.English);
            Assert.AreEqual(expected, LocalizationSettings.selectedLocale, "Expected selected locale to be set to English.");
        }

        [Test]
        public void CorrectFallbackReturnsWhenIncluded()
        {
            CreateTestLocalizationSettingsWithFallbacks();
            var sl = LocalizationSettings.availableLocales;

            var localeEn = sl.GetLocale("en");
            var localeEnUs = sl.GetLocale("en-US");
            var localeEnGb = sl.GetLocale("en-GB");

            Assert.IsNotNull(localeEn, "English(en) should be included in the localization settings.");
            Assert.IsNotNull(localeEnUs, "English(en-US) should be included in the localization settings.");
            Assert.IsNotNull(localeEnGb, "English(en-GB) should be included in the localization settings.");

            Assert.AreEqual(localeEnUs, localeEnGb.fallbackLocale, "English(en-GB) should fall back to English(en-US) but it did not.");
            Assert.AreEqual(localeEn, localeEnUs.fallbackLocale, "English(en-US) should fall back to English(en) but it did not.");
            Assert.IsNull(localeEn.fallbackLocale, "English(en) should have no fall back locale set.");
        }

        [Test]
        public void ChangingSelectedLocaleSendsLocaleChangedEvent()
        {
            CreateTestLocalizationSettings();
            LocalizationSettings.selectedLocaleChanged.AddListener(OnSelectedLocaleChanged);

            // Change the locale resulting in the event being sent.
            Assert.IsNull(m_OnSelectedLocaleChangedLocale);
            var japaneseLocale = LocalizationSettings.availableLocales.GetLocale(SystemLanguage.Japanese);
            Assert.IsNotNull(japaneseLocale);
            LocalizationSettings.selectedLocale = japaneseLocale;

            Assert.IsNotNull(m_OnSelectedLocaleChangedLocale, "Current language is null, the selectedLocaleChanged event was not sent.");
            Assert.AreEqual(LocalizationSettings.availableLocales.GetLocale(SystemLanguage.Japanese), m_OnSelectedLocaleChangedLocale, "Expected current language to be Japanese.");
        }

        Locale m_OnSelectedLocaleChangedLocale;
        void OnSelectedLocaleChanged(Locale locale)
        {
            Assert.IsNotNull(locale, "Locale should not be null, it should be Japanese.");
            m_OnSelectedLocaleChangedLocale = locale;
        }
    }
}
