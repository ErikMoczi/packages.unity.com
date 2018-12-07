using System;
using UnityEngine;
using UnityEditor;

namespace Unity.InteractiveTutorials
{
    public enum TutorialConclusion
    {
        Completed,
        Quit,
        Reloaded
    }

    class TutorialAnalyticsEventData
    {
        public string tutorialName;
        public string version;
        public TutorialConclusion conclusion;
        public string lessonID;

        public TutorialAnalyticsEventData(string tutorialName, string version, TutorialConclusion conclusion, string lessonID)
        {
            this.tutorialName = tutorialName;
            this.version = version;
            this.conclusion = conclusion;
            this.lessonID = lessonID;
        }
    }

    public enum TutorialPageConclusion
    {
        Completed,
        Reviewed
    }

    class TutorialPageAnalyticsEventData
    {
        public string tutorialName;
        public int pageIndex;
        public string guid;
        public TutorialPageConclusion conclusion;

        public TutorialPageAnalyticsEventData(string tutorialName, int pageIndex, string guid, TutorialPageConclusion conclusion)
        {
            this.tutorialName = tutorialName;
            this.pageIndex = pageIndex;
            this.guid = guid;
            this.conclusion = conclusion;
        }
    }

    public enum TutorialParagraphConclusion
    {
        Completed,
        Regressed
    }

    class TutorialParagraphAnalyticsEventData
    {
        public string tutorialName;
        public int pageIndex;
        public int paragraphIndex;
        public TutorialParagraphConclusion conclusion;

        public TutorialParagraphAnalyticsEventData(string tutorialName, int pageIndex, int paragraphIndex, TutorialParagraphConclusion conclusion)
        {
            this.tutorialName = tutorialName;
            this.pageIndex = pageIndex;
            this.paragraphIndex = paragraphIndex;
            this.conclusion = conclusion;
        }
    }

    class AnalyticsHelper : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        Tutorial currentTutorial;

        [SerializeField]
        TutorialPage currentPage;

        [SerializeField]
        int currentPageIndex;

        [SerializeField]
        TutorialPage lastPage;

        [SerializeField]
        int lastPageIndex;

        [SerializeField]
        int currentParagraphIndex;

        DateTime currentTutorialStartTime;

        DateTime currentPageStartTime;

        DateTime lastPageStartTime;

        DateTime currentParagraphStartTime;

        [SerializeField]
        long currentTutorialStartTicks;

        [SerializeField]
        long currentPageStartTicks;

        [SerializeField]
        long lastPageStartTicks;

        [SerializeField]
        long currentParagraphStartTicks;

        public static AnalyticsHelper Instance
        {
            get
            {
                if (!s_Instance)
                {
                    var instance = Resources.FindObjectsOfTypeAll<AnalyticsHelper>();
                    if (instance.Length == 0)
                    {
                        s_Instance = CreateInstance<AnalyticsHelper>();
                        s_Instance.hideFlags = HideFlags.HideAndDontSave;
                    }
                    else
                    {
                        s_Instance = instance[0] as AnalyticsHelper;
                    }
                }
                return s_Instance;
            }
        }
        static AnalyticsHelper s_Instance;

        public void OnBeforeSerialize()
        {
            currentTutorialStartTicks = currentTutorialStartTime.Ticks;
            currentPageStartTicks = currentPageStartTime.Ticks;
            lastPageStartTicks = lastPageStartTime.Ticks;
            currentParagraphStartTicks = currentParagraphStartTime.Ticks;
        }

        public void OnAfterDeserialize()
        {
            currentTutorialStartTime = new DateTime(currentTutorialStartTicks, DateTimeKind.Utc);
            currentPageStartTime = new DateTime(currentPageStartTicks, DateTimeKind.Utc);
            lastPageStartTime = new DateTime(lastPageStartTicks, DateTimeKind.Utc);
            currentParagraphStartTime = new DateTime(currentParagraphStartTicks, DateTimeKind.Utc);
        }

        static void DebugWarning(string message, params object[] args)
        {
#if DEBUG_TUTORIALS
            Debug.LogWarningFormat(message, args);
#endif
        }

        static void DebugLog(string message, params object[] args)
        {
#if DEBUG_TUTORIALS
            Debug.LogFormat(message, args);
#endif
        }

        internal static void TutorialStarted(Tutorial tutorial)
        {
            if (Instance.currentTutorial != null)
            {
                DebugWarning("TutorialStarted Ignored because tutorial is already set: {0}", tutorial);
                return;
            }
            DebugLog("Tutorial Started: {0}", tutorial);
            Instance.currentTutorial = tutorial;
            Instance.currentTutorialStartTime = DateTime.UtcNow;
            Instance.currentPageIndex = Instance.lastPageIndex = Instance.currentParagraphIndex = -1;
            Instance.currentPage = Instance.lastPage = null;
        }

        internal static void TutorialEnded(TutorialConclusion conclusion)
        {
            if (Instance.currentTutorial == null)
            {
                DebugWarning("TutorialEnded Ignored because no tutorial is set");
                return;
            }
            if (conclusion == TutorialConclusion.Completed)
                PageShown(Instance.lastPage, Instance.lastPageIndex + 1);  // "Show" a dummy page to get the last page to report
            var data = new TutorialAnalyticsEventData(Instance.currentTutorial.name, Instance.currentTutorial.version, conclusion, Instance.currentTutorial.LessonId);
            UsabilityAnalyticsProxy.SendEvent("tutorial", Instance.currentTutorialStartTime, DateTime.UtcNow - Instance.currentTutorialStartTime, false, data);
            DebugLog("Tutorial Ended");
            Instance.currentTutorial = null;
        }

        internal static void PageShown(TutorialPage page, int pageIndex)
        {
            if (Instance.currentTutorial == null)
            {
                DebugWarning("PageShown Ignored because no tutorial is set");
                return;
            }
            if (Instance.lastPage != null)
            {
                if (Instance.currentPageIndex < Instance.lastPageIndex)
                {
                    var data = new TutorialPageAnalyticsEventData(Instance.currentTutorial.name, Instance.currentPageIndex, Instance.currentPage.guid, TutorialPageConclusion.Reviewed);
                    UsabilityAnalyticsProxy.SendEvent("tutorialPage", Instance.currentPageStartTime, DateTime.UtcNow - Instance.currentPageStartTime, false, data);
                    DebugLog("Page Reviewed: {0}", Instance.currentPageIndex);
                }
                else if (pageIndex > Instance.lastPageIndex)
                {
                    var data = new TutorialPageAnalyticsEventData(Instance.currentTutorial.name, Instance.lastPageIndex, Instance.lastPage.guid, TutorialPageConclusion.Completed);
                    UsabilityAnalyticsProxy.SendEvent("tutorialPage", Instance.lastPageStartTime, DateTime.UtcNow - Instance.lastPageStartTime, false, data);
                    DebugLog("Page Completed: {0}", Instance.lastPageIndex);
                }
            }
            Instance.currentPageIndex = pageIndex;
            Instance.currentPage = page;
            Instance.currentPageStartTime = DateTime.UtcNow;
            if (Instance.currentPageIndex > Instance.lastPageIndex)
            {
                Instance.lastPageIndex = pageIndex;
                Instance.lastPage = page;
                Instance.lastPageStartTime = DateTime.UtcNow;
                Instance.currentParagraphIndex = -1;
            }
        }

        internal static void ParagraphStarted(int paragraphIndex)
        {
            if (Instance.currentTutorial == null)
            {
                DebugWarning("ParagraphStarted Ignored because no tutorial is set: {0}", paragraphIndex);
                return;
            }
            if (Instance.currentParagraphIndex >= 0)
                ParagraphEnded(true);
            DebugLog("Paragraph Started: {0}", paragraphIndex);
            Instance.currentParagraphStartTime = DateTime.UtcNow;
            Instance.currentParagraphIndex = paragraphIndex;
        }

        internal static void ParagraphEnded()
        {
            ParagraphEnded(false);
        }

        internal static void ParagraphEnded(bool regressed)
        {
            if (Instance.currentTutorial == null)
            {
                DebugWarning("ParagraphEnded Ignored because no tutorial is set");
                return;
            }
            if (Instance.currentParagraphIndex == -1)
            {
                DebugWarning("ParagraphEnded Ignored because no paragraph is active");
                return;
            }
            DebugLog("Paragraph Ended: regression = {0}", regressed);
            var conclusion = regressed ? TutorialParagraphConclusion.Regressed : TutorialParagraphConclusion.Completed;
            var data = new TutorialParagraphAnalyticsEventData(Instance.currentTutorial.name, Instance.currentPageIndex, Instance.currentParagraphIndex, conclusion);
            UsabilityAnalyticsProxy.SendEvent("tutorialParagraph", Instance.currentParagraphStartTime, DateTime.UtcNow - Instance.currentParagraphStartTime, false, data);
            Instance.currentParagraphIndex = -1;
        }
    }
}
