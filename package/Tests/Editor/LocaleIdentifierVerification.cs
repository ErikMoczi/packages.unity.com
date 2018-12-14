using UnityEngine;
using NUnit.Framework;
using UnityEngine.Experimental.Localization;

namespace UnityEditor.Experimental.Localization.Tests
{
    [Category("Localization")]
    public class LocaleIdentifierVerification
    {
        [Test]
        public void SystemLanguageUnknownMapsToUndefined()
        {
            var localeId = new LocaleIdentifier(SystemLanguage.Unknown);
            Assert.AreEqual(LocaleIdentifier.undefined, localeId);
        }

        [Test]
        public void UndefinedCultureInfoIsNull()
        {
            Assert.IsNull(LocaleIdentifier.undefined.cultureInfo, "Expected undefined to have no CultureInfo.");
        }
    }
}
