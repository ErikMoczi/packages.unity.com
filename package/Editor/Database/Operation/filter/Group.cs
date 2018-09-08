using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.Database.Operation.Filter
{
    public abstract class Group : Filter
    {
        public SortOrder order;//default ordering
        public Filter subGroupFilter;
        public Group(SortOrder order)
        {
            this.order = order;
        }

        public abstract int GetColumnIndex(Database.Table tableIn);
        public abstract string GetColumnName(Database.Table tableIn);
        public override Database.Table CreateFilter(Database.Table tableIn)
        {
            Database.Operation.GroupedTable tableOut = new Database.Operation.GroupedTable(tableIn, new ArrayRange(0, tableIn.GetRowCount()), GetColumnIndex(tableIn), order, CreateGroupTable);
            return tableOut;
        }

        public override Database.Table CreateFilter(Database.Table tableIn, ArrayRange range)
        {
            Database.Operation.GroupedTable tableOut = new Database.Operation.GroupedTable(tableIn, range, GetColumnIndex(tableIn), order, CreateGroupTable);
            return tableOut;
        }

        protected Database.Table CreateGroupTable(Database.Operation.GroupedTable table, Database.Operation.GroupedTable.Group g, long groupIndex)
        {
            if (subGroupFilter != null)
            {
                return subGroupFilter.CreateFilter(table.m_table, g.m_GroupIndice);
            }
            Database.Operation.IndexedTable subTable = new Database.Operation.IndexedTable(table.m_table, g.m_GroupIndice);
            return subTable;
        }

        public override IEnumerable<Filter> SubFilters()
        {
            if (subGroupFilter != null)
            {
                yield return subGroupFilter;
            }
        }

        public override bool RemoveSubFilters(Filter f)
        {
            if (subGroupFilter == f)
            {
                subGroupFilter = null;
                return true;
            }
            return false;
        }

        public override bool ReplaceSubFilters(Filter replaced, Filter with)
        {
            if (subGroupFilter == replaced)
            {
                subGroupFilter = with;
                return true;
            }
            return false;
        }

        public override Filter GetSurrogate()
        {
            return subGroupFilter;
        }

        public override bool OnGui(Database.Table sourceTable, ref bool dirty)
        {
            string colName = GetColumnName(sourceTable);
            string sortName = GetSortName(order);
            EditorGUILayout.BeginHorizontal();
            bool bRemove = OnGui_RemoveButton();
            GUILayout.Label("Group" + sortName + " '" + colName + "'");
            if (subGroupFilter != null)
            {
                if (subGroupFilter.OnGui(sourceTable, ref dirty))
                {
                    dirty = true;
                    RemoveFilter(this, subGroupFilter);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return bRemove;
        }

        public override void UpdateColumnState(Database.Table sourceTable, ColumnState[] colState)
        {
            long columnIndex = GetColumnIndex(sourceTable);
            colState[columnIndex].grouped = true;
            //if(colState[columnIndex].sorted == database.SortOrder.None)
            //{
            //    colState[columnIndex].sorted = order;
            //}
            if (subGroupFilter != null)
            {
                subGroupFilter.UpdateColumnState(sourceTable, colState);
            }
        }

        public override bool Simplify(ref bool dirty)
        {
            if (subGroupFilter != null)
            {
                subGroupFilter.Simplify(ref dirty);
            }
            return false;
        }
    }


    public class GroupByColumnName : Group
    {
        public string columnName;
        public GroupByColumnName(string columnName, SortOrder order)
            : base(order)
        {
            this.columnName = columnName;
            this.order = order;
        }

        public override Filter Clone(FilterCloning fc)
        {
            GroupByColumnName o = new GroupByColumnName(columnName, order);
            if (subGroupFilter != null)
            {
                o.subGroupFilter = subGroupFilter.Clone(fc);
            }
            return o;
        }

        public override int GetColumnIndex(Database.Table tableIn)
        {
            return tableIn.GetMetaData().GetColumnByName(columnName).index;
        }

        public override string GetColumnName(Table tableIn)
        {
            return columnName;
        }
    }
    //sourceTable.GetMetaData().GetColumnByIndex(columnIndex).name;
    public class GroupByColumnIndex : Group
    {
        public int columnIndex;
        public GroupByColumnIndex(int columnIndex, SortOrder order)
            : base(order)
        {
            this.columnIndex = columnIndex;
            this.order = order;
        }

        public override Filter Clone(FilterCloning fc)
        {
            GroupByColumnIndex o = new GroupByColumnIndex(columnIndex, order);
            if (subGroupFilter != null)
            {
                o.subGroupFilter = subGroupFilter.Clone(fc);
            }
            return o;
        }

        public override int GetColumnIndex(Database.Table tableIn)
        {
            return columnIndex;
        }

        public override string GetColumnName(Database.Table tableIn)
        {
            return tableIn.GetMetaData().GetColumnByIndex(columnIndex).name;
        }
    }
}
