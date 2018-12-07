using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.InteractiveTutorials
{
    class TutorialWindow : EditorWindowProxy
    {
        private static TutorialWindow window;

        [MenuItem("Window/Tutorials")]
        public static TutorialWindow CreateWindow()
        {
            window = GetWindow<TutorialWindow>();
            window.minSize = new Vector2(300f, 380f);
            return window;
        }

        public static TutorialWindow GetWindow()
        {
            if (window == null)
                window = GetWindow<TutorialWindow>();
            return window;
        }

        private List<TutorialParagraphView> m_Paragraphs = new List<TutorialParagraphView>();
        [SerializeField]
        private int[] m_Indexes;
        [SerializeField]
        private List<TutorialParagraphView> m_AllParagraphs = new List<TutorialParagraphView>();

        private const int k_MaxTitleLength = 26;

        private const int k_NumberOfPixelsThatTriggerLongerTitle = 8;

        static readonly float s_AuthoringModeToolbarButtonWidth = 100f;

        private bool canMoveToNextPage { get { return m_CurrentTutorial.currentPage.allCriteriaAreSatisfied || m_CurrentTutorial.currentPage.hasMovedToNextPage; } }

        private GUIStyle m_BackButton;

        private string m_Title = "";
        private string m_NextButtonText = "";

        private bool m_AuthoringMode;

        private static readonly GUIContent s_HomePromptTitle = new GUIContent("Return to Tutorials?");
        private static readonly GUIContent s_HomePromptText = new GUIContent("Returning to the Tutorial Selection means exiting the tutorial and losing all of your progress\nDo you wish to continue?");
        private static readonly GUIContent s_HomePromptYes = new GUIContent("Yes");
        private static readonly GUIContent s_HomePromptNo = new GUIContent("No");

        private static readonly GUIContent s_RestartPromptTitle = new GUIContent("Restart Tutorial?");
        private static readonly GUIContent s_RestartPromptText = new GUIContent("Returning to the first step will restart the tutorial and you will lose all of your progress. Do you wish to restart?");
        private static readonly GUIContent s_RestartPromptYes = new GUIContent("Yes");
        private static readonly GUIContent s_RestartPromptNo = new GUIContent("No");

        public Tutorial currentTutorial { get { return m_CurrentTutorial; } }
        private Tutorial m_CurrentTutorial;

        private bool maskingEnabled
        {
            get
            {
                var forceDisableMask = EditorPrefs.GetBool("Unity.InteractiveTutorials.forceDisableMask", false);
                return !forceDisableMask && (m_MaskingEnabled || !ProjectMode.IsAuthoringMode());
            }
            set { m_MaskingEnabled = value; }
        }
        [SerializeField]
        private bool m_MaskingEnabled = true;

        [SerializeField]
        private TutorialStyles m_Styles;
        [SerializeField]
        private Vector2 m_ScrollPosition;

        [SerializeField]
        private int m_FarthestPageCompleted = -1;

        [SerializeField]
        private bool m_PlayModeChanging;

        void TrackPlayModeChanging(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    m_PlayModeChanging = true;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    m_PlayModeChanging = false;
                    break;
            }
        }

        void OnEnable()
        {
            window = this;
            window.minSize = new Vector2(300f, 380f);
            if (m_CurrentTutorial == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:Tutorial");
                if (guids.Length > 0)
                {
                    var tutorial = AssetDatabase.LoadAssetAtPath<Tutorial>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    m_CurrentTutorial = tutorial;
                }
            }
            this.titleContent.text = "Tutorials";

            m_AuthoringMode = ProjectMode.IsAuthoringMode();

            GUIViewProxy.positionChanged += OnGUIViewPositionChanged;
            HostViewProxy.actualViewChanged += OnHostViewActualViewChanged;
            Tutorial.tutorialPagesChanged += OnTutorialPagesChanged;
            // test for page completion state changes (rather than criteria completion/invalidation directly) so that page completion state will be up-to-date
            TutorialPage.criteriaCompletionStateTested += OnTutorialPageCriteriaCompletionStateTested;
            TutorialPage.tutorialPageMaskingSettingsChanged += OnTutorialPageMaskingSettingsChanged;
            TutorialPage.tutorialPageNonMaskingSettingsChanged += OnTutorialPageNonMaskingSettingsChanged;
            EditorApplication.playModeStateChanged -= TrackPlayModeChanging;
            EditorApplication.playModeStateChanged += TrackPlayModeChanging;

            SetUpTutorial();

            ApplyMaskingSettings(true);
        }

        void WindowForParagraph()
        {
            foreach (var p in m_Paragraphs)
            {
                p.SetWindow(window);
            }
        }

        void OnHostViewActualViewChanged()
        {
            // do not mask immediately in case unmasked GUIView doesn't exist yet
            EditorApplication.update += ApplyQueuedMask;
        }

        void OnTutorialPageCriteriaCompletionStateTested(TutorialPage sender)
        {
            if (m_CurrentTutorial == null || m_CurrentTutorial.currentPage != sender)
                return;

            foreach (var paragraph in m_Paragraphs)
                paragraph.ResetState();

            if (sender.allCriteriaAreSatisfied && sender.autoAdvanceOnComplete && !sender.hasMovedToNextPage)
            {
                if (m_CurrentTutorial.TryGoToNextPage())
                    return;
            }

            ApplyMaskingSettings(true);
        }

        void OnDisable()
        {
            if (!m_PlayModeChanging)
                AnalyticsHelper.TutorialEnded(TutorialConclusion.Quit);

            ClearTutorialListener();

            Tutorial.tutorialPagesChanged -= OnTutorialPagesChanged;
            TutorialPage.criteriaCompletionStateTested -= OnTutorialPageCriteriaCompletionStateTested;
            TutorialPage.tutorialPageMaskingSettingsChanged -= OnTutorialPageMaskingSettingsChanged;
            TutorialPage.tutorialPageNonMaskingSettingsChanged -= OnTutorialPageNonMaskingSettingsChanged;
            GUIViewProxy.positionChanged -= OnGUIViewPositionChanged;
            HostViewProxy.actualViewChanged -= OnHostViewActualViewChanged;

            ApplyMaskingSettings(false);
        }

        private void OnTutorialInitiated()
        {
            AnalyticsHelper.TutorialStarted(m_CurrentTutorial);
            GenesisHelper.LogTutorialStarted(m_CurrentTutorial.LessonId);
            CreateTutorialViews();
        }

        private void OnTutorialCompleted()
        {
            AnalyticsHelper.TutorialEnded(TutorialConclusion.Completed);
            GenesisHelper.LogTutorialEnded(m_CurrentTutorial.LessonId);

            // Open Hub and show tutorials
            HomeWindowProxy.ShowTutorials();
        }

        public void CreateTutorialViews()
        {
            m_AllParagraphs.Clear();
            var ite = 0;
            m_Indexes = new int[m_CurrentTutorial.pageCount];
            foreach (var page in m_CurrentTutorial.pages)
            {
                if (page == null)
                    continue;

                var instructionIndex = 0;
                m_Indexes[ite] = m_AllParagraphs.Count;
                ite++;
                foreach (var paragraph in page.paragraphs)
                {
                    if (paragraph.type == ParagraphType.Instruction)
                        ++instructionIndex;
                    m_AllParagraphs.Add(new TutorialParagraphView(paragraph, window, m_Styles.orderedListDelimiter, m_Styles.unorderedListBullet, instructionIndex));
                }
            }
        }

        private List<TutorialParagraphView> GetCurrentParagraph()
        {
            List<TutorialParagraphView> tmp = new List<TutorialParagraphView>();
            if (m_Indexes.Length > 0)
            {
                var endIndex = m_CurrentTutorial.currentPageIndex + 1 > m_CurrentTutorial.pageCount - 1 ? m_AllParagraphs.Count : m_Indexes[m_CurrentTutorial.currentPageIndex + 1];
                for (int i = m_Indexes[m_CurrentTutorial.currentPageIndex]; i < endIndex; i++)
                {
                    tmp.Add(m_AllParagraphs[i]);
                }
            }
            return tmp;
        }

        public void PrepareNewPage(TutorialPage page = null, int index = 0)
        {
            if (!m_AllParagraphs.Any())
                CreateTutorialViews();
            m_Paragraphs.Clear();
            if (m_CurrentTutorial.currentPage == null)
                m_NextButtonText = string.Empty;
            else
                m_NextButtonText = IsLastPage() ? m_CurrentTutorial.currentPage.doneButton : m_CurrentTutorial.currentPage.nextButton;
            FormatTitle();

            m_Paragraphs = GetCurrentParagraph();

            m_Paragraphs.TrimExcess();

            WindowForParagraph();
        }

        public void ForceInititalizeTutorialAndPage()
        {
            m_FarthestPageCompleted = -1;

            CreateTutorialViews();
            PrepareNewPage();
        }

        private void OpenLoadTutorialDialog()
        {
            string assetPath = EditorUtility.OpenFilePanel("Load a Tutorial", "Assets", "asset");
            if (!string.IsNullOrEmpty(assetPath))
            {
                assetPath = string.Format("Assets{0}", assetPath.Substring(Application.dataPath.Length));
                SetTutorial(AssetDatabase.LoadAssetAtPath<Tutorial>(assetPath));
                GUIUtility.ExitGUI();
            }
        }

        private bool IsLastPage()
        {
            return m_CurrentTutorial.pageCount - 1 <= m_CurrentTutorial.currentPageIndex;
        }

        protected override void OnResized_Internal()
        {
            FormatTitle();
        }

        private void FormatTitle()
        {
            if (m_CurrentTutorial == null)
                return;

            var index = k_MaxTitleLength;
            var title = string.Empty;
            if (m_CurrentTutorial != null)
                title = string.IsNullOrEmpty(m_CurrentTutorial.tutorialTitle) ? m_CurrentTutorial.name : m_CurrentTutorial.tutorialTitle;

            if (window != null)
            {
                var extraCharactersForTitle = Mathf.RoundToInt((window.position.width - window.minSize.x) / k_NumberOfPixelsThatTriggerLongerTitle);
                index += extraCharactersForTitle;
            }
            index = index < title.Length ? index : title.Length - 1;

            m_Title = index == title.Length - 1 ? title : string.Format("{0}{1}", title.Substring(0, index - 1).TrimEnd(), "...");
        }

        private void ClearTutorialListener()
        {
            if (m_CurrentTutorial != null)
            {
                m_CurrentTutorial.tutorialInitiated -= OnTutorialInitiated;
                m_CurrentTutorial.tutorialCompleted -= OnTutorialCompleted;
                m_CurrentTutorial.pageInitiated -= OnShowPage;
                m_CurrentTutorial.StopTutorial();
            }
        }

        internal void SetTutorial(Tutorial tutorial, bool reload = true)
        {
            ClearTutorialListener();

            m_CurrentTutorial = tutorial;
            if (m_CurrentTutorial != null)
            {
                if (reload)
                    m_CurrentTutorial.ReloadTutorial();
                m_AllParagraphs.Clear();
                m_Paragraphs.Clear();
            }
            SetUpTutorial();
        }

        private void SetUpTutorial()
        {
            // bail out if this instance no longer exists such as when e.g., loading a new window layout
            if (this == null || m_CurrentTutorial == null || m_CurrentTutorial.currentPage == null)
                return;

            if (m_CurrentTutorial.currentPage != null)
                m_CurrentTutorial.currentPage.Initiate();

            m_CurrentTutorial.tutorialInitiated += OnTutorialInitiated;
            m_CurrentTutorial.tutorialCompleted += OnTutorialCompleted;
            m_CurrentTutorial.pageInitiated += OnShowPage;

            if (!m_AllParagraphs.Any())
                ForceInititalizeTutorialAndPage();
            else
                PrepareNewPage();
        }

        void ApplyQueuedMask()
        {
            if (!IsParentNull())
            {
                ApplyMaskingSettings(true);

                EditorApplication.update -= ApplyQueuedMask;
            }
        }

        void OnGUI()
        {
            //Force the GUI color to always be white, so the tutorial window
            //will not be darkened  while in playmode
            GUI.color = Color.white;

            GUISkin oldSkin = GUI.skin;
            GUI.skin = m_Styles.skin;

            if (m_AuthoringMode)
                ToolbarGUI();

            if (m_Styles == null)
            {
                TutorialStyles.DisplayErrorMessage("TutorialWindow.cs");
                return;
            }

            if (m_CurrentTutorial == null)
            {
                EditorGUILayout.HelpBox("No tutorial currently loaded. Please load one to begin.", MessageType.Info);
                return;
            }

            //Might be used later if a completed page is desired
            /*if (m_CurrentTutorial.IsCompletedPageShowing)
            {
                DrawCompletedPage();
            }*/

            var useGrayBackground =
                m_CurrentTutorial.currentPage != null &&
                m_CurrentTutorial.currentPage.paragraphs.All(element => element.type == ParagraphType.Narrative);

            using (var background = new EditorGUILayout.VerticalScope(useGrayBackground ? AllTutorialStyles.fullGreyBackground : AllTutorialStyles.background ?? GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
            {
                TopBar();

                if (m_CurrentTutorial.currentPage == null)
                {
                    GUILayout.Label(string.Format("No step {0} assigned for {1}.", m_CurrentTutorial.currentPageIndex, m_CurrentTutorial));
                }
                else
                {
                    // disable GUI except scrollbar/toolbar/gutter when revisiting a previously completed page to clearly indicate its tasks are no longer active
                    var pageCompleted = m_CurrentTutorial.currentPageIndex <= m_FarthestPageCompleted;
                    if (!string.IsNullOrEmpty(m_CurrentTutorial.currentPage.sectionTitle))
                    {
                        using (var bg = new EditorGUILayout.HorizontalScope(AllTutorialStyles.sectionTitleBackground, GUILayout.ExpandWidth(true)))
                        {
                            using (new EditorGUI.DisabledScope(pageCompleted))
                                GUILayout.Label(m_CurrentTutorial.currentPage.sectionTitle, AllTutorialStyles.sectionTitleLabel);
                        }
                    }

                    var previousTaskState = true;
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPosition, GUI.skin.verticalScrollbar, GUIStyle.none))
                    {
                        using (new EditorGUI.DisabledScope(pageCompleted))
                        {
                            foreach (var paragraph in m_Paragraphs)
                            {
                                if (paragraph.paragraph.type == ParagraphType.Instruction)
                                    GUILayout.Space(2f);

                                paragraph.Draw(ref previousTaskState, pageCompleted);
                            }
                        }
                        m_ScrollPosition = scrollView.scrollPosition;
                        GUILayout.FlexibleSpace();
                    }
                    GutterGUI(m_CurrentTutorial.currentPageIndex, m_CurrentTutorial.pageCount, background.rect);
                }
            }

            GUI.skin = oldSkin;
        }

        //Might be desirable if a completed page is something we want
        /*private void DrawCompletedPage()
        {
            if (m_CurrentTutorial.completedPage != null)
            {
                TutorialModalWindow.TryToShow(m_CurrentTutorial.completedPage, true, () =>
                    {
                        m_CurrentTutorial.IsCompletedPageShowing = false;
                        if (Event.current.shift)
                        {
                            OpenLoadTutorialDialog();
                        }
                        else
                        {
                            HomeWindow.Show(HomeWindow.HomeMode.Tutorial);
                        }
                    }
                    );
            }
            else if (m_CurrentTutorial.welcomePage != null)
            {
                TutorialModalWindow.TryToShow(m_CurrentTutorial.welcomePage, true, () =>
                    {
                        Debug.Log("Open next tutorial");
                        m_CurrentTutorial.IsCompletedPageShowing = false;
                    }
                    );
            }
            else
            {
                m_CurrentTutorial.IsCompletedPageShowing = false;
            }
        }*/

        private void TopBar()
        {
            using (var backgroundRect = new EditorGUILayout.HorizontalScope(AllTutorialStyles.topBarBackground ?? GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
            {
                if (m_CurrentTutorial.currentPageIndex > 0)
                {
                    if (GUILayout.Button(GUIContent.none, AllTutorialStyles.iconButtonBack))
                    {
                        m_CurrentTutorial.GoToPreviousPage();

                        // Masking could potentially change when pressing this button which causes an immediate repaint
                        // Exit GUI here to avoid re-entrant GUI errors
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    if (m_BackButton == null)
                        m_BackButton = AllTutorialStyles.iconButtonBack;
                    GUILayout.Space(m_BackButton.fixedWidth + m_BackButton.margin.right + m_BackButton.margin.left);
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(m_Title, AllTutorialStyles.headerLabel ?? GUIStyle.none);
                GUILayout.FlexibleSpace();

                //Restart
                if (GUILayout.Button(GUIContent.none, AllTutorialStyles.iconButtonReset))
                {
                    if (EditorUtility.DisplayDialog(s_RestartPromptTitle.text, s_RestartPromptText.text, s_RestartPromptYes.text, s_RestartPromptNo.text))
                    {
                        ResetTutorial();
                        GUIUtility.ExitGUI();
                    }
                }
                //Exit tutorial
                if (GUILayout.Button(GUIContent.none, AllTutorialStyles.iconButtonHome))
                {
                    if (EditorUtility.DisplayDialog(s_HomePromptTitle.text, s_HomePromptText.text, s_HomePromptYes.text, s_HomePromptNo.text))
                    {
                        HomeWindowProxy.ShowTutorials();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private void ToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            // scenes cannot be loaded while in play mode
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Load Tutorial", EditorStyles.toolbarButton, GUILayout.MaxWidth(s_AuthoringModeToolbarButtonWidth)))
                {
                    OpenLoadTutorialDialog();
                    GUIUtility.ExitGUI(); // Workaround: Avoid re-entrant OnGUI call when calling EditorSceneManager.NewScene
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Run Startup Code", EditorStyles.toolbarButton, GUILayout.MaxWidth(s_AuthoringModeToolbarButtonWidth)))
            {
                UserStartupCode.RunStartupCode();
            }

            EditorGUI.BeginChangeCheck();
            maskingEnabled = GUILayout.Toggle(
                    maskingEnabled, "Preview Masking", EditorStyles.toolbarButton, GUILayout.MaxWidth(s_AuthoringModeToolbarButtonWidth)
                    );
            if (EditorGUI.EndChangeCheck())
            {
                ApplyMaskingSettings(true);
                GUIUtility.ExitGUI();
                return;
            }

            EditorGUILayout.EndHorizontal();
        }

        public void GutterGUI(int currentPageIndex, int pageCount, Rect windowRect)
        {
            using (new GUILayout.HorizontalScope(AllTutorialStyles.gutter))
            {
                using (var gutterLeft = new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    var paginationText = string.Format("{0} of {1}", m_CurrentTutorial.currentPageIndex + 1, m_CurrentTutorial.pageCount);
                    GUI.Label(gutterLeft.rect, paginationText, AllTutorialStyles.paginationLabel);
                }

                using (var gutterRight = new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    using (new EditorGUI.DisabledScope(!canMoveToNextPage))
                    {
                        var nextButtonStyle = GUI.enabled ? AllTutorialStyles.nextButton : AllTutorialStyles.nextButtonDisabled;
                        var nextButtonRect = gutterRight.rect;
                        nextButtonRect.xMin += nextButtonStyle.margin.left;
                        nextButtonRect.yMin += nextButtonStyle.margin.top;
                        nextButtonRect.xMax -= nextButtonStyle.margin.right;
                        nextButtonRect.yMax -= nextButtonStyle.margin.bottom;
                        if (GUI.Button(nextButtonRect, m_NextButtonText, nextButtonStyle))
                        {
                            m_CurrentTutorial.TryGoToNextPage();
                            // exit GUI to prevent InvalidOperationException when disposing DisabledScope
                            // some other GUIView might clear the disabled stack when repainting immediately to be unmasked
                            GUIUtility.ExitGUI();
                            Repaint();
                        }
                    }
                }
            }

            float sizeOfEachBox = (windowRect.width / pageCount) * (currentPageIndex + 1);
            var style = AllTutorialStyles.progressBar;
            GUI.DrawTexture(new Rect(0, windowRect.yMax - style.fixedHeight, sizeOfEachBox, style.fixedHeight), style.normal.background);
        }

        private void OnTutorialPagesChanged(Tutorial sender)
        {
            if (sender == null || m_CurrentTutorial == null || m_CurrentTutorial != sender)
                return;

            FormatTitle();
            CreateTutorialViews();

            ApplyMaskingSettings(true);
        }

        private void OnTutorialPageMaskingSettingsChanged(TutorialPage sender)
        {
            if (sender != null && m_CurrentTutorial != null && m_CurrentTutorial.currentPage == sender)
                ApplyMaskingSettings(true);
        }

        private void OnTutorialPageNonMaskingSettingsChanged(TutorialPage sender)
        {
            if (sender != null && m_CurrentTutorial != null && m_CurrentTutorial.currentPage == sender)
                Repaint();
        }

        private void OnShowPage(TutorialPage page, int index)
        {
            m_FarthestPageCompleted = Mathf.Max(m_FarthestPageCompleted, index - 1);
            ApplyMaskingSettings(true);

            AnalyticsHelper.PageShown(page, index);
            PrepareNewPage();
        }

        void OnGUIViewPositionChanged(UnityObject sender)
        {
            if (sender.GetType().Name == "TooltipView")
                return;
            ApplyMaskingSettings(true);
        }

        private void ApplyMaskingSettings(bool applyMask)
        {
            MaskingManager.Unmask();

            if (!applyMask || !maskingEnabled || m_CurrentTutorial == null || m_CurrentTutorial.currentPage == null || IsParentNull())
            {
                InternalEditorUtility.RepaintAllViews();
                return;
            }

            var maskingSettings = m_CurrentTutorial.currentPage.currentMaskingSettings;

            try
            {
                if (maskingSettings != null && maskingSettings.enabled)
                {
                    var unmaskedViews = UnmaskedView.GetViewsAndRects(maskingSettings.unmaskedViews);

                    if (m_CurrentTutorial.currentPageIndex <= m_FarthestPageCompleted)
                    {
                        unmaskedViews = new UnmaskedView.MaskData();
                    }

                    UnmaskedView.MaskData highlightedViews;

                    // if the current page contains no instructions, assume unmasked views should be highlighted because they are called out in narrative text
                    if (unmaskedViews.Count > 0 && !m_CurrentTutorial.currentPage.paragraphs.Any(p => p.type == ParagraphType.Instruction))
                    {
                        highlightedViews = (UnmaskedView.MaskData)unmaskedViews.Clone();
                    }
                    // otherwise, if the current page is completed, highlight this window
                    else if (canMoveToNextPage)
                    {
                        highlightedViews = new UnmaskedView.MaskData();
                        highlightedViews.AddParent(this);
                    }
                    // otherwise, highlight manually specified control rects if there are any
                    else
                    {
                        var unmaskedControls = new List<GUIControlSelector>();
                        var unmaskedViewsWithControlsSpecified =
                            maskingSettings.unmaskedViews.Where(v => v.GetUnmaskedControls(unmaskedControls) > 0).ToArray();
                        // if there are no manually specified control rects, highlight all unmasked views
                        highlightedViews = UnmaskedView.GetViewsAndRects(
                                unmaskedViewsWithControlsSpecified.Length == 0 ?
                                maskingSettings.unmaskedViews : unmaskedViewsWithControlsSpecified
                                );
                    }

                    // ensure tutorial window's HostView and tooltips are not masked
                    unmaskedViews.AddParent(this);
                    unmaskedViews.AddTooltipViews();

                    // tooltip views should not be highlighted
                    highlightedViews.RemoveTooltipViews();

                    MaskingManager.Mask(
                        unmaskedViews,
                        m_Styles == null ? Color.magenta * new Color(1f, 1f, 1f, 0.8f) : m_Styles.maskingColor,
                        highlightedViews,
                        m_Styles == null ? Color.cyan * new Color(1f, 1f, 1f, 0.8f) : m_Styles.highlightColor,
                        m_Styles == null ? 3f : m_Styles.highlightThickness
                        );
                }
            }
            catch (ArgumentException e)
            {
                if (ProjectMode.IsAuthoringMode())
                    Debug.LogException(e, m_CurrentTutorial.currentPage);
                else
                    Console.WriteLine(StackTraceUtility.ExtractStringFromException(e));
            }
            finally
            {
                InternalEditorUtility.RepaintAllViews();
            }
        }

        private void ResetTutorialOnDelegate(PlayModeStateChange playmodeChange)
        {
            switch (playmodeChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.playModeStateChanged -= ResetTutorialOnDelegate;
                    ResetTutorial();
                    break;
            }
        }

        public void ResetTutorial()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.playModeStateChanged += ResetTutorialOnDelegate;
                EditorApplication.isPlaying = false;
                return;
            }
            else if (!EditorApplication.isPlaying)
            {
                m_FarthestPageCompleted = -1;
                if (m_CurrentTutorial != null)
                    m_CurrentTutorial.ReloadTutorial();
            }
        }

        public void ResetTutorialAndWriteDefaults()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Defaults cannot be written during play mode");
                return;
            }

            ResetTutorial();
            m_CurrentTutorial.WriteAssetsToTutorialDefaultsFolder();
        }

        static public void LoadWindowLayoutAndSetUpTutorial(string layoutPath, Tutorial tutorial)
        {
            EditorUtility.LoadWindowLayout(layoutPath);
            var tutorialWindow = EditorWindow.GetWindow<TutorialWindow>();
            tutorialWindow.SetTutorial(tutorial, reload: false);
        }
    }
}
