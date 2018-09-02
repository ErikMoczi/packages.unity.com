using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Networking.PlayerConnection;
using System.Collections.Generic;

namespace EditorDiagnostics
{
    public class EventViewerWindow : EditorWindow
    {
        [SerializeField]
        EventDataCollection eventData;
        GUIContent prevFrameIcon;
        GUIContent nextFrameIcon;
        int m_playerSessionIndex = 0;
        public int m_inspectFrame = 0;
        bool m_record = true;
        int m_eventListFrame = -1;
        VerticalSplitter verticalSplitter = new VerticalSplitter();
        HorizontalSplitter horizontalSplitter = new HorizontalSplitter();

        TreeViewState m_eventListTreeViewState;
        MultiColumnHeaderState m_eventListMCHS;
        EventListView m_eventList;

        TreeViewState m_graphListTreeViewState;
        MultiColumnHeaderState m_graphListMCHS;
        EventGraphListView m_graphList;

        EventDataCollection.PlayerSession activeSession { get { return eventData == null ? null : eventData.GetSessionByIndex(m_playerSessionIndex); } }

        private void Awake()
        {
            prevFrameIcon = EditorGUIUtility.IconContent("Profiler.PrevFrame", "|Go back one frame");
            nextFrameIcon = EditorGUIUtility.IconContent("Profiler.NextFrame", "|Go one frame forwards");
        }

        private void OnEnable()
        {
            lastEventListUpdate = 0;
            eventData = new EventDataCollection();
            eventData.AddSession("Editor", m_playerSessionIndex = 0);
            eventData.onEvent += OnEventProcessed;
            eventData.onRecordEvent += OnRecordEvent;
            EditorConnection.instance.Initialize();
            EditorConnection.instance.Register(EventCollector.eventGUID, OnPlayerConnectionMessage);
            EditorConnection.instance.RegisterConnection(OnPlayerConnection);
            EditorConnection.instance.RegisterDisconnection(OnPlayerDisconnection);
            EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
            RegisterEventHandler(true);
        }

        private void OnDisable()
        {
            EditorConnection.instance.Unregister(EventCollector.eventGUID, OnPlayerConnectionMessage);
            RegisterEventHandler(false);
            EditorApplication.playModeStateChanged -= OnEditorPlayModeChanged;
        }

        bool m_registeredWithRM = false;
        void RegisterEventHandler(bool reg)
        {
            if (reg == m_registeredWithRM)
                return;

            if (m_registeredWithRM = reg)
                EventCollector.RegisterEventHandler(OnEvent);
            else
                EventCollector.UnregisterEventHandler(OnEvent);
        }

        public void OnEvent(DiagnosticEvent evt)
        {
            eventData.ProcessEvent(evt, 0);
        }

        void OnPlayerConnection(int id)
        {
            eventData.GetPlayerSession(id, true).m_isActive = true;
        }

        void OnPlayerDisconnection(int id)
        {
            var c = eventData.GetPlayerSession(id, false);
            if (c != null)
                c.m_isActive = false;
        }

        void OnPlayerConnectionMessage(UnityEngine.Networking.PlayerConnection.MessageEventArgs args)
        {
            if (!m_record)
                return;
            var evt = DiagnosticEvent.Deserialize(args.data);
            eventData.ProcessEvent(evt, args.playerId);
        }

        protected virtual bool CanHandleEvent(string graph)
        {
            if (graph == "EventCount")
                return true;
            return OnCanHandleEvent(graph);
        }

        protected virtual bool OnCanHandleEvent(string graph) { return true; }

        private void OnEditorPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                lastEventListUpdate = 0;
                m_inspectFrame = -1;
                latestFrame = -1;
                m_playerSessionIndex = 0;
                RegisterEventHandler(true);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                RegisterEventHandler(false);
            }
        }

        protected virtual bool OnRecordEvent(DiagnosticEvent evt)
        {
            return false;
        }

        int latestFrame = 0;
        void OnEventProcessed(EventDataCollection.PlayerSession session, DiagnosticEvent evt, bool entryCreated)
        {
            if (!CanHandleEvent(evt.m_graph))
                return;

            bool moveInspectFrame = latestFrame < 0 || m_inspectFrame == latestFrame;
            latestFrame = evt.m_frame;
            if (entryCreated)
            {
                if (m_graphList != null)
                    m_graphList.Reload();
            }

            if (moveInspectFrame)
                m_inspectFrame = latestFrame;

            if (evt.m_id == "Events")
            {
                Repaint();
            }
        }

        bool draggingInspectLine = false;
        private void OnGUI()
        {
            var session = activeSession;
            if (session == null)
                return;
            InitializeGUI();

            DrawToolBar(session);

            var r = EditorGUILayout.GetControlRect();
            Rect contentRect = new Rect(r.x, r.y, r.width, position.height - (r.y + r.x));
            if (showEventPanel)
            {
                Rect top, bot;
                bool resizingVer = verticalSplitter.OnGUI(contentRect, out top, out bot);

                var graphRect = m_graphList.GetGraphRect();

                ProcessInspectFrame(graphRect);

                m_graphList.OnGUI(top, m_inspectFrame);

                DrawInspectFrame(graphRect);

                bool resizingHor = false;
                if (showEventDetailPanel)
                {
                    Rect left, right;
                    resizingHor = horizontalSplitter.OnGUI(bot, out left, out right);
                    m_eventList.OnGUI(left);
                    OnEventDetailGUI(right, m_eventList.selectedEvent);
                }
                else
                {
                    m_eventList.OnGUI(bot);
                }
                if (resizingVer || resizingHor)
                    Repaint();
            }
            else
            {
                var graphRect = m_graphList.GetGraphRect();
                ProcessInspectFrame(graphRect);
                m_graphList.OnGUI(contentRect, m_inspectFrame);
                DrawInspectFrame(graphRect);
            }
        }

        protected virtual bool showEventDetailPanel { get { return false; } }
        protected virtual bool showEventPanel { get { return false; } }

        protected virtual void OnEventDetailGUI(Rect right, DiagnosticEvent selectedEvent)
        {
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void ProcessInspectFrame(Rect graphRect)
        {
            if (Event.current.type == EventType.MouseDown && graphRect.Contains(Event.current.mousePosition))
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPaused = true;
                draggingInspectLine = true;
                m_eventList.SetEvents(null);
            }
            if (draggingInspectLine && (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.Repaint))
                m_inspectFrame = m_graphList.visibleStartTime + (int)GraphUtility.PixelToValue(Event.current.mousePosition.x, graphRect.xMin, graphRect.xMax, m_graphList.visibleDuration);
            if (Event.current.type == EventType.MouseUp)
            {
                draggingInspectLine = false;
                m_eventList.SetEvents(activeSession.GetFrameEvents(m_inspectFrame));
                lastEventListUpdate = Time.unscaledTime;
                m_eventListFrame = m_inspectFrame;
            }
        }

        private void DrawInspectFrame(Rect graphPanelRect)
        {
            if (m_inspectFrame != latestFrame)
            {
                var ix = graphPanelRect.xMin + GraphUtility.ValueToPixel(m_inspectFrame, m_graphList.visibleStartTime, m_graphList.visibleStartTime + m_graphList.visibleDuration, graphPanelRect.width);
                EditorGUI.DrawRect(new Rect(ix - 1, graphPanelRect.yMin, 3, graphPanelRect.height), Color.white * .8f);
            }
        }

        private void DrawToolBar(EventDataCollection.PlayerSession session)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            m_record = GUILayout.Toggle(m_record, "Record", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                session.Clear();
            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
                session.Load();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                session.Save();

            GUILayout.FlexibleSpace();
            GUILayout.Label(m_inspectFrame == latestFrame ? "Frame:     " : "Frame: " + m_inspectFrame + "/" + latestFrame, EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(m_inspectFrame <= 0))
                if (GUILayout.Button(prevFrameIcon, EditorStyles.toolbarButton))
                    m_inspectFrame--;

            using (new EditorGUI.DisabledScope(m_inspectFrame >= latestFrame))
                if (GUILayout.Button(nextFrameIcon, EditorStyles.toolbarButton))
                    m_inspectFrame++;

            if (GUILayout.Button("Current", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                m_inspectFrame = latestFrame;
            GUILayout.EndHorizontal();
        }

        protected virtual void OnGetColumns(List<string> columnNames, List<float> columnSizes)
        {
            columnNames.Add("Event"); columnSizes.Add(50);
            columnNames.Add("Id"); columnSizes.Add(200);
            columnNames.Add("Data"); columnSizes.Add(400);
        }

        protected virtual bool OnColumnCellGUI(Rect cellRect, DiagnosticEvent evt, int column)
        {
            return false;
        }

        protected virtual void ColumnCellGUI(Rect cellRect, DiagnosticEvent evt, int column)
        {
            if (!OnColumnCellGUI(cellRect, evt, column))
            {
                switch (column)
                {
                    case 0: EditorGUI.LabelField(cellRect, evt.m_stream.ToString()); break;
                    case 1: EditorGUI.LabelField(cellRect, evt.m_id); break;
                    case 2: EditorGUI.LabelField(cellRect, evt.m_data == null ? "null" : evt.m_data.ToString()); break;
                }
            }
        }

        private void InitializeGUI()
        {
            if (m_graphList == null)
            {
                if (m_graphListTreeViewState == null)
                    m_graphListTreeViewState = new TreeViewState();

                var headerState = EventGraphListView.CreateDefaultMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_graphListMCHS, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_graphListMCHS, headerState);

                m_graphListMCHS = headerState;
                m_graphList = new EventGraphListView(activeSession, m_graphListTreeViewState, m_graphListMCHS, CanHandleEvent);
                InitializeGraphView(m_graphList);
                m_graphList.Reload();
            }

            if (m_eventList == null)
            {
                if (m_eventListTreeViewState == null)
                    m_eventListTreeViewState = new TreeViewState();

                var columns = new List<string>();
                var sizes = new List<float>();
                OnGetColumns(columns, sizes);
                var headerState = EventListView.CreateDefaultMultiColumnHeaderState(columns, sizes);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_eventListMCHS, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_eventListMCHS, headerState);

                m_eventListMCHS = headerState;
                m_eventList = new EventListView(m_eventListTreeViewState, m_eventListMCHS, ColumnCellGUI, OnRecordEvent);
                m_eventList.Reload();
            }

            if (m_eventListFrame != m_inspectFrame && m_inspectFrame != latestFrame && !draggingInspectLine && Time.unscaledTime - lastEventListUpdate > .25f)
            {
                m_eventList.SetEvents(activeSession.GetFrameEvents(m_inspectFrame));
                lastEventListUpdate = Time.unscaledTime;
                m_eventListFrame = m_inspectFrame;
            }
        }

        float lastEventListUpdate = 0;

        void InitializeGraphView(EventGraphListView gv)
        {
            gv.DefineGraph("EventCount", 0, new GraphLayerVertValueLine(0, "Events", "Event count per frame", Color.green));
            //gv.DefineGraph("EventCount", 0, new GraphLayerVertValueLineRects(0, "Events", "Event count per frame", Color.green));
            OnInitializeGraphView(gv);
        }

        virtual protected void OnInitializeGraphView(EventGraphListView gv) {}
    }
    [Serializable]
    class VerticalSplitter
    {
        bool resizing = false;
        Rect rect = new Rect(0, 0, 0, 3);
        public Rect splitterRect { get { return rect; } }
        [SerializeField]
        float currentPercent = .8f;
        float minPercent = .2f;
        float maxPercent = .9f;
        public VerticalSplitter(float percent = .8f, float minPer = .2f, float maxPer = .9f)
        {
            currentPercent = percent;
            minPercent = minPer;
            maxPercent = maxPer;
        }

        public bool OnGUI(Rect content, out Rect top, out Rect bot)
        {
            rect.x = content.x;
            rect.y = (int)(content.y + content.height * currentPercent);
            rect.width = content.width;

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                resizing = true;

            if (resizing)
            {
                EditorGUIUtility.AddCursorRect(content, MouseCursor.ResizeVertical);

                var mousePosInRect = Event.current.mousePosition.y - content.y;
                currentPercent = Mathf.Clamp(mousePosInRect / content.height, minPercent, maxPercent);
                rect.y = Mathf.Min((int)(content.y + content.height * currentPercent), content.yMax - rect.height);
                if (Event.current.type == EventType.MouseUp)
                    resizing = false;
            }

            top = new Rect(content.x, content.y, content.width, rect.yMin - content.yMin);
            bot = new Rect(content.x, rect.yMax, content.width, content.yMax - rect.yMax);
            return resizing;
        }
    }

    [Serializable]
    class HorizontalSplitter
    {
        bool resizing = false;
        Rect rect = new Rect(0, 0, 3, 0);
        public Rect splitterRect { get { return rect; } }
        [SerializeField]
        float currentPercent = .8f;
        float minPercent = .2f;
        float maxPercent = .9f;
        public HorizontalSplitter(float percent = .8f, float minPer = .2f, float maxPer = .9f)
        {
            currentPercent = percent;
            minPercent = minPer;
            maxPercent = maxPer;
        }

        public bool OnGUI(Rect content, out Rect left, out Rect right)
        {
            rect.y = content.y;
            rect.x = (int)(content.x + content.width * currentPercent);
            rect.height = content.height;

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                resizing = true;

            if (resizing)
            {
                EditorGUIUtility.AddCursorRect(content, MouseCursor.ResizeHorizontal);

                var mousePosInRect = Event.current.mousePosition.x - content.x;
                currentPercent = Mathf.Clamp(mousePosInRect / content.width, minPercent, maxPercent);
                rect.x = Mathf.Min((int)(content.x + content.width * currentPercent), content.xMax - rect.width);
                if (Event.current.type == EventType.MouseUp)
                    resizing = false;
            }

            left = new Rect(content.x, content.y, rect.xMin, content.height);
            right = new Rect(rect.xMax, content.y, content.width - rect.xMax, content.height);
            return resizing;
        }
    }
}
