using Unity.MemoryProfiler.Editor.Debuging;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;

namespace Unity.MemoryProfiler.Editor.UI
{
    public class UIState
    {
        public abstract class BaseMode
        {
            public string[] m_TableNames = { "none" };
            public Database.Table[] m_Tables = { null };

            public UI.ViewPane currentViewPane;

            public abstract ViewPane GetDefaultView(UIState uiState, IViewPaneEventListener viewPaneEventListener);

            public int GetTableIndex(Database.Table tab)
            {
                int index = System.Array.FindIndex(m_Tables, x => x == tab);
                return index;
            }

            public abstract Database.Table GetTableByIndex(int index);
            public abstract void UpdateTableSelectionNames();

            public virtual UI.HistoryEvent GetCurrentHistoryEvent()
            {
                if (currentViewPane == null) return null;
                return currentViewPane.GetCurrentHistoryEvent();
            }

            public abstract Database.Scheme GetSheme();
            protected void UpdateTableSelectionNamesFromScheme(DataRenderer dataRenderer, Database.Scheme scheme)
            {
                if (scheme == null)
                {
                    m_TableNames = new string[1];
                    m_TableNames[0] = "none";
                    m_Tables = new Database.Table[1];
                    m_Tables[0] = null;
                    return;
                }
                m_TableNames = new string[scheme.GetTableCount() + 1];
                m_Tables = new Database.Table[scheme.GetTableCount() + 1];
                m_TableNames[0] = "none";
                m_Tables[0] = null;
                for (long i = 0; i != scheme.GetTableCount(); ++i)
                {
                    var tab = scheme.GetTableByIndex(i);
                    tab.Update(); //update table internal data
                    long rowCount = tab.GetRowCount();
                    m_TableNames[i + 1] = (dataRenderer.showPrettyNames ? tab.GetDisplayName() : tab.GetName()) + " (" + (rowCount >= 0 ? rowCount.ToString() : "?") + ")";
                    m_Tables[i + 1] = tab;
                }
            }
        }

        public class SnapshotMode : BaseMode
        {
            public PackedMemorySnapshot m_RawSnapshot;
            public RawScheme m_RawScheme;
            public Database.View.ViewScheme m_ViewScheme;
            public Database.Scheme m_SchemeToDisplay;
            public CachedSnapshot snapshot { get { return m_RawScheme.m_Snapshot; } }
            public SnapshotMode(UIState uiState, PackedMemorySnapshot snapshot)
            {
                SetSnapshot(uiState, snapshot);
            }

            public override Database.Scheme GetSheme()
            {
                return m_SchemeToDisplay;
            }

            public void SetSnapshot(UIState uiState, PackedMemorySnapshot snapshot)
            {
                if (snapshot == null)
                {
                    m_RawSnapshot = null;
                    m_RawScheme = null;
                    m_SchemeToDisplay = null;
                    UpdateTableSelectionNames();
                    return;
                }
                m_RawSnapshot = snapshot;
                m_RawScheme = new RawScheme(snapshot, uiState.m_DataRenderer);
                m_SchemeToDisplay = m_RawScheme;
                if (k_DefaultViewFilePath.Length > 0)
                {
                    using (ScopeDebugContext.Func(() => { return "File '" + k_DefaultViewFilePath + "'"; }))
                    {
                        var builder = Database.View.ViewScheme.Builder.LoadFromXMLFile(k_DefaultViewFilePath);

                        if (builder != null)
                        {
                            m_ViewScheme = builder.Build(m_RawScheme);
                            if (m_ViewScheme != null)
                            {
                                m_SchemeToDisplay = m_ViewScheme;
                            }
                        }
                    }
                }

                UpdateTableSelectionNames();
            }

            public override Database.Table GetTableByIndex(int index)
            {
                return m_SchemeToDisplay.GetTableByIndex(index);
            }

            public override void UpdateTableSelectionNames()
            {
                if (m_RawScheme != null)
                {
                    UpdateTableSelectionNamesFromScheme(m_RawScheme.renderer.m_BaseRenderer, m_SchemeToDisplay);
                }
            }

            public override ViewPane GetDefaultView(UIState uiState, IViewPaneEventListener viewPaneEventListener)
            {
                return new UI.TreeMapPane(uiState, viewPaneEventListener);
            }
        }
        public class DiffMode : BaseMode
        {
            public BaseMode modeFirst;
            public BaseMode modeSecond;
            public Database.Scheme m_SchemeFirst;
            public Database.Scheme m_SchemeSecond;
            public Database.Operation.DiffScheme m_SchemeDiff;
            public UIState m_UIState;

            private const string k_DefaultDiffViewTable = "All Object";
            public DiffMode(UIState uiState, PackedMemorySnapshot snapshotFirst, PackedMemorySnapshot snapshotSecond)
            {
                m_UIState = uiState;
                modeFirst = new SnapshotMode(uiState, snapshotFirst);
                modeSecond = new SnapshotMode(uiState, snapshotSecond);
                m_SchemeFirst = modeFirst.GetSheme();
                m_SchemeSecond = modeSecond.GetSheme();

                m_SchemeDiff = new Database.Operation.DiffScheme(m_SchemeFirst, m_SchemeSecond);
                UpdateTableSelectionNames();
            }

            public override Database.Scheme GetSheme()
            {
                return m_SchemeDiff;
            }

            public override Database.Table GetTableByIndex(int index)
            {
                return m_SchemeDiff.GetTableByIndex(index);
            }

            public override void UpdateTableSelectionNames()
            {
                UpdateTableSelectionNamesFromScheme(m_UIState.m_DataRenderer, m_SchemeDiff);
            }

            public override ViewPane GetDefaultView(UIState uiState, IViewPaneEventListener viewPaneEventListener)
            {
                Database.Table table = null;
                for (int i = 1; i < uiState.currentMode.m_TableNames.Length; i++)
                {
                    if (uiState.currentMode.m_TableNames[i].Contains(k_DefaultDiffViewTable))
                    {
                        table = uiState.currentMode.GetTableByIndex(i - 1);
                    }
                }
                if (table == null)
                    table = uiState.currentMode.GetTableByIndex(Mathf.Min(0, m_TableNames.Length - 1));

                var pane = new UI.SpreadsheetPane(uiState, viewPaneEventListener);
                pane.OpenTable(new Database.TableLink(table.GetName()), table);
                return pane;
            }
        }

        const string k_DefaultViewFilePath = "Packages/com.unity.memoryprofiler/Resources/MemView.xml";

        public UI.History history = new UI.History();

        public BaseMode currentMode;
        public SnapshotMode snapshotMode { get { return currentMode as SnapshotMode; } }
        public DiffMode diffMode;

        public DefaultHotKey m_HotKey = new DefaultHotKey();
        public DataRenderer m_DataRenderer = new DataRenderer();


        public void AddHistoryEvent(UI.HistoryEvent he)
        {
            if (he != null)
            {
                history.AddEvent(he);
            }
        }

        public void SetSnapshot(UnityEditor.Profiling.Memory.Experimental.PackedMemorySnapshot snapshot)
        {
            history.Clear();
            currentMode =  new SnapshotMode(this, snapshot);
        }

        public void DiffSnapshot(UnityEditor.Profiling.Memory.Experimental.PackedMemorySnapshot snapshotA, PackedMemorySnapshot snapshotB)
        {
            history.Clear();
            currentMode = diffMode = new DiffMode(this, snapshotA, snapshotB);
        }

        public bool LoadView(string filename)
        {
            history.Clear();
            if (snapshotMode == null)
            {
                DebugUtility.LogWarning("Must open a snapshot before loading a view file");
                return false;
            }
            if (filename.Length != 0)
            {
                using (ScopeDebugContext.Func(() => { return "File '" + filename + "'"; }))
                {
                    var builder = Database.View.ViewScheme.Builder.LoadFromXMLFile(filename);
                    if (builder != null)
                    {
                        snapshotMode.m_ViewScheme = builder.Build(snapshotMode.m_RawScheme);
                        if (snapshotMode.m_ViewScheme != null)
                        {
                            snapshotMode.m_SchemeToDisplay = snapshotMode.m_ViewScheme;
                            snapshotMode.UpdateTableSelectionNames();
                            history.Clear();
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
