using System;

namespace Unity.MemoryProfiler.Editor.Database.View
{
    public class ViewColumnNode
    {
        public ViewTable viewTable;
        public MetaColumn metaColumn;
        public Operation.ExpressionParsingContext ParsingContext;
        public ViewColumnNode(ViewTable viewTable, MetaColumn metaColumn, Operation.ExpressionParsingContext parsingContext)
        {
            this.viewTable = viewTable;
            this.metaColumn = metaColumn;
            ParsingContext = parsingContext;
        }

        public interface IViewColumnNode
        {
            void SetColumn(ViewColumnNode vc);
            Database.Column GetColumn();
            void SetEntry(long row, Operation.Expression exp, MetaLink Link);
        }
    }

    // List of expression set by a view when the data type is "node"
    public class ViewColumnNodeTyped<DataT> : Database.ColumnTyped<DataT>, ViewColumnNode.IViewColumnNode where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            if (entries[(int)row] == null)
            {
                return "ViewColumnNodeTyped<" + typeof(DataT).Name + ">[" + row + "]{ no entries }";
            }
            else
            {
                return "ViewColumnNodeTyped<" + typeof(DataT).Name + ">[" + row + "]{" + entries[row].GetDebugString(0) + "}";
            }
        }

#endif
        public Operation.TypedExpression<DataT>[] entries;
        public MetaLink[] linkEntries;
        private ViewColumnNode m_ViewColumn;
        //TODO add cache here, clear it on update
        public ViewColumnNodeTyped()
        {
            type = typeof(DataT);
        }

        void ViewColumnNode.IViewColumnNode.SetColumn(ViewColumnNode vc)
        {
            if (vc.viewTable.node.data.type != ViewTable.Builder.Node.Data.DataType.Node)
            {
                throw new Exception("Cannot set a ViewColumnNodeTyped column on a non-Node data type view table");
            }
            m_ViewColumn = vc;
            entries = new Operation.TypedExpression<DataT>[vc.viewTable.GetNodeChildCount()];
            linkEntries = new MetaLink[vc.viewTable.GetNodeChildCount()];
        }

        Database.Column ViewColumnNode.IViewColumnNode.GetColumn()
        {
            return this;
        }

        void ViewColumnNode.IViewColumnNode.SetEntry(long row, Operation.Expression exp, MetaLink link)
        {
            entries[(int)row] = exp as Operation.TypedExpression<DataT>;
            linkEntries[(int)row] = link;
        }

        public override long GetRowCount()
        {
            return entries.Length;
        }

        public override Database.View.LinkRequest GetRowLink(long row)
        {
            return Database.View.LinkRequest.MakeLinkRequest(linkEntries[(int)row], m_ViewColumn.viewTable, this, row, m_ViewColumn.ParsingContext);
        }

        public override string GetRowValueString(long row)
        {
            if (entries[(int)row] == null)
            {
                return "";
            }
            return entries[(int)row].GetValueString(0);
        }

        public override DataT GetRowValue(long row)
        {
            if (entries[(int)row] == null)
            {
                return default(DataT);
            }
            return entries[(int)row].GetValue(0);
        }

        public override void SetRowValue(long row, DataT value)
        {
        }
    }
}
