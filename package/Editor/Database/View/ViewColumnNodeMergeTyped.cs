using System;

namespace Unity.MemoryProfiler.Editor.Database.View
{
    // List of merged value on a view when the data type is "node"
    internal class ViewColumnNodeMergeTyped<DataT> : ColumnTyped<DataT>, ViewColumnNode.IViewColumnNode where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            var subTable = m_ViewColumnNode.viewTable.CreateGroupTable(row);
            if (subTable != null)
            {
                var subColumn = subTable.GetColumnByIndex(m_ViewColumnNode.metaColumn.index);
                var rowCount = subColumn.GetRowCount();
                if (rowCount == 0)
                {
                    return "ViewColumnNodeMergeTyped<" + typeof(DataT).Name + ">(0, 0)[" + row + "]{}";
                }
                else
                {
                    return "ViewColumnNodeMergeTyped<" + typeof(DataT).Name + ">(0, " + rowCount + ")[" + row + "]{ [0] = " + subColumn.GetDebugString(0) + "}";
                }
            }
            return "ViewColumnNodeMergeTyped<" + typeof(DataT).Name + ">(0, 0){ no sub table }";
        }

#endif
        private ViewColumnNode m_ViewColumnNode;
        public DataT[] entries;
        bool mbDirty = true;
        public ViewColumnNodeMergeTyped()
        {
            type = typeof(DataT);
        }

        void ViewColumnNode.IViewColumnNode.SetColumn(ViewColumnNode vc)
        {
            m_ViewColumnNode = vc;
        }

        void ClearCache()
        {
            entries = null;
            mbDirty = true;
        }

        void ComputeAllValues()
        {
            long rowCount = m_ViewColumnNode.viewTable.GetGroupCount();
            entries = new DataT[rowCount];
            for (long row = 0; row != rowCount; ++row)
            {
                using (Profiling.GetMarker(Profiling.MarkerId.MergeColumn).Auto())
                {
                    var subTable = m_ViewColumnNode.viewTable.CreateGroupTable(row);
                    if (subTable != null)
                    {
                        var subColumn = subTable.GetColumnByIndex(m_ViewColumnNode.metaColumn.Index);
                        while (subColumn is IColumnDecorator)
                        {
                            subColumn = (subColumn as IColumnDecorator).GetBaseColumn();
                        }
                        m_ViewColumnNode.metaColumn.DefaultMergeAlgorithm.Merge(this, row, subColumn, new ArrayRange(0, subColumn.GetRowCount()));
                    }
                }
            }
            mbDirty = false;
        }

        Database.Column ViewColumnNode.IViewColumnNode.GetColumn()
        {
            return this;
        }

        void ViewColumnNode.IViewColumnNode.SetEntry(long row, Operation.Expression exp, TableLink link)
        {
        }

        public override bool Update()
        {
            bool changed = base.Update();
            if (changed)
            {
                ClearCache();
            }
            return changed;
        }

        public override long GetRowCount()
        {
            if (entries == null) return m_ViewColumnNode.viewTable.GetGroupCount();
            return entries.Length;
        }

        public override LinkRequest GetRowLink(long row)
        {
            return null;
        }

        public override string GetRowValueString(long row)
        {
            if (mbDirty)
            {
                ComputeAllValues();
            }
            return (string)Convert.ChangeType(entries[(int)row], typeof(string));
        }

        public override DataT GetRowValue(long row)
        {
            if (mbDirty)
            {
                ComputeAllValues();
            }
            return entries[(int)row];
        }

        public override void SetRowValue(long row, DataT value)
        {
            entries[(int)row] = value;
        }
    }
}
