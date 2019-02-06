using UnityEngine;

namespace Unity.MemoryProfiler.Editor.Database.Operation
{
    internal class GroupedColumnTyped<DataT> : ExpandColumnTyped<DataT>, IGroupedColumn, IColumnDecorator where DataT : System.IComparable
    {
#if MEMPROFILER_DEBUG_INFO
        public override string GetDebugString(long row)
        {
            var e = m_GroupTable.m_RowData[row];
            if (e.isGroupHead())
            {
                return "GroupedColumnTyped<" + typeof(DataT).Name + ">[" + row + "]{ group " + e.groupIndex + "}";
            }
            else
            {
                var subTable = m_GroupTable.GetGroupSubTable(e.groupIndex);
                var subCol = subTable.GetColumnByIndex(m_ColumnIndex);
                return "GroupedColumnTyped<" + typeof(DataT).Name + ">[" + row + "]{" + subCol.GetDebugString(e.subGroupIndex) + "}";
            }
        }

#endif
        protected GroupedTable m_GroupTable;
        DataT[] m_GroupData;
        string[] m_GroupDataString;
        bool[] m_GroupComputed;
        bool m_IsGroupKey;

        public Grouping.IMergeAlgorithm algorithm;

        public GroupedColumnTyped()
        {
            type = typeof(DataT);
        }

        Column IColumnDecorator.GetBaseColumn()
        {
            return m_Column;
        }

        public void Initialize(GroupedTable table, Column column, int columnIndex, Grouping.IMergeAlgorithm algo, bool isGroup)
        {
            base.Initialize(table, column, columnIndex);
            m_GroupTable = table;
            algorithm = algo;
            m_IsGroupKey = isGroup;
            m_GroupComputed = new bool[table.m_Groups.Length];
            m_GroupData = new DataT[table.m_Groups.Length];
            m_GroupDataString = new string[table.m_Groups.Length];
        }

        public override long GetRowCount()
        {
            return m_GroupTable.m_Groups.Length;
        }

        public override LinkRequest GetRowLink(long row)
        {
            var e = m_GroupTable.m_RowData[row];
            if (e.isGroupHead())
            {
                if (m_GroupTable.IsGroupDegenerate(e.groupIndex) || m_IsGroupKey)
                {
                    // use first of group
                    long firstIndex = m_GroupTable.m_Groups[e.groupIndex].m_GroupIndice[0];
                    return m_Column.GetRowLink(firstIndex);
                }
                else if (algorithm != null)
                {
                    return algorithm.GetLink(m_Column, m_GroupTable.m_Groups[e.groupIndex].m_GroupIndice);
                }
            }
            else
            {
                var subTable = m_GroupTable.GetGroupSubTable(e.groupIndex);
                if (subTable != null)
                {
                    return subTable.GetCellLink(new CellPosition(e.subGroupIndex, m_ColumnIndex));
                }
            }
            return null;
        }

        private void ComputeGroupValue(long groupIndex, ref GroupedTable.Group tableGroup, long row)
        {
            if (m_IsGroupKey || m_GroupTable.IsGroupDegenerate(groupIndex))
            {
                //pick first value of the group
                m_GroupData[groupIndex] = m_Column.GetRowValue(tableGroup.m_GroupIndice[0]);
                m_GroupDataString[groupIndex] = m_Column.GetRowValueString(tableGroup.m_GroupIndice[0]);
            }
            else if (algorithm != null)
            {
                algorithm.Merge(this, row, m_Column, tableGroup.m_GroupIndice);
            }
            else
            {
                m_GroupData[groupIndex] = default(DataT);
                m_GroupDataString[groupIndex] = "";
            }
        }

        private DataT GetGroupValue(long groupIndex, long row)
        {
            if (!m_GroupComputed[groupIndex])
            {
                m_GroupComputed[groupIndex] = true;
                ComputeGroupValue(groupIndex, ref m_GroupTable.m_Groups[groupIndex], row);
            }
            return m_GroupData[groupIndex];
        }

        private string GetGroupValueString(long groupIndex, long row)
        {
            if (!m_GroupComputed[groupIndex])
            {
                m_GroupComputed[groupIndex] = true;
                ComputeGroupValue(groupIndex, ref m_GroupTable.m_Groups[groupIndex], row);
            }
            return m_GroupDataString[groupIndex];
        }

        public override string GetRowValueString(long row)
        {
            var e = m_GroupTable.m_RowData[row];
            if (e.isGroupHead())
            {
                string v = GetGroupValueString(e.groupIndex, row);
                if (m_IsGroupKey)
                {
                    string r = v;
                    r += " (" + m_GroupTable.m_Groups[e.groupIndex].m_GroupIndice.indexCount + ")";
                    return r;
                }
                else
                {
                    return v.ToString();
                }
            }
            else
            {
                var subTable = m_GroupTable.GetGroupSubTable(e.groupIndex);
                var subCol = (ColumnTyped<DataT>)subTable.GetColumnByIndex(m_ColumnIndex);
                return subCol.GetRowValueString(e.subGroupIndex);
            }
        }

        public override DataT GetRowValue(long row)
        {
            var e = m_GroupTable.m_RowData[row];
            if (e.isGroupHead())
            {
                return GetGroupValue(e.groupIndex, row);
            }
            else
            {
                var subTable = m_GroupTable.GetGroupSubTable(e.groupIndex);
                var subCol = (ColumnTyped<DataT>)subTable.GetColumnByIndex(m_ColumnIndex);
                return subCol.GetRowValue(e.subGroupIndex);
            }
        }

        public override void SetRowValue(long row, DataT value)
        {
            if (m_GroupTable.m_RowData[row].subGroupIndex >= 0)
            {
                //TODO
                //var groupIndex = m_Table.m_RowData[row].groupIndex;
                //var subGroupIndex = m_Table.m_RowData[row].subGroupIndex;
                //var subTableRow = m_Table.m_Groups[groupIndex].m_GroupIndice[subGroupIndex];
                //ColumnTyped<DataT> subTableCol = m_Column;//(ColumnTyped<DataT>)m_Table.m_Groups[groupIndex].table.GetColumnByIndex(m_ColumnIndex);
                //subTableCol.SetRowValue(subTableRow, value);


                UnityEngine.Debug.Assert(false);
            }
            else
            {
                m_GroupData[m_GroupTable.m_RowData[row].groupIndex] = value;

                m_GroupDataString[m_GroupTable.m_RowData[row].groupIndex] = m_Column.ValueToString(value);
            }
        }

        public override long[] GetMatchIndex(ArrayRange indices, Database.Operation.Matcher matcher)
        {
            return GetMatchIndex(indices, matcher, new Database.Operation.ExpColumn<DataT>(this));
        }
    }
}
