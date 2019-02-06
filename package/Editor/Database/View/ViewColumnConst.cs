using System;

namespace Unity.MemoryProfiler.Editor.Database.View
{
    // Used when displaying a constant value for all rows
    internal class ViewColumnConst<DataT> : Database.ColumnTyped<DataT>, ViewColumn.IViewColumn where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ViewColumnConst<" + typeof(DataT).Name + ">{" + value.ToString() + "}";
        }

#endif
        private ViewColumn vc;
        public DataT value;
        public ViewColumnConst(ViewColumn vc, DataT v)
        {
            this.vc = vc;
            value = v;
        }

        public ViewColumnConst()
        {
        }

        void ViewColumn.IViewColumn.SetColumn(ViewColumn vc, Database.Column col)
        {
            if (this.vc != null)
            {
                throw new InvalidOperationException("Cannot call 'ViewColumn.IViewColumn.SetColumn' once already set");
            }
            this.vc = vc;
        }

        void ViewColumn.IViewColumn.SetConstValue(string value)
        {
            this.value = (DataT)Convert.ChangeType(value, typeof(DataT));
        }

        Database.Column ViewColumn.IViewColumn.GetColumn()
        {
            return this;
        }

        public override long GetRowCount()
        {
            return vc.viewTable.GetRowCount();
        }

        public override string GetRowValueString(long row)
        {
            if (vc.m_IsDisplayMergedOnly) return "";
            return value.ToString();
        }

        public override DataT GetRowValue(long row)
        {
            return value;
        }

        public override void SetRowValue(long row, DataT value)
        {
            this.value = value;
        }

        public override LinkRequest GetRowLink(long row)
        {
            return LinkRequestTable.MakeLinkRequest(vc.m_MetaLink, vc.viewTable, this, row, vc.ParsingContext);
        }
    }
}
