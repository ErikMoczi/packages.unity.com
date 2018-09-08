using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Text;
using Unity.Profiling.Memory.UI;
using UnityEngine.Profiling.Memory.Experimental;
using UnityEditor.Profiling.Memory.Experimental;
using Unity.MemoryProfiler.Editor.Database;
using Unity.MemoryProfiler.Editor.UI;
using Unity.MemoryProfiler.Editor.Debuging;

namespace Unity.MemoryProfiler.Editor
{
    using UnityMemoryProfiler = UnityEngine.Profiling.Memory.Experimental.MemoryProfiler;
    public class MemoryProfilerWindow : EditorWindow, UI.IViewPaneEventListener
    {
        static class Content
        {
            public static readonly GUIContent SaveCollection = new GUIContent("Save...", "Save current collection file.");
            public static readonly GUIContent LoadCollection = new GUIContent("Load...", "Load a collection.");
            public static readonly GUIContent NewCollection = new GUIContent("New", "Create new collection.");
            public static readonly GUIContent UnloadCollection = new GUIContent("X", "Unload current collection file. The file will not be deleted!");
            public static readonly GUIContent SaveCapture = new GUIContent("Save...", "Save capture information to a binary file.");
            public static readonly GUIContent LoadCapture = new GUIContent("Load...", "Load capture information from a file.");
            public static readonly GUIContent LoadViewFile = new GUIContent("Load View...", "Load a view from a file.");
            public static readonly GUIContent DiffCaptures = new GUIContent("Diff...", "Load a saved snapshot to show the difference between it and the current one.");
            public static readonly GUIContent CaptureData = new GUIContent("Capture", "Captures a memory snapshot. Warning, this can take a moment.");
            public static readonly GUIContent History = new GUIContent("View History:");
            public static readonly GUIContent BackwardsInHistory = EditorGUIUtility.IconContent("Profiler.PrevFrame", "Previous view");
            public static readonly GUIContent ForwardsInHistory = EditorGUIUtility.IconContent("Profiler.NextFrame", "Next view");
            public static readonly GUIContent NoneView = new GUIContent("None", "");
            public static readonly GUIContent MemoryMapView = new GUIContent("Memory Map", "Show Snapshot as Memory Map");
            public static readonly GUIContent TreeMapView = new GUIContent("Tree Map", "Show Snapshot as Memory Tree");
            public static readonly GUIContent TableMapViewRoot = new GUIContent("Table/", "");
            public static readonly GUIContent RawDataTableMapViewRoot = new GUIContent("Raw Data/", "");
            public static readonly GUIContent EmptyWorkbench = new GUIContent("No snapshots available", "Take a Capture or load a workbench or snapshot file.");

            public static readonly GUIContent CollectionsHeader = new GUIContent("Collection", "You can order your snapshots into multiple collections.");
            public static readonly GUIContent ExitCollectionRename = new GUIContent("x", "Exit rename and save.");
            public static readonly GUIContent SnapshotsHeader = new GUIContent("Snapshots", "This lists all snapshots in the currently opened collection.");

            public static readonly GUIContent UnloadSnapshot = new GUIContent("-", "Remove snapshot from collection.");
            public static readonly GUIContent OpenSnapshot = new GUIContent("Open", "Open snapshot in view.");
            public static readonly GUIContent DiffSnapshot = new GUIContent("Diff", "Diff with open snapshot.");
            public static readonly GUIContent SwitchDiffFocus = new GUIContent("", "Only show this snapshot. Click again to go back to seeing the diff.");

            public const string LoadSnapshotFilePanelText = "Load Snapshot";
            public const string LoadSecondSnapshotFilePanelText = "Load Second Snapshot";
            public const string SaveSnapshotFilePanelText = "Save Snapshot";
            public const string LoadViewFilePanelText = "Load View";
        }

        static class Styles
        {
            public static readonly GUIStyle HeaderLabel = new GUIStyle(EditorStyles.toolbarButton);//new GUIStyle(EditorStyles.largeLabel);
            public static readonly GUIStyle ToolbarPopup = new GUIStyle(EditorStyles.toolbarPopup);
            public static readonly GUIStyle HeaderBar = new GUIStyle(EditorStyles.toolbar);
            public static readonly GUIStyle HeaderButton = new GUIStyle(EditorStyles.toolbarButton);
            public static readonly GUIStyle WorkbenchLabel = new GUIStyle(EditorStyles.label);
            public static readonly GUIStyle CollectionLabelSelected = new GUIStyle(EditorStyles.label);
            public static readonly GUIStyle ExitCollectionRename = new GUIStyle(EditorStyles.miniButtonLeft);
            public static readonly GUIStyle SnapshotListItemButtons = new GUIStyle(EditorStyles.miniButton);
            public static readonly GUIStyle SnapshotListDiffToggle = new GUIStyle(EditorStyles.radioButton);
            public static readonly GUIStyle SnapshotListItemBox = new GUIStyle(EditorStyles.helpBox);
            public static readonly GUIStyle SnapshotListItemBoxLoaded = new GUIStyle(EditorStyles.helpBox);
            public static readonly GUIStyle SnapshotListItemBoxLoadedDiff = new GUIStyle(EditorStyles.helpBox);
            public static readonly GUIStyle SnapshotListItemBoxLoadedDiffActive = new GUIStyle(EditorStyles.helpBox);
            public static readonly GUIStyle SnapshotListItemBoxLoadedDiffInactive = new GUIStyle(EditorStyles.helpBox);

            public const int ViewPaneMargin = 2;
            public const int WorkbenchDefaultWidth = 2 * SnapshotButtonMargin + 3 * k_SnapshotButtonWidth;
            public const int SnapshotButtonMargin = 25;
            const int k_SnapshotButtonWidth = 55 + 15;
            public static readonly float SnapshotPreviewImageHeight = EditorGUIUtility.singleLineHeight * 3;
            public static readonly float SnapshotPreviewImageWidth = SnapshotPreviewImageHeight * 2;
            public static readonly float SnapshotListItemWidth = WorkbenchDefaultWidth - EditorGUIUtility.singleLineHeight - 5f;
            public static readonly float RightMargin = 2f;

            public static readonly GUILayoutOption[] CollectionRenameFieldOptions = new GUILayoutOption[] {
                GUILayout.ExpandWidth(true), GUILayout.MinWidth(2 * k_SnapshotButtonWidth),
            };
            static Styles()
            {
                CollectionLabelSelected.normal = CollectionLabelSelected.onActive;
            }
        }

        const string k_SnapshotsFolderName = "/MemoryCaptures";
        const string k_SnapshotFileNamePart = "/MemoryCapture-";
        const string k_SnapshotFileExtension = ".snap";
        const string k_ViewFileExtension = "xml";
        const string k_RawCategoryName = "Raw";

        Vector2 m_SnapshotListScrollViewPosition;
        static int snapshotCounter = 0;
        static Vector2 s_ViewDropdownSize;

        [MenuItem("Window/Analysis/Memory Profiler", false, 2)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<MemoryProfilerWindow>("Memory Profiler");
        }

        CaptureFlags m_CaptureFlags = CaptureFlags.ManagedObjects
            | CaptureFlags.NativeObjects
            | CaptureFlags.NativeAllocations
            | CaptureFlags.NativeAllocationSites
            | CaptureFlags.NativeStackTraces;

        StringBuilder m_TabelNameStringBuilder = new StringBuilder();
        Dictionary<string, GUIContent> m_UIFriendlyViewOptionNamesWithFullPath = new Dictionary<string, GUIContent>();
        Dictionary<string, GUIContent> m_UIFriendlyViewOptionNames = new Dictionary<string, GUIContent>();

        [NonSerialized]
        SnapshotCollection.SnapshotUIData m_FirstOrOnlySnapshotLoaded;
        [NonSerialized]
        SnapshotCollection.SnapshotUIData m_SecondSnapshotLoaded;

        [System.NonSerialized]
        static MemoryProfilerWorkbench s_WorkBench;

        // TODO: move this to  "ProjectSettings/MemoryProfilerWorkspace.asset" once we're trunk based and have access to Unified Settings
        const string k_CollectionsFileNameAndPath = "Assets/MemoryProfilerWorkspace.asset";
        static MemoryProfilerWorkbench Workbench
        {
            get
            {
                if (s_WorkBench == null)
                {
                    // try to load the Collections file
                    s_WorkBench = AssetDatabase.LoadAssetAtPath<MemoryProfilerWorkbench>(k_CollectionsFileNameAndPath);
                    if (s_WorkBench == null)
                    {
                        // if none exists, create a new one
                        s_WorkBench = ScriptableObject.CreateInstance<MemoryProfilerWorkbench>();
                        AssetDatabase.CreateAsset(s_WorkBench, k_CollectionsFileNameAndPath);
                    }
                }
                return s_WorkBench;
            }
        }

        UI.HistoryEvent m_EventToOpen;

        public UI.UIState m_UIState = null;
        private UI.ViewPane currentViewPane
        {
            get
            {
                if (m_UIState.currentMode == null) return null;
                return m_UIState.currentMode.currentViewPane;
            }
            set
            {
                m_UIState.currentMode.currentViewPane = value;
            }
        }
        public MemoryProfilerWindow()
        {
            m_UIState = new UI.UIState();
        }

        void SnapshotFinished(string path, bool result)
        {
            if (result)
            {
                OnSnapshotReceived(PackedMemorySnapshot.Load(path));
            }
        }

        void MetaDataCollect(MetaData data)
        {
            data.content = "User Information";
            data.platform = Application.platform.ToString();

            int width = Screen.width;
            int height = Screen.height;

            if (Application.isPlaying)
            {
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
                //Read pixels from the currently active render target into the newly created image
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, true);

                int divider = 0;

                while (width > 480 || height > 240)
                {
                    width /= 2;
                    height /= 2;
                    ++divider;
                }

                data.screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                data.screenshot.SetPixels(tex.GetPixels(divider));
                data.screenshot.Apply();
            }
        }

        void OnEnable()
        {
            UnityMemoryProfiler.createMetaData += MetaDataCollect;
        }

        void OnDisable()
        {
            UnityMemoryProfiler.createMetaData -= MetaDataCollect;
        }

        void UI.IViewPaneEventListener.OnRepaint()
        {
            Repaint();
        }

        void UI.IViewPaneEventListener.OnOpenTable(Database.View.LinkRequest link)
        {
            OpenTable(link);
        }

        void UI.IViewPaneEventListener.OnOpenMemoryMap()
        {
            OpenMemoryMap();
        }

        void UI.IViewPaneEventListener.OnOpenTreeMap()
        {
            OpenTreeMap(null);
        }

        void SetSnapshot(PackedMemorySnapshot snapshot)
        {
            m_UIState = new UI.UIState();
            m_UIState.SetSnapshot(snapshot);
            ShowNothing();
        }

        void OnSnapshotReceived(PackedMemorySnapshot snapshot)
        {
            try
            {
                using (new Service<IDebugContextService>.ScopeService(new DebugContextService()))
                {
                    ClearCurrentlyOpenedCapturesUIData();
                    Workbench.CurrentCollection.Collection.AddCapture(snapshot, snapshot.filePath);
                    EditorUtility.SetDirty(Workbench);
                }
            }
            catch (System.Exception e)
            {
                throw new System.Exception(DebugUtility.GetExceptionHelpMessage(e));
            }
        }

        void TransitMode(UIState.BaseMode newMode)
        {
            m_UIState.currentMode = newMode;
        }

        void TransitModeForTable(Table table)
        {
            if (m_UIState.diffMode != null)
            {
                //open the appropriate snapshot mode, the one the table is from.
                if (m_UIState.diffMode.modeFirst.GetSheme().OwnsTable(table))
                {
                    TransitMode(m_UIState.diffMode.modeFirst);
                }
                else if (m_UIState.diffMode.modeSecond.GetSheme().OwnsTable(table))
                {
                    TransitMode(m_UIState.diffMode.modeSecond);
                }
                else if (m_UIState.diffMode.GetSheme().OwnsTable(table))
                {
                    TransitMode(m_UIState.diffMode);
                }
            }
        }

        void TransitPane(UI.ViewPane newPane)
        {
            if (m_UIState.currentMode.currentViewPane != null)
            {
                m_UIState.currentMode.currentViewPane.OnClose();
            }
            m_UIState.currentMode.currentViewPane = newPane;
        }

        void ShowNothing()
        {
            TransitPane(null);
        }

        void StepBackwardsInHistory()
        {
            var history = m_UIState.history;
            if (history.hasPast)
            {
                if (!history.hasPresentEvent)
                {
                    if (m_UIState.currentMode != null)
                    {
                        history.SetPresentEvent(m_UIState.currentMode.GetCurrentHistoryEvent());
                    }
                }
                m_EventToOpen = history.Backward();
                Repaint();
            }
        }

        void StepForwardsInHistory()
        {
            var evt = m_UIState.history.Forward();
            if (evt != null)
            {
                m_EventToOpen = evt;
                Repaint();
            }
        }

        void AddCurrentHistoryEvent()
        {
            if (currentViewPane != null)
            {
                m_UIState.AddHistoryEvent(currentViewPane.GetCurrentHistoryEvent());
            }
        }

        void OpenTable(Database.View.LinkRequest link)
        {
            var tableLink = new Database.TableLink(link.metaLink.linkViewName, link.param);
            var table = m_UIState.currentMode.GetSheme().GetTableByLink(tableLink);

            var pane = new UI.SpreadsheetPane(m_UIState, this);
            if (pane.OpenLinkRequest(link, tableLink, table))
            {
                TransitModeForTable(table);
                TransitPane(pane);
            }
        }

        void OpenTable(Database.TableLink link, Database.Table table)
        {
            TransitModeForTable(table);
            var pane = new UI.SpreadsheetPane(m_UIState, this);
            pane.OpenTable(link, table);
            TransitPane(pane);
        }

        void OpenTable(Database.TableLink link, Database.Table table, Database.CellPosition pos)
        {
            TransitModeForTable(table);
            var pane = new UI.SpreadsheetPane(m_UIState, this);
            pane.OpenTable(link, table, pos);
            TransitPane(pane);
        }

        void OpenHistoryEvent(UI.HistoryEvent evt)
        {
            if (evt == null) return;
            if (evt is UI.HETable)
            {
                var eventTable = (UI.HETable)evt;
                TransitMode(eventTable.mode);
                var pane = new UI.SpreadsheetPane(m_UIState, this);
                pane.OpenHistoryEvent(eventTable);
                TransitPane(pane);
            }
            else if (evt is UI.HEMemoryMap)
            {
                var eventMemoryMap = (UI.HEMemoryMap)evt;
                TransitMode(eventMemoryMap.mode);
                OpenMemoryMap();
            }
            else if (evt is UI.HETreeMap)
            {
                var eventTreeMap = (UI.HETreeMap)evt;
                TransitMode(eventTreeMap.mode);
                OpenTreeMap(evt as UI.HETreeMap);
            }
        }

        void OpenMemoryMap()
        {
            var pane = new UI.MemoryMapPane(m_UIState, this);

            TransitPane(pane);
        }

        void OpenTreeMap(UI.HETreeMap evt)
        {
            if (currentViewPane is UI.TreeMapPane)
            {
                if (evt != null)
                {
                    (currentViewPane as UI.TreeMapPane).OpenHistoryEvent(evt);
                    return;
                }
            }
            var pane = new UI.TreeMapPane(m_UIState, this);
            if (evt != null) pane.OpenHistoryEvent(evt);
            TransitPane(pane);
        }

        void FocusFirstSnapshot()
        {
            TransitMode(m_UIState.diffMode.modeFirst);
            if (m_UIState.currentMode.currentViewPane == null)
            {
                // TODO, Change this to m_UIState.currentMode.GetDefaultView(m_UIState, this) once the default for diff is treemap
                TransitPane(m_UIState.diffMode.GetDefaultView(m_UIState, this));
            }
        }

        void FocusSecondSnapshot()
        {
            TransitMode(m_UIState.diffMode.modeSecond);
            if (m_UIState.currentMode.currentViewPane == null)
            {
                // TODO, Change this to m_UIState.currentMode.GetDefaultView(m_UIState, this) once the default for diff is treemap
                TransitPane(m_UIState.diffMode.GetDefaultView(m_UIState, this));
            }
        }

        void BackToSnapshotDiffView()
        {
            TransitMode(m_UIState.diffMode);
            if (m_UIState.currentMode.currentViewPane == null)
            {
                TransitPane(m_UIState.currentMode.GetDefaultView(m_UIState, this));
            }
        }

        void DiffCaptureWithCurrent(PackedMemorySnapshot snapshot)
        {
            bool diffedAsFirst;
            DiffCaptureWithCurrent(snapshot, out diffedAsFirst);
        }

        void DiffCaptureWithCurrent(PackedMemorySnapshot snapshot, out bool diffedAsFirst)
        {
            diffedAsFirst = true;
            if (snapshot != null)
            {
                if (m_UIState.snapshotMode.m_RawSnapshot.recordDate <= snapshot.recordDate)
                {
                    m_UIState.DiffSnapshot(m_UIState.snapshotMode.m_RawSnapshot, snapshot);
                    diffedAsFirst = false;
                }
                else
                {
                    m_UIState.DiffSnapshot(snapshot, m_UIState.snapshotMode.m_RawSnapshot);
                }

                TransitPane(m_UIState.currentMode.GetDefaultView(m_UIState, this));
            }
        }

        void DelayedSnapshot()
        {
            string baseDir = Application.dataPath.Replace("/Assets", k_SnapshotsFolderName);

            System.IO.Directory.CreateDirectory(baseDir);

            string basePath = baseDir + k_SnapshotFileNamePart + snapshotCounter.ToString();

            snapshotCounter++;

            UnityMemoryProfiler.TakeSnapshot(basePath + k_SnapshotFileExtension, SnapshotFinished, (CaptureFlags)((uint)m_CaptureFlags));

            Application.onBeforeRender -= DelayedSnapshot;
            EditorApplication.update -= DelayedSnapshot;
        }

        void TakeCapture()
        {
            ClearCurrentlyOpenedCapturesUIData();
            SetSnapshot(null);
            GC.Collect();

            if (Application.isPlaying)
            {
                //TODO: temporary workaround to not hang unity if gameview isn't selected
                System.Reflection.Assembly assembly = typeof(EditorWindow).Assembly;
                Type type = assembly.GetType("UnityEditor.GameView");
                GetWindow(type).Focus();

                Application.onBeforeRender += DelayedSnapshot;
            }
            else
            {
                EditorApplication.update += DelayedSnapshot;
            }
        }

        void LoadCapture()
        {
            ClearCurrentlyOpenedCapturesUIData();
            PackedMemorySnapshot snapshot;
            m_FirstOrOnlySnapshotLoaded = Workbench.CurrentCollection.Collection.LoadCapture(out snapshot);
            EditorUtility.SetDirty(Workbench);

            OpenCapture(snapshot);

            GUIUtility.ExitGUI();
        }

        void OpenCapture(PackedMemorySnapshot snapshot)
        {
            if (snapshot != null)
            {
                SetSnapshot(snapshot);
                TransitPane(m_UIState.currentMode.GetDefaultView(m_UIState, this));
            }
        }

        void DrawTableSelection()
        {
            using (new EditorGUI.DisabledGroupScope(m_UIState.currentMode == null))
            {
                var dropdownContent = Content.NoneView;
                if (m_UIState.currentMode != null && m_UIState.currentMode.currentViewPane != null)
                {
                    var currentViewPane = m_UIState.currentMode.currentViewPane;
                    if (currentViewPane is UI.TreeMapPane)
                    {
                        dropdownContent = Content.TreeMapView;
                    }
                    else if (currentViewPane is UI.MemoryMapPane)
                    {
                        dropdownContent = Content.MemoryMapView;
                    }
                    else if (currentViewPane is UI.SpreadsheetPane)
                    {
                        dropdownContent = ConvertTableNameForUI((currentViewPane as UI.SpreadsheetPane).m_Spreadsheet.sourceTable.GetDisplayName(), false);
                    }
                }
                Rect viewDropdownRect = GUILayoutUtility.GetRect(s_ViewDropdownSize.x, s_ViewDropdownSize.y, Styles.ToolbarPopup);
                if (EditorGUI.DropdownButton(viewDropdownRect, dropdownContent, FocusType.Passive, Styles.ToolbarPopup))
                {
                    int curTableIndex = -1;
                    if (currentViewPane is UI.SpreadsheetPane)
                    {
                        UI.SpreadsheetPane pane = (UI.SpreadsheetPane)currentViewPane;
                        curTableIndex = pane.m_CurrentTableIndex;
                    }

                    GenericMenu menu = new GenericMenu();

                    if (m_UIState.currentMode is UI.UIState.SnapshotMode)
                    {
                        menu.AddItem(Content.TreeMapView, m_UIState.currentMode.currentViewPane is UI.TreeMapPane, () =>
                        {
                            AddCurrentHistoryEvent();
                            OpenTreeMap(null);
                        });
                        menu.AddItem(Content.MemoryMapView, m_UIState.currentMode.currentViewPane is UI.MemoryMapPane, () =>
                        {
                            AddCurrentHistoryEvent();
                            OpenMemoryMap();
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(Content.TreeMapView);
                        menu.AddDisabledItem(Content.MemoryMapView);
                    }

                    if (m_UIState.currentMode != null)
                    {
                        // skip "none"
                        int numberOfTabelsToSkip = 1;

                        for (int i = numberOfTabelsToSkip; i < m_UIState.currentMode.m_TableNames.Length; i++)
                        {
                            int newTableIndex = i;
                            menu.AddItem(ConvertTableNameForUI(m_UIState.currentMode.m_TableNames[i]), newTableIndex == curTableIndex, () =>
                            {
                                var tab = m_UIState.currentMode.GetTableByIndex(newTableIndex - numberOfTabelsToSkip);
                                if (tab.ComputeRowCount())
                                {
                                    m_UIState.currentMode.UpdateTableSelectionNames();
                                }
                                AddCurrentHistoryEvent();

                                OpenTable(new Database.TableLink(tab.GetName()), tab);
                            });
                        }
                    }

                    menu.DropDown(viewDropdownRect);
                }
                GUILayout.Space(20);

                EditorGUI.BeginDisabledGroup(!m_UIState.history.hasPast && !m_UIState.history.hasFuture);
                GUILayout.Label(Content.History, EditorStyles.toolbarButton);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!m_UIState.history.hasPast);
                if (GUILayout.Button(Content.BackwardsInHistory, EditorStyles.toolbarButton))
                {
                    StepBackwardsInHistory();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!m_UIState.history.hasFuture);
                if (GUILayout.Button(Content.ForwardsInHistory, EditorStyles.toolbarButton))
                {
                    StepForwardsInHistory();
                }
                EditorGUI.EndDisabledGroup();

                // Load View
                EditorGUI.BeginDisabledGroup(!(m_UIState.currentMode is UI.UIState.SnapshotMode));
                if (GUILayout.Button(Content.LoadViewFile, EditorStyles.toolbarButton))
                {
                    string viewFile = EditorUtility.OpenFilePanel(Content.LoadViewFilePanelText, "", k_ViewFileExtension);
                    if (viewFile.Length != 0)
                    {
                        if (m_UIState.LoadView(viewFile))
                        {
                            ShowNothing();
                        }
                    }
                    GUIUtility.ExitGUI();
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
            }
        }

        private GUIContent ConvertTableNameForUI(string tableName, bool fullPath = true)
        {
            if (!m_UIFriendlyViewOptionNames.ContainsKey(tableName))
            {
                m_TabelNameStringBuilder.Length = 0;
                m_TabelNameStringBuilder.Append(Content.TableMapViewRoot.text);
                m_TabelNameStringBuilder.Append(tableName);
                m_TabelNameStringBuilder.Replace(k_RawCategoryName, Content.RawDataTableMapViewRoot.text, Content.TableMapViewRoot.text.Length, k_RawCategoryName.Length);
                string name = m_TabelNameStringBuilder.ToString();
                m_UIFriendlyViewOptionNamesWithFullPath[tableName] = new GUIContent(name);

                int lastSlash = name.LastIndexOf('/');
                if (lastSlash >= 0 && lastSlash + 1 < name.Length)
                    name = name.Substring(lastSlash + 1);
                m_UIFriendlyViewOptionNames[tableName] = new GUIContent(name);

                Vector2 potentialViewDropdownSize = Styles.ToolbarPopup.CalcSize(m_UIFriendlyViewOptionNames[tableName]);
                if (s_ViewDropdownSize.x < potentialViewDropdownSize.x)
                {
                    s_ViewDropdownSize = potentialViewDropdownSize;
                }
            }

            return fullPath ? m_UIFriendlyViewOptionNamesWithFullPath[tableName] : m_UIFriendlyViewOptionNames[tableName];
        }

        void DrawWorkbench(Rect workbenchSidebarRect)
        {
            GUILayout.BeginArea(workbenchSidebarRect);
            {
                GUI.Box(workbenchSidebarRect, GUIContent.none);

                DrawCollections();
                DrawSnapshotsList(workbenchSidebarRect);
            }
            EditorUtility.SetDirty(Workbench);
            GUILayout.EndArea();
        }

        void DrawCollections()
        {
            GUILayout.BeginHorizontal(Styles.HeaderBar);
            {
                GUILayout.Label(Content.CollectionsHeader, Styles.HeaderLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            using (var collectionIterator = Workbench.GetEnumerator())
            {
                while (collectionIterator.MoveNext())
                {
                    GUILayout.BeginHorizontal();
                    {
                        bool isCurrentlyOpenedCollection = ((MemoryProfilerWorkbench.CollectionUIDataIterator)collectionIterator).IsCurrentlyOpenedCollection;
                        bool isCollectionInRenameMode = ((MemoryProfilerWorkbench.CollectionUIDataIterator)collectionIterator).IsCollectionInRenameMode;
                        if (isCollectionInRenameMode)
                        {
                            bool done = GUILayout.Button(Content.ExitCollectionRename, Styles.ExitCollectionRename);
                            done = done || (Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Return));

                            collectionIterator.Current.Name.text = GUILayout.TextField(collectionIterator.Current.Name.text, Styles.CollectionRenameFieldOptions);
                            int returnIndex = collectionIterator.Current.Name.text.IndexOf('\n');
                            if (returnIndex > 0)
                            {
                                collectionIterator.Current.Name.text = collectionIterator.Current.Name.text.Substring(0, returnIndex);
                                done = true;
                            }
                            if (done)
                            {
                                ((MemoryProfilerWorkbench.CollectionUIDataIterator)collectionIterator).ExitRenameMode();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(collectionIterator.Current.Name, isCurrentlyOpenedCollection ? Styles.CollectionLabelSelected : Styles.WorkbenchLabel))
                            {
                                if (isCurrentlyOpenedCollection)
                                    ((MemoryProfilerWorkbench.CollectionUIDataIterator)collectionIterator).EnterRenameMode();
                                else
                                    ((MemoryProfilerWorkbench.CollectionUIDataIterator)collectionIterator).OpenCurrentCollection();
                            }
                        }
                        GUILayout.FlexibleSpace();
                        if (collectionIterator.Current.IsTemporary && GUILayout.Button(Content.SaveCollection) /*&& !isCurrentlyOpenedCollection*/)
                        {
                            MemoryProfilerWorkbench.SaveCollection(collectionIterator.Current);
                        }
                        if (GUILayout.Button(Content.UnloadCollection /*, Styles.CollectionLabel*/))
                        {
                            ((MemoryProfilerWorkbench.CollectionUIDataIterator)collectionIterator).UnloadCurrentCollection();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(Styles.SnapshotButtonMargin);
                if (GUILayout.Button(Content.NewCollection))
                    Workbench.CreateTemporaryCollection();
                if (GUILayout.Button(Content.LoadCollection))
                    Workbench.LoadCollection();
                GUILayout.Space(Styles.SnapshotButtonMargin);
            }
            GUILayout.EndHorizontal();
        }

        void DrawSnapshotsList(Rect workbenchSidebarRect)
        {
            var snapshotsListRect = workbenchSidebarRect;
            var spacer = GUILayoutUtility.GetRect(Content.SnapshotsHeader, Styles.HeaderBar);
            spacer.y += spacer.height;
            snapshotsListRect.y = spacer.yMax;
            snapshotsListRect.height = workbenchSidebarRect.height - snapshotsListRect.y;
            var headerBarHeight = Styles.HeaderBar.CalcSize(Content.SnapshotsHeader).y;
            GUILayout.BeginHorizontal(Styles.HeaderBar);
            {
                GUILayout.Label(Content.SnapshotsHeader, Styles.HeaderLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Content.CaptureData, Styles.HeaderButton))
                {
                    TakeCapture();
                }
                if (GUILayout.Button(Content.LoadCapture, Styles.HeaderButton))
                {
                    LoadCapture();
                }
            }
            GUILayout.EndHorizontal();
            var scrollAreaPosition = new Vector2(snapshotsListRect.x, snapshotsListRect.y);
            var heightNeededForScrollList = Styles.SnapshotPreviewImageHeight * Workbench.CurrentCollection.Collection.SnapshotCount;
            var scrollbarWidth = EditorGUIUtility.singleLineHeight;
            var scrollAreaHeight = Mathf.Min(snapshotsListRect.height, heightNeededForScrollList);
            bool showScrollbar = heightNeededForScrollList > scrollAreaHeight;
            var scrollAreaWidth = Styles.WorkbenchDefaultWidth - (showScrollbar ? scrollbarWidth : 0);
            var scrollAreaRect = new Rect(scrollAreaPosition, new Vector2(scrollAreaWidth, scrollAreaHeight));
            GUI.BeginClip(scrollAreaRect);
            scrollAreaRect.position = new Vector2(0, -m_SnapshotListScrollViewPosition.y);
            using (var collectionIterator = Workbench.CurrentCollection.Collection.GetEnumerator())
            {
                bool diffmode = m_SecondSnapshotLoaded != null;
                bool oneSnapshotIsLoaded = !diffmode && m_FirstOrOnlySnapshotLoaded != null;

                var snapshotListItemRect = new Rect(scrollAreaRect.position, new Vector2(scrollAreaRect.width, Styles.SnapshotPreviewImageHeight));
                while (collectionIterator.MoveNext())
                {
                    GUIStyle boxStyle = Styles.SnapshotListItemBox;
                    if (oneSnapshotIsLoaded && collectionIterator.Current == m_FirstOrOnlySnapshotLoaded)
                    {
                        boxStyle = Styles.SnapshotListItemBoxLoaded;
                    }
                    bool isFirst = collectionIterator.Current == m_FirstOrOnlySnapshotLoaded;
                    bool isSecond = collectionIterator.Current == m_SecondSnapshotLoaded;
                    if (diffmode)
                    {
                        if (m_UIState.currentMode == m_UIState.diffMode.modeFirst)
                        {
                            //TODO: Differentiate visually
                            if (isFirst)
                                boxStyle = Styles.SnapshotListItemBoxLoadedDiffActive;
                            else if (isSecond)
                                boxStyle = Styles.SnapshotListItemBoxLoadedDiffInactive;
                        }
                        else if (m_UIState.currentMode == m_UIState.diffMode.modeSecond)
                        {
                            if (isFirst)
                                boxStyle = Styles.SnapshotListItemBoxLoadedDiffInactive;
                            else if (isSecond)
                                boxStyle = Styles.SnapshotListItemBoxLoadedDiffActive;
                        }
                        else if (m_UIState.currentMode == m_UIState.diffMode && (isFirst || isSecond))
                        {
                            boxStyle = Styles.SnapshotListItemBoxLoadedDiff;
                        }
                    }
                    GUI.Box(snapshotListItemRect, GUIContent.none, boxStyle);

                    var previewImageRect = new Rect(snapshotListItemRect.x, snapshotListItemRect.y, Styles.SnapshotPreviewImageWidth, Styles.SnapshotPreviewImageHeight);
                    GUI.Box(previewImageRect, collectionIterator.Current.PreviewImage);

                    var unloadButtonSize = Styles.SnapshotListItemButtons.CalcSize(Content.UnloadSnapshot);
                    var openButtonSize = Styles.SnapshotListItemButtons.CalcSize(Content.OpenSnapshot);
                    var diffButtonSize = Styles.SnapshotListItemButtons.CalcSize(Content.DiffSnapshot);

                    var maxButonWidth = Mathf.Max(unloadButtonSize.x, openButtonSize.x, diffButtonSize.x);
                    openButtonSize.x = maxButonWidth;
                    diffButtonSize.x = maxButonWidth;

                    var unloadButtonRect = new Rect(new Vector2(snapshotListItemRect.width - unloadButtonSize.x - Styles.RightMargin, snapshotListItemRect.y), unloadButtonSize);
                    var openButtonRect = new Rect(new Vector2(snapshotListItemRect.width - openButtonSize.x - Styles.RightMargin, snapshotListItemRect.y + EditorGUIUtility.singleLineHeight), openButtonSize);
                    var diffButtonRect = new Rect(new Vector2(snapshotListItemRect.width - diffButtonSize.x - Styles.RightMargin, snapshotListItemRect.y + EditorGUIUtility.singleLineHeight * 2), diffButtonSize);

                    var switchDiffButtonSize = Styles.SnapshotListDiffToggle.CalcSize(Content.SwitchDiffFocus);
                    var switchDiffButtonRect = new Rect(new Vector2(diffButtonRect.xMin - switchDiffButtonSize.x - Styles.RightMargin, snapshotListItemRect.y + EditorGUIUtility.singleLineHeight * 2), switchDiffButtonSize);

                    var labelRect = new Rect(previewImageRect.xMax, previewImageRect.y, snapshotListItemRect.width - (maxButonWidth + previewImageRect.width), EditorGUIUtility.singleLineHeight);
                    {
                        GUI.Label(labelRect, collectionIterator.Current.Name);
                        labelRect.y += labelRect.height;
                        GUI.Label(labelRect, collectionIterator.Current.MetaInfo);
                        labelRect.y += labelRect.height;
                        GUI.Label(labelRect, collectionIterator.Current.FileSize);
                    }
                    {
                        if (GUI.Button(unloadButtonRect, Content.UnloadSnapshot, Styles.SnapshotListItemButtons))
                            ((SnapshotCollection.SnapshotUIData.Enumerator)collectionIterator).UnloadCurrent();

                        bool guiWasEnabled = GUI.enabled;
                        bool currentSnapshotIsLoaded = isFirst || isSecond;
                        GUI.enabled = !currentSnapshotIsLoaded;
                        if (GUI.Button(openButtonRect, Content.OpenSnapshot, Styles.SnapshotListItemButtons))
                        {
                            ClearCurrentlyOpenedCapturesUIData();
                            PackedMemorySnapshot snapshot;
                            m_FirstOrOnlySnapshotLoaded = ((SnapshotCollection.SnapshotUIData.Enumerator)collectionIterator).OpenCurrent(out snapshot);
                            OpenCapture(snapshot);
                            collectionIterator.Current.CurrentState = SnapshotCollection.SnapshotUIData.State.Open;
                        }
                        GUI.enabled = guiWasEnabled;

                        if (diffmode)
                        {
                            if (isFirst)
                            {
                                bool wasFocused = m_UIState.currentMode == m_UIState.diffMode.modeFirst;
                                bool isFocused = GUI.Toggle(switchDiffButtonRect, wasFocused, Content.SwitchDiffFocus, Styles.SnapshotListDiffToggle);
                                if (wasFocused != isFocused)
                                {
                                    if (isFocused)
                                        FocusFirstSnapshot();
                                    else
                                        BackToSnapshotDiffView();
                                }
                            }
                            else if (isSecond)
                            {
                                bool wasFocused = m_UIState.currentMode == m_UIState.diffMode.modeSecond;
                                bool isFocused = GUI.Toggle(switchDiffButtonRect, wasFocused, Content.SwitchDiffFocus, Styles.SnapshotListDiffToggle);
                                if (wasFocused != isFocused)
                                {
                                    if (isFocused)
                                        FocusSecondSnapshot();
                                    else
                                        BackToSnapshotDiffView();
                                }
                            }
                        }
                        GUI.enabled = oneSnapshotIsLoaded && !currentSnapshotIsLoaded;
                        if (GUI.Button(diffButtonRect, Content.DiffSnapshot, Styles.SnapshotListItemButtons))
                        {
                            bool diffedAsFirst;
                            PackedMemorySnapshot snapshot;
                            var snapshotUI = ((SnapshotCollection.SnapshotUIData.Enumerator)collectionIterator).OpenCurrent(out snapshot);
                            DiffCaptureWithCurrent(snapshot, out diffedAsFirst);
                            if (diffedAsFirst)
                            {
                                m_SecondSnapshotLoaded = m_FirstOrOnlySnapshotLoaded;
                                m_FirstOrOnlySnapshotLoaded = snapshotUI;
                            }
                            else
                            {
                                m_SecondSnapshotLoaded = snapshotUI;
                            }
                            m_FirstOrOnlySnapshotLoaded.CurrentState = SnapshotCollection.SnapshotUIData.State.OpenInDiffAsFirst;
                            m_SecondSnapshotLoaded.CurrentState = SnapshotCollection.SnapshotUIData.State.OpenInDiffAsSecond;
                        }
                        GUI.enabled = guiWasEnabled;
                    }
                    snapshotListItemRect.y += snapshotListItemRect.height;
                }
            }
            GUI.EndClip();
            if (showScrollbar)
            {
                var scrollbarRect = new Rect(scrollAreaPosition.x + scrollAreaRect.width, scrollAreaPosition.y, scrollbarWidth, scrollAreaRect.height);
                m_SnapshotListScrollViewPosition.y = GUI.VerticalScrollbar(scrollbarRect, m_SnapshotListScrollViewPosition.y, scrollAreaHeight, 0, heightNeededForScrollList);
            }
        }

        void ClearCurrentlyOpenedCapturesUIData()
        {
            if (m_FirstOrOnlySnapshotLoaded != null)
            {
                m_FirstOrOnlySnapshotLoaded.CurrentState = SnapshotCollection.SnapshotUIData.State.Listed;
                m_FirstOrOnlySnapshotLoaded = null;
            }
            if (m_SecondSnapshotLoaded != null)
            {
                m_SecondSnapshotLoaded.CurrentState = SnapshotCollection.SnapshotUIData.State.Listed;
                m_SecondSnapshotLoaded = null;
            }
        }

        void OnGUI()
        {
            try
            {
                using (new Service<IDebugContextService>.ScopeService(new DebugContextService()))
                {
                    if (Event.current.type == EventType.Layout)
                    {
                        if (m_EventToOpen != null)
                        {
                            OpenHistoryEvent(m_EventToOpen);
                            m_EventToOpen = null;
                        }
                        if (currentViewPane != null)
                        {
                            currentViewPane.OnPreGUI();
                        }
                    }

                    if (s_ViewDropdownSize.y == 0)
                        s_ViewDropdownSize = Styles.ToolbarPopup.CalcSize(Content.NoneView);
                    var viewRect = position;
                    viewRect.x = viewRect.y = 0;

                    EditorGUILayout.BeginHorizontal();
                    {
                        var workbenchSidebarRect = viewRect;
                        workbenchSidebarRect.width = Styles.WorkbenchDefaultWidth;
                        DrawWorkbench(workbenchSidebarRect);

                        EditorGUILayout.BeginVertical();
                        {
                            var mainViewRect = viewRect;
                            mainViewRect.xMin += Styles.WorkbenchDefaultWidth;
                            var headerRect = mainViewRect;
                            headerRect.height = EditorStyles.toolbar.CalcHeight(Content.CaptureData, 10) * Styles.ViewPaneMargin;
                            GUILayout.BeginArea(headerRect);
                            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                            {
                                DrawTableSelection();
                                GUILayout.FlexibleSpace();
                            }
                            EditorGUILayout.EndHorizontal();
                            GUILayout.EndArea();

                            var viewPaneRect = new Rect(mainViewRect.x + Styles.ViewPaneMargin, mainViewRect.y + headerRect.height, mainViewRect.width - Styles.ViewPaneMargin * 2, mainViewRect.height - headerRect.height - Styles.ViewPaneMargin);
                            if (currentViewPane != null)
                            {
                                currentViewPane.OnGUI(viewPaneRect);
                            }
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new Exception(DebugUtility.GetExceptionHelpMessage(e));
            }
        }
    }
}