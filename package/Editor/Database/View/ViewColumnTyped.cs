using System;
using Unity.MemoryProfiler.Editor.Debuging;

namespace Unity.MemoryProfiler.Editor.Database.View
{
    // Used only with select statement that are not Many-To-Many
    internal class ViewColumnTyped<DataT> : Database.ColumnTyped<DataT>, ViewColumn.IViewColumn where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            if (m_rowIndex == null)
            {
                return "ViewColumnExpressionType<" + typeof(DataT).Name + ">[" + row + "]{" + column.GetDebugString(row) + "}";
            }
            else if (row < m_rowIndex.Length && m_rowIndex[row] >= 0)
            {
                return "ViewColumnExpressionType<" + typeof(DataT).Name + ">[" + row + "]{" + column.GetDebugString(m_rowIndex[row]) + "}";
            }
            else
            {
                return "ViewColumnExpressionType<" + typeof(DataT).Name + ">[" + row + "]{}";
            }
        }

#endif
        private long[] m_rowIndex; //when is null, either the data was not computed yet or the column act as passthrough (no where condition on the select statement)
        private ViewColumn vc;
        public Database.ColumnTyped<DataT> column;

        public ViewColumnTyped()
        {
            type = typeof(DataT);
        }

        void ViewColumn.IViewColumn.SetColumn(ViewColumn vc, Database.Column col)
        {
            this.vc = vc;
            column = (Database.ColumnTyped<DataT>)col;
            m_rowIndex = null;
        }

        void ViewColumn.IViewColumn.SetConstValue(string value)
        {
            var metaColumn = vc.viewTable.GetMetaColumnByColumn(this);
            string extraInfo = "";
            if (metaColumn != null)
            {
                extraInfo += " column '" + metaColumn.Name + "'";
            }
            DebugUtility.LogError("Cannot set a const value on an indexed view column. Table '" + vc.viewTable.GetName() + "'" + extraInfo);
        }

        Database.Column ViewColumn.IViewColumn.GetColumn()
        {
            return this;
        }

        public override long GetRowCount()
        {
            if (m_rowIndex != null)
            {
                return m_rowIndex.Length;
            }
            return vc.viewTable.GetRowCount();
        }

        public override bool ComputeRowCount()
        {
            if (m_rowIndex != null) return false;
            return vc.viewTable.ComputeRowCount();
        }

        public override bool Update()
        {
            if (m_rowIndex != null) return false;
            if (vc.ParsingContext != null && vc.ParsingContext.fixedRow >= 0)
            {
                m_rowIndex = vc.select.GetMatchingIndices(vc.ParsingContext.fixedRow);
            }
            else
            {
                m_rowIndex = vc.select.GetMatchingIndices();
            }

            return m_rowIndex != null;
        }

        public override LinkRequest GetRowLink(long row)
        {
            return LinkRequestTable.MakeLinkRequest(vc.m_MetaLink, vc.viewTable, this, row, vc.ParsingContext);
        }

        public override string GetRowValueString(long row)
        {
            if (vc.m_IsDisplayMergedOnly) return "";
            Update();
            if (m_rowIndex == null)
            {
                //act as passthrough
                return column.GetRowValue(row).ToString();
            }
            else if (row < m_rowIndex.Length && m_rowIndex[row] >= 0)
            {
                return column.GetRowValue(m_rowIndex[row]).ToString();
            }
            else
            {
                return "N/A";
            }
        }

        public override DataT GetRowValue(long row)
        {
            Update();
            if (m_rowIndex == null)
            {
                //act as passthrough
                return column.GetRowValue(row);
            }
            else if (row < m_rowIndex.Length && m_rowIndex[row] >= 0)
            {
                return column.GetRowValue(m_rowIndex[row]);
            }
            else
            {
                return default(DataT);
            }
        }

        public override void SetRowValue(long row, DataT value)
        {
            Update();
            if (m_rowIndex == null)
            {
                //act as passthrough
                column.SetRowValue(row, value);
            }
            else if (row < m_rowIndex.Length && m_rowIndex[row] >= 0)
            {
                column.SetRowValue(m_rowIndex[row], value);
            }
        }
    }
}
