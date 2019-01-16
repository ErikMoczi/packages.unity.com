using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    class TutorialProjectSettings : ScriptableObject
    {
        static TutorialProjectSettings s_Instance;
        public static TutorialProjectSettings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var assetGUIDs = AssetDatabase.FindAssets("t:Unity.InteractiveTutorials.TutorialProjectSettings");
                    if (assetGUIDs.Length == 0)
                        s_Instance = CreateInstance<TutorialProjectSettings>();
                    else
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[0]);

                        if (assetGUIDs.Length > 1)
                            Debug.LogWarningFormat("There is more than one TutorialProjectSetting asset in project.\n" +
                                "Using asset at path: {0}", assetPath);

                        s_Instance = AssetDatabase.LoadAssetAtPath<TutorialProjectSettings>(assetPath);
                    }
                }

                return s_Instance;
            }
        }

        public static void ReloadInstance()
        {
            s_Instance = null;
        }

        [SerializeField]
        bool m_RestoreDefaultAssetsOnTutorialReload = true;
        public bool restoreDefaultAssetsOnTutorialReload { get { return m_RestoreDefaultAssetsOnTutorialReload; } }

        [SerializeField]
        [Tooltip("If enabled, disregard startup tutorial and start the first tutorial found in the project")]
        bool m_UseLegacyStartupBehavior = true;

        [SerializeField]
        Tutorial m_StartupTutorial = null;

        public Tutorial startupTutorial
        {
            get
            {
                if (m_UseLegacyStartupBehavior)
                {
                    var guids = AssetDatabase.FindAssets("t:Tutorial");
                    if (guids.Length > 0)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        return AssetDatabase.LoadAssetAtPath<Tutorial>(assetPath);
                    }

                    return null;
                }

                return m_StartupTutorial;
            }
        }

        [SerializeField]
        TutorialStyles m_TutorialStyle;
        public TutorialStyles TutorialStyle
        {
            get
            {
                if(!m_TutorialStyle)
                {
                    m_TutorialStyle = AssetDatabase.LoadAssetAtPath<TutorialStyles>("Packages/com.unity.learn.iet-framework/Framework/Interactive Tutorials/GUI/Tutorial Styles.asset");
                }
                return m_TutorialStyle;
            }
        }
    }
}
