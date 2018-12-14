using System;
using UnityEditor;
using UnityEngine;

namespace UnityEngine.Experimental.Localization
{
    [CreateAssetMenu(menuName = "Localization/Startup Locale Selectors/Command Line")]
    public class CommandLineLocaleSelector : StartupLocaleSelector
    {
        [SerializeField]
        string m_commandLineArgument = "-language =";

        /// <summary>
        /// The command line argument used to assign the locale.
        /// </summary>
        public string commandLineArgument
        {
            get { return m_commandLineArgument; }
            set { m_commandLineArgument = value; }
        }

        public override Locale GetStartupLocale(AvailableLocales availableLocales)
        {
            if (string.IsNullOrEmpty(m_commandLineArgument))
                return null;

            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith(m_commandLineArgument, StringComparison.OrdinalIgnoreCase))
                {
                    var argValue = arg.Substring(m_commandLineArgument.Length);
                    int id;
                    return int.TryParse(argValue, out id) ? availableLocales.GetLocale(id) : availableLocales.GetLocale(argValue);
                }
            }
            return null;
        }
    }
}