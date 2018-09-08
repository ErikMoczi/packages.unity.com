using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.Database.Operation.Filter
{
    public class Sort : Filter
    {
        public abstract class Level
        {
            public Level(SortOrder order)
            {
                this.order = order;
            }

            public abstract int GetColumnIndex(Database.Table tableIn);
            public abstract string GetColumnName(Database.Table tableIn);
            //public long columnIndex;
            public SortOrder order;
        }
        public class LevelByIndex : Level
        {
            public LevelByIndex(int columnIndex, SortOrder order)
                : base(order)
            {
                this.columnIndex = columnIndex;
            }

            public override int GetColumnIndex(Database.Table tableIn)
            {
                return columnIndex;
            }

            public override string GetColumnName(Database.Table tableIn)
            {
                return tableIn.GetMetaData().GetColumnByIndex(columnIndex).name;
            }

            public int columnIndex;
        }
        public class LevelByName : Level
        {
            public LevelByName(string columnName, SortOrder order)
                : base(order)
            {
                this.columnName = columnName;
            }

            public override int GetColumnIndex(Database.Table tableIn)
            {
                return tableIn.GetMetaData().GetColumnByName(columnName).index;
            }

            public override string GetColumnName(Database.Table tableIn)
            {
                return columnName;
            }

            public string columnName;
        }
        public List<Level> sortLevel = new List<Level>();

        public override Filter Clone(FilterCloning fc)
        {
            Sort o = new Sort();

            o.sortLevel.Capacity = sortLevel.Count;
            foreach (var l in sortLevel)
            {
                o.sortLevel.Add(l);
            }

            return o;
        }

        public override Database.Table CreateFilter(Database.Table tableIn)
        {
            if (sortLevel.Count == 0)
            {
                return tableIn;
            }
            if (tableIn is ExpandTable)
            {
                var et = (ExpandTable)tableIn;
                et.ResetAllGroup();
            }
            return CreateFilter(tableIn, new ArrayRange(0, tableIn.GetRowCount()));
        }

        public override Database.Table CreateFilter(Database.Table tableIn, ArrayRange range)
        {
            if (sortLevel.Count == 0)
            {
                return new Database.Operation.IndexedTable(tableIn, range);
            }


            int[] columnIndex = new int[sortLevel.Count];
            SortOrder[] order = new SortOrder[sortLevel.Count];
            for (int i = 0; i != sortLevel.Count; ++i)
            {
                columnIndex[i] = sortLevel[i].GetColumnIndex(tableIn);
                order[i] = sortLevel[i].order;
            }
            return new Database.Operation.SortedTable(tableIn, columnIndex, order, 0, range);
        }

        public override IEnumerable<Filter> SubFilters()
        {
            yield break;
        }

        public override bool OnGui(Database.Table sourceTable, ref bool dirty)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            int iLevelToRemove = -1;
            for (int i = 0; i != sortLevel.Count; ++i)
            {
                string sortName = GetSortName(sortLevel[i].order);
                string colName = sortLevel[i].GetColumnName(sourceTable);

                EditorGUILayout.BeginHorizontal();
                if (OnGui_RemoveButton())
                {
                    iLevelToRemove = i;
                }
                GUILayout.Label("Sort" + sortName + " '" + colName + "'");
                EditorGUILayout.EndHorizontal();
            }
            if (iLevelToRemove >= 0)
            {
                dirty = true;
                sortLevel.RemoveAt(iLevelToRemove);
            }

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            //remove this filter if it's empty
            return sortLevel.Count == 0;
        }

        public override void UpdateColumnState(Database.Table sourceTable, ColumnState[] colState)
        {
            foreach (var l in sortLevel)
            {
                colState[l.GetColumnIndex(sourceTable)].sorted = l.order;
            }
        }

        public override bool Simplify(ref bool dirty)
        {
            return sortLevel.Count == 0;
        }
    }
}
