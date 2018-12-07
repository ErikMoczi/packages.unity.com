using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;

using UnityObject = UnityEngine.Object;

namespace Unity.InteractiveTutorials
{
    class Tutorial : ScriptableObject
    {
        [Serializable]
        public class TutorialPageCollection : CollectionWrapper<TutorialPage>
        {
            public TutorialPageCollection() : base()
            {
            }

            public TutorialPageCollection(IList<TutorialPage> items) : base(items)
            {
            }
        }

        static void DirectoryCopy(string sourceDirectory, string destinationDirectory, HashSet<string> dirtyMetaFiles)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirectory);

            // Abort if source directory doesn't exist
            if (!dir.Exists)
                return;

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destinationDirectory, file.Name);
                if (dirtyMetaFiles != null && string.Equals(Path.GetExtension(tempPath), ".meta", StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(tempPath) || !File.ReadAllBytes(tempPath).SequenceEqual(File.ReadAllBytes(file.FullName)))
                        dirtyMetaFiles.Add(tempPath);
                }
                file.CopyTo(tempPath, true);
            }

            // copy sub directories and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destinationDirectory, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, dirtyMetaFiles);
            }
        }

        public static event Action<Tutorial> tutorialPagesChanged;

        public string tutorialTitle { get { return m_TutorialTitle; } }
        [Header("Content")]
        [SerializeField]
        string m_TutorialTitle = "";

        [SerializeField]
        string m_LessonId = "";

        public string version { get { return m_Version; } }
        [SerializeField]
        string m_Version = "0";

        [Header("Scene Data")]
        [SerializeField]
        UnityEditor.SceneAsset m_Scene;
        [SerializeField]
        SceneViewCameraSettings m_DefaultSceneCameraSettings;

        public TutorialWelcomePage welcomePage { get { return m_WelcomePage; } }
        [Header("Pages"), SerializeField]
        private TutorialWelcomePage m_WelcomePage;
        [SerializeField]
        public TutorialWelcomePage completedPage { get { return m_CompletedPage; } }
        [SerializeField]
        private TutorialWelcomePage m_CompletedPage;

        [SerializeField]
        internal bool IsWelcomingPageShowing = true;
        [SerializeField]
        internal bool IsCompletedPageShowing = false;

        AutoCompletion m_AutoCompletion;

        public event Action tutorialInitiated;
        public event Action<TutorialPage, int> pageInitiated;
        public event Action<TutorialPage> goingBack;
        public event Action tutorialCompleted;

        public Tutorial()
        {
            m_AutoCompletion = new AutoCompletion(this);
        }

        void OnEnable()
        {
            m_AutoCompletion.OnEnable();
        }

        void OnDisable()
        {
            m_AutoCompletion.OnDisable();
        }

        [SerializeField, FormerlySerializedAs("m_Steps")]
        internal TutorialPageCollection m_Pages = new TutorialPageCollection();

        public IEnumerable<TutorialPage> pages { get { return m_Pages; } }

        public int currentPageIndex { get { return m_CurrentPageIndex; } }
        [SerializeField, HideInInspector]
        int m_CurrentPageIndex = 0;

        public TutorialPage currentPage
        {
            get
            {
                return m_Pages.count == 0 ?
                    null : m_Pages[m_CurrentPageIndex = Mathf.Min(m_CurrentPageIndex, m_Pages.count - 1)];
            }
        }

        public int pageCount { get { return m_Pages.count; } }

        public bool completed { get { return pageCount == 0 || (currentPageIndex == pageCount - 1 && currentPage != null && currentPage.allCriteriaAreSatisfied); } }

        [SerializeField, Tooltip("Saved layouts can be found in the following directories:\n" +
             "Windows: %APPDATA%/Unity/<version>/Preferences/Layouts\n" +
             "macOS: ~/Library/Preferences/Unity/<version>/Layouts\n" +
             "Linux: ~/.config/Preferences/Unity/<version>/Layouts")]
        UnityObject m_WindowLayout;

        public bool isAutoCompleting { get { return m_AutoCompletion.running; } }
        public string LessonId { get { return m_LessonId; } }

        public void StartAutoCompletion()
        {
            m_AutoCompletion.Start();
        }

        public void StopAutoCompletion()
        {
            m_AutoCompletion.Stop();
        }

        public void StopTutorial()
        {
            if (currentPage != null)
                currentPage.RemoveCompletionRequirements();
        }

        public void GoToPreviousPage()
        {
            if (m_CurrentPageIndex == 0 && !IsWelcomingPageShowing)
            {
                IsWelcomingPageShowing = true;
                return;
            }
            OnGoingBack(currentPage);
            m_CurrentPageIndex = Mathf.Max(0, m_CurrentPageIndex - 1);
            OnPageInitiated(currentPage, m_CurrentPageIndex);
        }

        public bool TryGoToNextPage()
        {
            if (!currentPage.allCriteriaAreSatisfied && !currentPage.hasMovedToNextPage)
                return false;
            if (m_Pages.count == m_CurrentPageIndex + 1)
            {
                OnTutorialCompleted();
                IsCompletedPageShowing = true;
                return false;
            }
            int newIndex = Mathf.Min(m_Pages.count - 1, m_CurrentPageIndex + 1);
            if (newIndex != m_CurrentPageIndex)
            {
                if (currentPage != null)
                    currentPage.OnPageCompleted();
                m_CurrentPageIndex = newIndex;
                OnPageInitiated(currentPage, m_CurrentPageIndex);
                return true;
            }
            return false;
        }

        public void RaiseTutorialPagesChangedEvent()
        {
            if (tutorialPagesChanged != null)
                tutorialPagesChanged(this);
        }

        private void LoadScene()
        {
            // load scene
            if (m_Scene != null)
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_Scene));
            else
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

            // move scene view camera into place
            if (m_DefaultSceneCameraSettings != null && m_DefaultSceneCameraSettings.enabled)
                m_DefaultSceneCameraSettings.Apply();
            OnTutorialInitiated();
            if (pageCount > 0)
                OnPageInitiated(currentPage, m_CurrentPageIndex);
        }

        const string k_DefaultsFolder = "Tutorial Defaults";

        void LoadTutorialDefaultsIntoAssetsFolder()
        {
            AssetDatabase.SaveAssets();
            string defaultsPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, k_DefaultsFolder);
            var dirtyMetaFiles = new HashSet<string>();
            DirectoryCopy(defaultsPath, Application.dataPath, dirtyMetaFiles);
            AssetDatabase.Refresh();
            int startIndex = Application.dataPath.Length - "Assets".Length;
            foreach (var dirtyMetaFile in dirtyMetaFiles)
                AssetDatabase.ImportAsset(Path.ChangeExtension(dirtyMetaFile.Substring(startIndex), null));
        }

        public void WriteAssetsToTutorialDefaultsFolder()
        {
            string defaultsPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, k_DefaultsFolder);
            DirectoryInfo defaultsDirectory = new DirectoryInfo(defaultsPath);
            if (defaultsDirectory.Exists)
            {
                foreach (var file in defaultsDirectory.GetFiles())
                    file.Delete();
                foreach (var directory in defaultsDirectory.GetDirectories())
                    directory.Delete(true);
            }
            DirectoryCopy(Application.dataPath, defaultsPath, null);
        }

        void LoadWindowLayout()
        {
            if (m_WindowLayout == null)
                return;

            var layoutPath = AssetDatabase.GetAssetPath(m_WindowLayout);
            TutorialWindow.LoadWindowLayoutAndSetUpTutorial(layoutPath, this);
        }

        public void ReloadTutorial()
        {
            LoadWindowLayout();
            ResetProgress();

            // Do not overwrite workspace in authoring mode, use version control instead.
            if (!ProjectMode.IsAuthoringMode())
                LoadTutorialDefaultsIntoAssetsFolder();
        }

        public void ResetProgress()
        {
            foreach (var page in m_Pages)
            {
                if (page != null)
                    page.ResetUserProgress();
            }
            m_CurrentPageIndex = 0;
            IsWelcomingPageShowing = true;
            IsCompletedPageShowing = false;
            LoadScene();
        }

        protected virtual void OnTutorialInitiated()
        {
            if (tutorialInitiated != null)
                tutorialInitiated();
        }

        protected virtual void OnTutorialCompleted()
        {
            if (tutorialCompleted != null)
                tutorialCompleted();
        }

        protected virtual void OnPageInitiated(TutorialPage page, int index)
        {
            if (page != null)
                page.Initiate();

            if (pageInitiated != null)
                pageInitiated(page, index);
        }

        protected virtual void OnGoingBack(TutorialPage page)
        {
            if (page != null)
                page.RemoveCompletionRequirements();

            if (goingBack != null)
                goingBack(page);
        }
    }
}
