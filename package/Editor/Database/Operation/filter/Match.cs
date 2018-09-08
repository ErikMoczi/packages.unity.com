using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.MemoryProfiler.Editor.Database.Operation.Filter
{
    public class MatchTable : IndexedTable
    {
        public int m_columnIndex;
        public string m_matchString;
        public ArrayRange m_Range;
        public MatchTable(Database.Table sourceTable, int columnIndex, string matchString, ArrayRange range)
            : base(sourceTable)
        {
            m_columnIndex = columnIndex;
            m_matchString = matchString;
            m_Range = range;
            UpdateIndices();
        }

        public void UpdateIndices()
        {
            var metaCol = m_SourceTable.GetMetaData().GetColumnByIndex(m_columnIndex);
            var col = m_SourceTable.GetColumnByIndex(m_columnIndex);
            if (metaCol == null || col == null)
            {
                UnityEngine.Debug.LogError("No column index " + m_columnIndex + " on table '" + m_SourceTable.GetName() + "'");
                indices = new long[0];
            }
            var t = metaCol.type;
            Matcher m;
            if (t == typeof(string))
            {
                var ssm = new SubStringMatcher();
                ssm.value = m_matchString;
                m = ssm;
            }
            else
            {
                m = ColumnCreator.CreateConstMatcher(t, m_matchString);
                if (m == null)
                {
                    indices = new long[0];
                    return;
                }
            }
            var matchIndices = col.GetMatchIndex(m_Range, m);
            indices = matchIndices;
        }

        protected class IndexUpdater : IUpdater
        {
            public MatchTable m_Table;
            public long[] m_OldToNew;
            public long m_RowCount;
            long IUpdater.OldToNewRow(long a)
            {
                if (a < 0 || a >= m_OldToNew.Length) return -1;
                var subRow = m_OldToNew[a];
                var newIndex = System.Array.FindIndex(m_Table.indices, x => x == subRow);
                return newIndex;
            }

            long IUpdater.GetRowCount()
            {
                return m_RowCount;
            }
        }
        public override IUpdater BeginUpdate()
        {
            var oldRowCount = m_SourceTable.GetRowCount();
            var sourceUpdater = m_SourceTable.BeginUpdate();
            var updater = new IndexUpdater();
            updater.m_Table = this;
            updater.m_OldToNew = new long[indices.Length];
            for (int i = 0; i != indices.Length; ++i)
            {
                updater.m_OldToNew[i] = sourceUpdater.OldToNewRow(indices[i]);
            }
            if (m_Range.array != null)
            {
                for (int i = 0; i != m_Range.array.Length; ++i)
                {
                    m_Range.array[i] = sourceUpdater.OldToNewRow(m_Range.array[i]);
                }
            }
            else
            {
                if (m_Range.indexCount == oldRowCount)
                {
                    m_Range = new ArrayRange(0, sourceUpdater.GetRowCount());
                }
                else
                {
                    long newFirst = 0;
                    long newLast = 0;
                    for (long i = m_Range.directIndexFirst; i != m_Range.directIndexLast; ++i)
                    {
                        var n = sourceUpdater.OldToNewRow(i);
                        if (n >= 0)
                        {
                            newFirst = n;
                            break;
                        }
                    }

                    for (long i = m_Range.directIndexLast; i != m_Range.directIndexFirst; --i)
                    {
                        var n = sourceUpdater.OldToNewRow(i - 1);
                        if (n >= 0)
                        {
                            newLast = n + 1;
                            break;
                        }
                    }
                    if (newFirst < newLast)
                    {
                        m_Range = new ArrayRange(newFirst, newLast);
                    }
                    else
                    {
                        m_Range = new ArrayRange(0, sourceUpdater.GetRowCount());
                    }
                }
            }

            //TODO should not call end update here
            m_SourceTable.EndUpdate(sourceUpdater);
            UpdateIndices();
            updater.m_RowCount = indices.Length;
            return updater;
        }

        public override void EndUpdate(IUpdater updater)
        {
        }
    }
    public class Match : Filter
    {
        int m_ColumnIndex;
        string m_MatchString;
        int m_SelectedPopup = 0;
        public Match(int col)
        {
            m_ColumnIndex = col;
            m_MatchString = "";
        }

        public override Filter Clone(FilterCloning fc)
        {
            Match o = new Match(m_ColumnIndex);
            o.m_MatchString = m_MatchString;
            o.m_SelectedPopup = m_SelectedPopup;
            return o;
        }

        public override Database.Table CreateFilter(Database.Table tableIn)
        {
            return CreateFilter(tableIn, new ArrayRange(0, tableIn.GetRowCount()));
        }

        public override Database.Table CreateFilter(Database.Table tableIn, ArrayRange range)
        {
            if (String.IsNullOrEmpty(m_MatchString))
            {
                return tableIn;
            }
            var tableOut = new MatchTable(tableIn, m_ColumnIndex, m_MatchString, range);
            return tableOut;
        }

        public override IEnumerable<Filter> SubFilters()
        {
            yield break;
        }

        Database.Table m_CacheSourceTable;
        string[] m_CachePopupSelection;
        public override bool OnGui(Database.Table sourceTable, ref bool dirty)
        {
            EditorGUILayout.BeginHorizontal();
            bool bRemove = OnGui_RemoveButton();
            var metaCol = sourceTable.GetMetaData().GetColumnByIndex(m_ColumnIndex);
            string label;

            var t = metaCol.type;
            if (t == typeof(string))
            {
                label = "'" + metaCol.displayName + "' contains:";
            }
            else
            {
                label = "'" + metaCol.displayName + "' is:";
            }
            GUILayout.Label(label);
            if (t.IsEnum)
            {
                string[] popupSelection;
                if (m_CacheSourceTable == sourceTable)
                {
                    popupSelection = m_CachePopupSelection;
                }
                else
                {
                    var names = System.Enum.GetNames(t);
                    popupSelection = new string[names.Length + 1];
                    popupSelection[0] = "<All>";
                    System.Array.Copy(names, 0, popupSelection, 1, names.Length);

                    if (m_CacheSourceTable == null)
                    {
                        m_CacheSourceTable = sourceTable;
                        m_CachePopupSelection = popupSelection;
                    }
                }

                int newSelectedPopup = EditorGUILayout.Popup(m_SelectedPopup, popupSelection);
                if (m_SelectedPopup != newSelectedPopup)
                {
                    m_SelectedPopup = newSelectedPopup;
                    if (m_SelectedPopup == 0)
                    {
                        m_MatchString = "";
                    }
                    else
                    {
                        m_MatchString = popupSelection[m_SelectedPopup];
                    }
                    dirty = true;
                }
            }
            else
            {
                GUI.SetNextControlName(k_FilterInput);
                var newMatchString = GUILayout.TextField(m_MatchString, GUILayout.MinWidth(250));
                GUI.FocusControl(k_FilterInput);
                if (m_MatchString != newMatchString)
                {
                    m_MatchString = newMatchString;
                    dirty = true;
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return bRemove;
        }

        public override void UpdateColumnState(Database.Table sourceTable, ColumnState[] colState)
        {
        }

        public override bool Simplify(ref bool dirty)
        {
            return false;
        }
    }
}
