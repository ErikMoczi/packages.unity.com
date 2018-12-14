using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.Localization
{
    [CreateAssetMenu(menuName = "Localization//Locale/Locales Collections")]
    public class LocalesCollection : AvailableLocales
    {
        [SerializeField]
        List<Locale> m_Locales = new List<Locale>();

        [SerializeField]
        Locale m_DefaultLocale;

        public override List<Locale> locales
        {
            get { return m_Locales; }
            set { m_Locales = value; }
        }

        public override Locale defaultLocale
        {
            get { return m_DefaultLocale; }
            set { m_DefaultLocale = value; }
        }

        public override Locale GetLocale(LocaleIdentifier id)
        {
            return m_Locales.FirstOrDefault(o => o.identifier.Equals(id));
        }

        public override Locale GetLocale(string code)
        {
            return m_Locales.FirstOrDefault(o => o.identifier.code == code);
        }

        public override Locale GetLocale(int id)
        {
            return m_Locales.FirstOrDefault(o => o.identifier.id == id);
        }

        public override void AddLocale(Locale locale)
        {
            if (locale == null)
                throw new ArgumentNullException("locale");

            if (GetLocale(locale.identifier) != null)
            {
                Debug.LogWarning("Ignoring locale. A locale with the same Id has already been added: " + locale.identifier);
                return;
            }

            m_Locales.Add(locale);
        }

        public override bool RemoveLocale(Locale locale)
        {
            var ret = m_Locales.Remove(locale);
            base.RemoveLocale(locale);
            return ret;
        }
    }
}
