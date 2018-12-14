using System;
using System.Globalization;
using UnityEngine.Events;
using UnityEngine;

namespace UnityEngine.Experimental.Localization
{
    [Serializable]
    public class LocaleEvent : UnityEvent<Locale> {};

    /// <summary>
    /// The identifier containing the identification information for a language or regional variant.
    /// </summary>
    [Serializable]
    public struct LocaleIdentifier
    {
        [SerializeField] int m_Id;
        [SerializeField] string m_Code;
        CultureInfo m_CultureInfo;

        /// <summary>
        /// Represents an undefined Local Identifier. One that does not define any language or region.
        /// </summary>
        public static LocaleIdentifier undefined { get { return new LocaleIdentifier(-1, "undefined"); } }

        /// <summary>
        /// A unique number representing the Locale. When possible, this value is taken from the <see cref="cultureInfo"/> LCID property.
        /// </summary>
        public int id { get { return m_Id; } }

        /// <summary>
        /// The culture name in the format [language]-[region].
        /// </summary>
        /// <remarks>
        /// For example, Language English would be 'en', Regional English(UK) would be 'en-GB' and Regional English(US) would be 'en-US'.
        /// </remarks>
        public string code { get { return m_Code; } }

        /// <summary>
        /// A <see cref="System.Globalization.CultureInfo"/> representation of the Locale.
        /// </summary>
        /// <remarks>
        /// The id is used to query for a <see cref="System.Globalization.CultureInfo"/> unless its value is 0, in which case the <see cref="code"/> will be used.
        /// </remarks>
        public CultureInfo cultureInfo
        {
            get
            {
                if (m_CultureInfo == null)
                {
                    try
                    {
                        m_CultureInfo = m_Id != 0 ? CultureInfo.GetCultureInfo(m_Id) : CultureInfo.GetCultureInfo(m_Code);
                    }
                    catch (Exception)
                    {
                        // If a culture info can not be found then we do not consider this an error. It could be a custom locale.
                    }
                }
                return m_CultureInfo;
            }
        }

        public LocaleIdentifier(int id, string code)
        {
            m_Id = id;
            m_Code = code;
            m_CultureInfo = null;
        }

        public LocaleIdentifier(int id)
        {
            m_Id = id;
            m_Code = string.Empty;
            m_CultureInfo = null;
            if (cultureInfo != null)
                m_Code = cultureInfo.Name;
        }

        public LocaleIdentifier(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                this = undefined;
                return;
            }

            m_Id = 0;
            m_Code = code;
            m_CultureInfo = null;
            if (cultureInfo != null)
                m_Id = cultureInfo.LCID;
        }

        public LocaleIdentifier(CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException("culture");

            m_Id = culture.LCID;
            m_Code = culture.Name;
            m_CultureInfo = culture;
        }

        public LocaleIdentifier(SystemLanguage systemLanguage)
            : this(SystemLanguageConverter.GetSystemLanguageCultureCode(systemLanguage))
        {
        }

        public static implicit operator LocaleIdentifier(int id)
        {
            return new LocaleIdentifier(id);
        }

        public static implicit operator LocaleIdentifier(string code)
        {
            return new LocaleIdentifier(code);
        }

        public static implicit operator LocaleIdentifier(CultureInfo culture)
        {
            return new LocaleIdentifier(culture);
        }

        public static implicit operator LocaleIdentifier(SystemLanguage systemLanguage)
        {
            return new LocaleIdentifier(systemLanguage);
        }

        public override string ToString()
        {
            return string.Format("[{0}:{1}]", id, code);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LocaleIdentifier && Equals((LocaleIdentifier)obj);
        }

        public bool Equals(LocaleIdentifier other)
        {
            return id == other.id && code == other.code;
        }

        public override int GetHashCode()
        {
            if (cultureInfo != null)
                return cultureInfo.GetHashCode();

            if (string.IsNullOrEmpty(code))
                return code.GetHashCode();

            return id;
        }

        public static bool operator==(LocaleIdentifier l1, LocaleIdentifier l2)
        {
            return l1.Equals(l2);
        }

        public static bool operator!=(LocaleIdentifier l1, LocaleIdentifier l2)
        {
            return !l1.Equals(l2);
        }
    }

    /// <summary>
    /// A Locale represents a language. It supports regional variations and can be configured with an optional fallback locale.
    /// </summary>
    [CreateAssetMenu(menuName = "Localization/Empty Locale")]
    public class Locale : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] LocaleIdentifier m_Identifier;
        [SerializeField] Locale m_Fallback;

        /// <summary>
        /// The identifier contains the identifying information such as the id and culture code for this Locale.
        /// </summary>
        public LocaleIdentifier identifier
        {
            get { return m_Identifier; }
            set { m_Identifier = value; }
        }

        /// <summary>
        /// An optional fallback locale can be configured to link Locales that may serve as good alternatives when no localization
        /// data is available for the current locale.
        /// </summary>
        /// <remarks>
        /// For example, regional variations could be configured for some specific words such as when the selected Locale is English(UK),
        /// then the word Color could be localized to ‘Colour’ however other words would fall back from English(UK) to English(US).
        /// Note: The fallback locale must be added to the active LocalizationSettings or it will not be possible to retrieve it.
        /// </remarks>
        public Locale fallbackLocale
        {
            get
            {
                return m_Fallback;
            }
            set
            {
                m_Fallback = value;
                ValidateFallback();
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            ValidateFallback();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            ValidateFallback();
        }
 
        void ValidateFallback()
        {
            Locale parent = m_Fallback;
            while (parent != null)
            {
                if (parent == this)
                {
                    Debug.LogWarning("Cyclic fallback linking detected. Can not set fallback locale as it would create an infinite loop.", this);
                    m_Fallback = null;
                }
                parent = parent.fallbackLocale;
            }
        }

        public static Locale CreateLocale(string code)
        {
            var locale = CreateInstance<Locale>();
            locale.m_Identifier = new LocaleIdentifier(code);
            if (locale.m_Identifier.cultureInfo != null)
            {
                locale.name = locale.m_Identifier.cultureInfo.EnglishName;
            }
            return locale;
        }

        public static Locale CreateLocale(LocaleIdentifier identifier)
        {
            var locale = CreateInstance<Locale>();
            locale.m_Identifier = identifier;
            if (locale.m_Identifier.cultureInfo != null)
            {
                locale.name = locale.m_Identifier.cultureInfo.EnglishName;
            }
            return locale;
        }

        public static Locale CreateLocale(SystemLanguage language)
        {
            return CreateLocale(new LocaleIdentifier(SystemLanguageConverter.GetSystemLanguageCultureCode(language)));
        }

        public static Locale CreateLocale(CultureInfo cultureInfo)
        {
            return CreateLocale(new LocaleIdentifier(cultureInfo));
        }

        public override string ToString()
        {
            return m_Identifier + " " + name;
        }
    }
}
