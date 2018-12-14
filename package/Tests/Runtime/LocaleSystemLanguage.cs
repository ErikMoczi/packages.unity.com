using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEngine.Experimental.Localization.Tests
{
    [Category("Localization")]
    public class LocaleSystemLanguage
    {
        static List<SystemLanguage> GetSystemLanguageWithoutUnknown()
        {
            var values = Enum.GetValues(typeof(SystemLanguage));
            List<SystemLanguage> langList = new List<SystemLanguage>();
            foreach (SystemLanguage systemLanguage in values)
            {
                if (systemLanguage != SystemLanguage.Unknown)
                    langList.Add(systemLanguage);
            }
            return langList;
        }

        [Test]
        [TestCaseSource("GetSystemLanguageWithoutUnknown")]
        public void SystemLanguageMapsToLocaleIdentifier(SystemLanguage lang)
        {
            var localeId = new LocaleIdentifier(lang);
            Assert.IsNotNull(localeId.cultureInfo, "Expected the SystemLanguage to be mapped to a cultureInfo but it was not." + localeId);
            Assert.Greater(localeId.id, 0, "Expected the locale to extract a valid id from the SystemLanguage but it did not." + localeId);
            Assert.IsNotEmpty(localeId.code, "Expected the locale to extract a valid code from the SystemLanguage but it did not." + localeId);
        }
    }
}
