using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Experimental.Localization;

namespace UnityEditor.Experimental.Localization
{
    class LocalizationBuildPlayer : IPreprocessBuild, IPostprocessBuild
    {
        LocalizationSettings m_Settings;

        Object[] m_OriginalPreloadedAssets;

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            m_Settings = LocalizationPlayerSettings.activeLocalizationSettings;
            if (m_Settings == null)
                return;

            AddToPreloadedAssets(m_Settings);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (m_Settings == null)
                return;

            RemoveFromPreloadedAssets(m_Settings);
        }

        void AddToPreloadedAssets(LocalizationSettings ls)
        {
            m_OriginalPreloadedAssets = PlayerSettings.GetPreloadedAssets();

            if (!m_OriginalPreloadedAssets.Contains(ls))
            {
                var preloadedAssets = m_OriginalPreloadedAssets.ToList();
                preloadedAssets.Add(ls);
                PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
            }
        }

        void RemoveFromPreloadedAssets(LocalizationSettings ls)
        {
            // Revert back to original state
            PlayerSettings.SetPreloadedAssets(m_OriginalPreloadedAssets);
        }
    }
}
