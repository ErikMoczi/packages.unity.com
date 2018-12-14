using UnityEngine.Experimental.Localization;

namespace UnityEditor.Experimental.Localization
{
    public class LocalizationPlayerSettings
    {
        /// <summary>
        /// The LocalizationSettings used for this project.
        /// </summary>
        /// <remarks>
        /// The activeLocalizationSettings will be available in any player builds
        /// and the editor when playing.
        /// During a build or entering play mode, the asset will be added to the preloaded assets list.
        /// Note: This needs to be an asset.
        /// </remarks>
        public static LocalizationSettings activeLocalizationSettings
        {
            get
            {
                LocalizationSettings settings = null;
                EditorBuildSettings.TryGetConfigObject(LocalizationSettings.ConfigName, out settings);
                return settings;
            }
            set
            {
                if (value == null)
                {
                    EditorBuildSettings.RemoveConfigObject(LocalizationSettings.ConfigName);
                }
                else
                {
                    EditorBuildSettings.AddConfigObject(LocalizationSettings.ConfigName, value, true);
                }
            }
        }
    }
}
