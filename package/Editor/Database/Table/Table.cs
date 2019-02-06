using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.Database
{
    internal struct CellPosition
    {
        public static CellPosition invalid
        {
            get
            {
                return new CellPosition(-1, -1);
            }
        }
        public CellPosition(long row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public bool isValid()
        {
            return row >= 0;
        }

        public override string ToString()
        {
            return "[" + row + ":" + col + "]";
        }

        public long row;
        public int col;
    }
    internal class RowIndexColumn : ColumnTyped<long>
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "RowIndexColumn<long>[" + row + "]{" + row + "}";
        }

#endif
        public Database.Table table;
        public RowIndexColumn(Database.Table table)
        {
            type = typeof(long);
            this.table = table;
        }

        public override long GetRowCount()
        {
            return table.GetRowCount();
        }

        public override long GetRowValue(long row)
        {
            return row;
        }

        public override void SetRowValue(long row, long value)
        {
            //do nothing
        }
    }
    internal abstract class Table
    {
        protected Database.MetaTable m_Meta;
        public Database.MetaTable GetMetaData() { return m_Meta; }

        public virtual string GetName() { return m_Meta.name; }
        public virtual string GetDisplayName() { return m_Meta.displayName; }

        protected RowIndexColumn m_RowIndexColumn;
        protected List<Column> m_Columns = new List<Column>();
        public Schema Schema;
        public Table(Schema schema)
        {
            this.Schema = schema;
        }

        public virtual MetaColumn GetMetaColumnByColumn(Column col)
        {
            int index = m_Columns.IndexOf(col);
            if (index >= 0)
            {
                return m_Meta.GetColumnByIndex(index);
            }
            return null;
        }

        public virtual Column GetColumnByIndex(int index)
        {
            if (index < 0 || index >= m_Columns.Count)
            {
                return null;
            }
            return m_Columns[index];
        }

        public Column GetColumnByName(string name)
        {
            if (name == MetaTable.kRowIndexColumnName)
            {
                if (m_RowIndexColumn == null)
                {
                    m_RowIndexColumn = new RowIndexColumn(this);
                }
                return m_RowIndexColumn;
            }
            var mc = m_Meta.GetColumnByName(name);
            if (mc == null) return null;
            return GetColumnByIndex(mc.Index);
        }

        //will return -1 if the underlying data has not been computed yet.
        // ComputeRowCount or Update should be called at least once before getting accurate row count
        public abstract long GetRowCount();

        // returns if the table was dirty
        // default implementation calls BeginUpdate and EndUpdate.
        public virtual bool Update()
        {
            bool wasDirty = UpdateColumns();
            var updater = BeginUpdate();
            EndUpdate(updater);
            return wasDirty || updater != null;
        }

        // returns if any column was dirty
        protected virtual bool UpdateColumns()
        {
            bool wasDirty = false;
            foreach (var c in m_Columns)
            {
                bool colWasdirty = c.Update();
                wasDirty = wasDirty || colWasdirty;
            }
            return wasDirty;
        }

        internal interface IUpdater
        {
            long OldToNewRow(long a);
            long GetRowCount();
        }
        protected class DefaultDirtyUpdater : IUpdater
        {
            public Table m_Table;
            public DefaultDirtyUpdater(Table t)
            {
                m_Table = t;
            }

            long IUpdater.OldToNewRow(long a)
            {
                return a;
            }

            long IUpdater.GetRowCount()
            {
                return m_Table.GetRowCount();
            }
        }
        public virtual IUpdater BeginUpdate() { return null; }
        public virtual void EndUpdate(IUpdater updater) {}

        // returns if a new row count has been computed
        public virtual bool ComputeRowCount()
        {
            return false;
        }

        public struct ExpandableCellState
        {
            public bool isExpandable;
            public bool isExpanded;
            public bool isRowExpandable;//if the row might have an expandable cell
            public bool isColumnExpandable;//if the column might have an expandable cell
            public int expandDepth;
            public static ExpandableCellState NonExpandable
            {
                get
                {
                    ExpandableCellState o;
                    o.isExpandable = false;
                    o.isExpanded = false;
                    o.isColumnExpandable = false;
                    o.isRowExpandable = false;
                    o.expandDepth = 0;
                    return o;
                }
            }
        }
        public virtual ExpandableCellState GetCellExpandState(long row, int col) { return ExpandableCellState.NonExpandable; }
        public virtual LinkRequest GetCellLink(Database.CellPosition pos)
        {
            var column = GetColumnByIndex(pos.col);
            if (column != null) return column.GetRowLink(pos.row);
            return null;
        }

        public abstract Database.CellLink GetLinkTo(CellPosition pos);
        //return the range or new/deleted row. length is positive for added, negative for removed
        public virtual Range ExpandCell(long row, int col, bool expanded) { return Range.Empty(); }
    }

    internal abstract class CellLink
    {
        public abstract CellPosition Apply(Table t);
    }
    internal class LinkPosition : CellLink
    {
        public LinkPosition(CellPosition pos)
        {
            this.pos = pos;
        }

        public CellPosition pos;
        public override CellPosition Apply(Table t)
        {
            return pos;
        }

        public override string ToString()
        {
            return pos.ToString();
        }
    }


    internal interface IExpandColumn
    {
        void Initialize(ExpandTable table, Column column, int columnIndex);
    }
}
