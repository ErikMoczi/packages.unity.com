using System;
using Unity.MemoryProfiler.Editor.Database;
using Unity.MemoryProfiler.Editor.Debuging;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;

namespace Unity.MemoryProfiler.Editor.UI
{
    internal class UIState
    {
        internal abstract class BaseMode
        {
            public string [] TableNames
            {
                get
                {
                    return m_TableNames;
                }
            }

            protected string[] m_TableNames = { "none" };
            Database.Table[] m_Tables = { null };
            
            public event Action<ViewPane> ViewPaneChanged = delegate { };

            public ViewPane CurrentViewPane { get; private set; }

            public abstract ViewPane GetDefaultView(UIState uiState, IViewPaneEventListener viewPaneEventListener);

            public int GetTableIndex(Database.Table tab)
            {
                int index = Array.FindIndex(m_Tables, x => x == tab);
                return index;
            }

            public abstract Database.Table GetTableByIndex(int index);
            public abstract void UpdateTableSelectionNames();

            public virtual HistoryEvent GetCurrentHistoryEvent()
            {
                if (CurrentViewPane == null) return null;
                return CurrentViewPane.GetCurrentHistoryEvent();
            }

            public void TransitPane(ViewPane newPane)
            {
                if (CurrentViewPane != newPane && CurrentViewPane != null)
                {
                    CurrentViewPane.OnClose();
                }
                CurrentViewPane = newPane;
                ViewPaneChanged(newPane);
            }

            public abstract Database.Schema GetSchema();
            protected void UpdateTableSelectionNamesFromSchema(DataRenderer dataRenderer, Database.Schema schema)
            {
                if (schema == null)
                {
                    m_TableNames = new string[1];
                    m_TableNames[0] = "none";
                    m_Tables = new Database.Table[1];
                    m_Tables[0] = null;
                    return;
                }
                m_TableNames = new string[schema.GetTableCount() + 1];
                m_Tables = new Database.Table[schema.GetTableCount() + 1];
                m_TableNames[0] = "none";
                m_Tables[0] = null;
                for (long i = 0; i != schema.GetTableCount(); ++i)
                {
                    var tab = schema.GetTableByIndex(i);
                    tab.Update(); //update table internal data
                    long rowCount = tab.GetRowCount();
                    m_TableNames[i + 1] = (dataRenderer.ShowPrettyNames ? tab.GetDisplayName() : tab.GetName()) + " (" + (rowCount >= 0 ? rowCount.ToString() : "?") + ")";
                    m_Tables[i + 1] = tab;
                }
            }

            public abstract void Clear();
        }

        internal class SnapshotMode : BaseMode
        {
            PackedMemorySnapshot m_RawSnapshot;
            RawSchema m_RawSchema;

            public RawSchema RawSchema
            {
                get
                {
                    return m_RawSchema;
                }
            }

            public Database.View.ViewSchema ViewSchema;
            public Database.Schema SchemaToDisplay;
            public CachedSnapshot snapshot {
                get
                {
                    if (m_RawSchema == null)
                        return null;
                    return m_RawSchema.m_Snapshot;
                }
            }
            public SnapshotMode(DataRenderer dataRenderer, PackedMemorySnapshot snapshot)
            {
                dataRenderer.PrettyNamesOptionChanged += UpdateTableSelectionNames;
                SetSnapshot(dataRenderer, snapshot);
            }

            public override Database.Schema GetSchema()
            {
                return SchemaToDisplay;
            }

            void SetSnapshot(DataRenderer dataRenderer, PackedMemorySnapshot snapshot)
            {
                if (snapshot == null)
                {
                    m_RawSnapshot = null;
                    m_RawSchema = null;
                    SchemaToDisplay = null;
                    UpdateTableSelectionNames();
                    return;
                }
                m_RawSnapshot = snapshot;
                using (Profiling.GetMarker(Profiling.MarkerId.CreateSnapshotSchema).Auto())
                {
                     m_RawSchema = new RawSchema(snapshot, dataRenderer);
                }
                SchemaToDisplay = m_RawSchema;
                if (k_DefaultViewFilePath.Length > 0)
                {
                    using (ScopeDebugContext.Func(() => { return "File '" + k_DefaultViewFilePath + "'"; }))
                    {
                        Database.View.ViewSchema.Builder builder = null;
                        using (Profiling.GetMarker(Profiling.MarkerId.LoadViewDefinitionFile).Auto())
                        {
                             builder = Database.View.ViewSchema.Builder.LoadFromXMLFile(k_DefaultViewFilePath);
                        }
                        if (builder != null)
                        {
                            using (Profiling.GetMarker(Profiling.MarkerId.BuildViewDefinitionFile).Auto())
                            {
                                 ViewSchema = builder.Build(m_RawSchema);
                            }
                            if (ViewSchema != null)
                            {
                                SchemaToDisplay = ViewSchema;
                            }
                        }
                    }
                }

                UpdateTableSelectionNames();
            }

            public override Database.Table GetTableByIndex(int index)
            {
                return SchemaToDisplay.GetTableByIndex(index);
            }

            public override void UpdateTableSelectionNames()
            {
                if (m_RawSchema != null)
                {
                    UpdateTableSelectionNamesFromSchema(m_RawSchema.renderer.m_BaseRenderer, SchemaToDisplay);
                }
            }

            public override ViewPane GetDefaultView(UIState uiState, IViewPaneEventListener viewPaneEventListener)
            {
                if (uiState.snapshotMode != null && uiState.snapshotMode.snapshot != null)
                    return new UI.TreeMapPane(uiState, viewPaneEventListener);
                else
                    return null;
            }

            public override void Clear()
            {
                CurrentViewPane.OnClose();
                SchemaToDisplay = null;
                m_RawSchema.Clear();
                m_RawSchema = null;
                if (m_RawSnapshot != null)
                    m_RawSnapshot.Dispose();
            }
        }
        internal class DiffMode : BaseMode
        {
            public BaseMode modeFirst;
            public BaseMode modeSecond;
            Database.Schema m_SchemaFirst;
            Database.Schema m_SchemaSecond;
            Database.Operation.DiffSchema m_SchemaDiff;
            DataRenderer m_DataRenderer;

            private const string k_DefaultDiffViewTable = "All Object";
            public DiffMode(DataRenderer dataRenderer, PackedMemorySnapshot snapshotFirst, PackedMemorySnapshot snapshotSecond)
            {
                m_DataRenderer = dataRenderer;
                m_DataRenderer.PrettyNamesOptionChanged += UpdateTableSelectionNames;
                modeFirst = new SnapshotMode(dataRenderer, snapshotFirst);
                modeSecond = new SnapshotMode(dataRenderer, snapshotSecond);
                m_SchemaFirst = modeFirst.GetSchema();
                m_SchemaSecond = modeSecond.GetSchema();

                m_SchemaDiff = new Database.Operation.DiffSchema(m_SchemaFirst, m_SchemaSecond);
                UpdateTableSelectionNames();
            }

            public DiffMode(DataRenderer dataRenderer, BaseMode snapshotFirst, BaseMode snapshotSecond)
            {
                m_DataRenderer = dataRenderer;
                m_DataRenderer.PrettyNamesOptionChanged += UpdateTableSelectionNames;
                modeFirst = snapshotFirst;
                modeSecond = snapshotSecond;
                m_SchemaFirst = modeFirst.GetSchema();
                m_SchemaSecond = modeSecond.GetSchema();

                m_SchemaDiff = new Database.Operation.DiffSchema(m_SchemaFirst, m_SchemaSecond);
                UpdateTableSelectionNames();
            }

            public override Database.Schema GetSchema()
            {
                return m_SchemaDiff;
            }

            public override Database.Table GetTableByIndex(int index)
            {
                return m_SchemaDiff.GetTableByIndex(index);
            }

            public override void UpdateTableSelectionNames()
            {
                UpdateTableSelectionNamesFromSchema(m_DataRenderer, m_SchemaDiff);
            }

            public override ViewPane GetDefaultView(UIState uiState, IViewPaneEventListener viewPaneEventListener)
            {
                //TODO: delete this method once the default for diff is treemap
                Database.Table table = null;
                for (int i = 1; i < uiState.CurrentMode.TableNames.Length; i++)
                {
                    if (uiState.CurrentMode.TableNames[i].Contains(k_DefaultDiffViewTable))
                    {
                        table = uiState.CurrentMode.GetTableByIndex(i - 1);
                    }
                }
                if (table == null)
                    table = uiState.CurrentMode.GetTableByIndex(Mathf.Min(0, m_TableNames.Length - 1));

                var pane = new UI.SpreadsheetPane(uiState, viewPaneEventListener);
                pane.OpenTable(new Database.TableLink(table.GetName()), table);
                return pane;
            }

            public override void Clear()
            {
                modeFirst.Clear();
                modeSecond.Clear();
            }
        }

        const string k_DefaultViewFilePath = "Packages/com.unity.memoryprofiler/Package Resources/MemView.xml";

        public History history = new History();

        public event Action<BaseMode, ViewMode> ModeChanged = delegate { };

        public BaseMode CurrentMode
        {
            get
            {
                switch (m_CurrentViewMode)
                {
                    case ViewMode.ShowNone:
                        return noMode;
                    case ViewMode.ShowFirst:
                        return FirstMode;
                    case ViewMode.ShowSecond:
                        return SecondMode;
                    case ViewMode.ShowDiff:
                        return diffMode;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        
        public BaseMode FirstMode { get; private set; }
        public BaseMode SecondMode { get; private set; }

        public enum ViewMode
        {
            ShowNone = -1,
            ShowDiff,
            ShowFirst,
            ShowSecond,
        }
        ViewMode m_CurrentViewMode = ViewMode.ShowNone;
        public ViewMode CurrentViewMode
        {
            get
            {
                return m_CurrentViewMode;
            }
            set
            {
                if(m_CurrentViewMode != value)
                {
                    m_CurrentViewMode = value;
                    ModeChanged(CurrentMode, value);
                }
            }
        }

        public SnapshotMode snapshotMode { get { return CurrentMode as SnapshotMode; } }
        public DiffMode diffMode;

        public SnapshotMode noMode;

        public readonly DefaultHotKey HotKey = new DefaultHotKey();
        public readonly DataRenderer DataRenderer = new DataRenderer();

        public UIState()
        {
            noMode = new SnapshotMode(DataRenderer, null);
        }

        public void AddHistoryEvent(HistoryEvent he)
        {
            if (he != null)
            {
                history.AddEvent(he);
            }
        }

        public void ClearDiffMode()
        {
            diffMode = null;
            if(CurrentViewMode == ViewMode.ShowDiff)
            {
                if (FirstMode != null)
                    CurrentViewMode = ViewMode.ShowFirst;
                else if(SecondMode != null)
                    CurrentViewMode = ViewMode.ShowSecond;
                else
                    CurrentViewMode = ViewMode.ShowNone;
            }
        }

        public void ClearAllOpenModes()
        {
            if (SecondMode != null)
                SecondMode.Clear();
            SecondMode = null;
            if (FirstMode != null)
                FirstMode.Clear();
            FirstMode = null;
            CurrentViewMode = ViewMode.ShowNone;
            diffMode = null;
            history.Clear();
        }

        public void ClearFirstMode()
        {
            if (FirstMode != null)
                FirstMode.Clear();
            FirstMode = null;

            if (diffMode != null)
            {
                ClearDiffMode();
            }

            if (CurrentViewMode == ViewMode.ShowFirst)
            {
                if(SecondMode != null)
                    CurrentViewMode = ViewMode.ShowSecond;
                else
                    CurrentViewMode = ViewMode.ShowNone;
            }
        }

        public void ClearSecondMode()
        {
            if (SecondMode != null)
                SecondMode.Clear();
            SecondMode = null;

            if (diffMode != null)
            {
                ClearDiffMode();
            }

            if (CurrentViewMode == ViewMode.ShowSecond)
            {
                if (FirstMode != null)
                    CurrentViewMode = ViewMode.ShowFirst;
                else
                    CurrentViewMode = ViewMode.ShowNone;
            }
        }

        public void SetFirstSnapshot(PackedMemorySnapshot snapshot)
        {
            if(snapshot == null)
            {
                Debug.LogError("UIState.SetFirstSnapshot can't be called with null, if you meant to clear the open snapshots, call ClearAllOpenSnapshots");
                return;
            }
            history.Clear();
            if (FirstMode != null)
            {
                if (SecondMode != null)
                    SecondMode.Clear();
                SecondMode = FirstMode;
            }
            FirstMode = new SnapshotMode(DataRenderer, snapshot);

            // Make sure that the first mode is shown and that ModeChanged (fired by ShownMode if set to something different) is fired.
            if (CurrentViewMode != ViewMode.ShowFirst)
                CurrentViewMode = ViewMode.ShowFirst;
            else
                ModeChanged(CurrentMode, CurrentViewMode);
            ClearDiffMode();
        }

        public void SwapLastAndCurrentSnapshot()
        {
            // TODO: find out if we actually need to clear this or if it can be saved with the mode
            history.Clear();
            var temp = SecondMode;
            SecondMode = FirstMode;
            FirstMode = temp;
            if(CurrentViewMode != ViewMode.ShowDiff)
            {
                CurrentViewMode = CurrentViewMode == ViewMode.ShowFirst ? ViewMode.ShowSecond : ViewMode.ShowFirst;
                ModeChanged(CurrentMode, CurrentViewMode);
            }
        }

        public void DiffLastAndCurrentSnapshot(bool firstIsOlder)
        {
            history.Clear();

            diffMode = new DiffMode(DataRenderer, firstIsOlder ? FirstMode : SecondMode , firstIsOlder ? SecondMode : FirstMode);
            CurrentViewMode = ViewMode.ShowDiff;
        }

        public void DiffSnapshot(PackedMemorySnapshot snapshotA, PackedMemorySnapshot snapshotB)
        {
            history.Clear();
            diffMode = new DiffMode(DataRenderer, snapshotA, snapshotB);
            CurrentViewMode = ViewMode.ShowDiff;
        }

        public bool LoadView(string filename)
        {
            history.Clear();
            if (snapshotMode == null)
            {
                DebugUtility.LogWarning("Must open a snapshot before loading a view file");
                MemoryProfilerAnalytics.AddMetaDatatoEvent<MemoryProfilerAnalytics.LoadViewXMLEvent>(1);
                return false;
            }
            if (filename.Length != 0)
            {
                using (ScopeDebugContext.Func(() => { return "File '" + filename + "'"; }))
                {
                    var builder = Database.View.ViewSchema.Builder.LoadFromXMLFile(filename);
                    if (builder != null)
                    {
                        snapshotMode.ViewSchema = builder.Build(snapshotMode.RawSchema);
                        if (snapshotMode.ViewSchema != null)
                        {
                            snapshotMode.SchemaToDisplay = snapshotMode.ViewSchema;
                            snapshotMode.UpdateTableSelectionNames();
                            history.Clear();
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        public void TransitModeToOwningTable(Table table)
        {
            if (diffMode != null)
            {
                //open the appropriate snapshot mode, the one the table is from.
                if (diffMode.modeFirst.GetSchema().OwnsTable(table))
                {
                    TransitMode(diffMode.modeFirst);
                }
                else if (diffMode.modeSecond.GetSchema().OwnsTable(table))
                {
                    TransitMode(diffMode.modeSecond);
                }
                else if (diffMode.GetSchema().OwnsTable(table))
                {
                    TransitMode(diffMode);
                }
            }
        }

        public void TransitMode(UIState.BaseMode newMode)
        {
            if(newMode == diffMode)
            {
                CurrentViewMode = ViewMode.ShowDiff;
            }
            else if(newMode == FirstMode)
            {
                CurrentViewMode = ViewMode.ShowFirst;
            }
            else if (newMode == SecondMode)
            {
                CurrentViewMode = ViewMode.ShowSecond;
            }
            else
            {
                FirstMode = newMode;
                CurrentViewMode = ViewMode.ShowFirst;
                ModeChanged(newMode, CurrentViewMode);
            }
        }
    }
}
