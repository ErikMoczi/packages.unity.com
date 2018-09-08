using System;

namespace Unity.MemoryProfiler.Editor.Database.View
{
    // Used when displaying a constant value for all rows
    public class ViewColumnConst<DataT> : Database.ColumnTyped<DataT>, ViewColumn.IViewColumn where DataT : IComparable
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

        public override Database.View.LinkRequest GetRowLink(long row)
        {
            return Database.View.LinkRequest.MakeLinkRequest(vc.m_MetaLink, vc.viewTable, this, row, vc.ParsingContext);
        }
    }
}
