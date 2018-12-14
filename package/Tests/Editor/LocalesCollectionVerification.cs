using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEngine.Experimental.Localization;

namespace UnityEditor.Experimental.Localization.Tests
{
    [Category("Localization")]
    public class LocalesCollectionVerification
    {
        AvailableLocales m_Locales;

        [SetUp]
        public void Setup()
        {
            m_Locales = ScriptableObject.CreateInstance<LocalesCollection>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(m_Locales);
        }

        [Test]
        public void WarningWhenAddingDuplicateLocale()
        {
            var locale = Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.Danish));
            m_Locales.AddLocale(locale);
            LogAssert.Expect(LogType.Warning, new Regex("Ignoring locale*"));
            m_Locales.AddLocale(locale);
            Object.DestroyImmediate(m_Locales);
        }

        [Test]
        public void NullReturnedWhenLocaleIsNotFound()
        {
            Assert.IsNull(m_Locales.GetLocale("en"), "Expected no locale to be found.");
        }
    }
}
