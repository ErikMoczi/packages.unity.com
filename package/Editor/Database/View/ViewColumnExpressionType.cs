using System;

namespace Unity.MemoryProfiler.Editor.Database.View
{
    // Column that yield the value of an expression
    public class ViewColumnExpressionType<DataT> : Database.ColumnTyped<DataT>, ViewColumn.IViewColumn where DataT : IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            return "ViewColumnExpressionType<" + typeof(DataT).Name + ">[" + row + "]{" + SourceExpression.GetDebugString(row) + "}";
        }

#endif
        public ViewColumn SourceViewColumn;
        public Operation.TypedExpression<DataT> SourceExpression;

        public ViewColumnExpressionType(Operation.TypedExpression<DataT> expression)
        {
            type = typeof(DataT);
            SourceExpression = expression;
        }

        void ViewColumn.IViewColumn.SetColumn(ViewColumn vc, Database.Column col)
        {
            SourceViewColumn = vc;
        }

        void ViewColumn.IViewColumn.SetConstValue(string value)
        {
        }

        Database.Column ViewColumn.IViewColumn.GetColumn()
        {
            return this;
        }

        public override long GetRowCount()
        {
            return SourceExpression.RowCount();
        }

        public override bool ComputeRowCount()
        {
            return SourceViewColumn.viewTable.ComputeRowCount();
        }

        public override Database.View.LinkRequest GetRowLink(long row)
        {
            if (SourceViewColumn.ParsingContext != null && SourceViewColumn.ParsingContext.fixedRow >= 0)
            {
                return Database.View.LinkRequest.MakeLinkRequest(SourceViewColumn.m_MetaLink, SourceViewColumn.viewTable, this, SourceViewColumn.ParsingContext.fixedRow, SourceViewColumn.ParsingContext);
            }
            else
            {
                return Database.View.LinkRequest.MakeLinkRequest(SourceViewColumn.m_MetaLink, SourceViewColumn.viewTable, this, row, SourceViewColumn.ParsingContext);
            }
        }

        public override string GetRowValueString(long row)
        {
            if (SourceViewColumn.m_IsDisplayMergedOnly) return "";
            if (SourceViewColumn.ParsingContext != null && SourceViewColumn.ParsingContext.fixedRow >= 0)
            {
                return SourceExpression.GetValueString(SourceViewColumn.ParsingContext.fixedRow);
            }
            else
            {
                return SourceExpression.GetValueString(row);
            }
        }

        public override DataT GetRowValue(long row)
        {
            if (SourceViewColumn.ParsingContext != null && SourceViewColumn.ParsingContext.fixedRow >= 0)
            {
                return SourceExpression.GetValue(SourceViewColumn.ParsingContext.fixedRow);
            }
            else
            {
                return SourceExpression.GetValue(row);
            }
        }

        public override void SetRowValue(long row, DataT value)
        {
        }
    }
}
