using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.Database
{
    public class MetaTable
    {
        public static string kRowIndexColumnName = "_row_index_";
        public string name;
        public string displayName;
        protected List<MetaColumn> m_Columns = new List<MetaColumn>();
        protected Dictionary<string, Database.MetaColumn> m_ColumnsByName = new Dictionary<string, Database.MetaColumn>();
        public Operation.Filter.Filter defaultFilter;
        public Operation.Filter.Sort defaultAllLevelSortFilter;
        public int GetColumnCount()
        {
            return m_Columns.Count;
        }

        public int[] GetPrimaryKeyColumnIndex()
        {
            System.Collections.Generic.List<int> o = new System.Collections.Generic.List<int>();
            for (int i = 0; i < m_Columns.Count; ++i)
            {
                if (m_Columns[i].isPrimaryKey)
                {
                    o.Add(i);
                }
            }
            return o.ToArray();
        }

        public Database.MetaColumn GetColumnByIndex(int index)
        {
            if (index >= m_Columns.Count)
            {
                return null;
            }
            return m_Columns[index];
        }

        public Database.MetaColumn GetColumnByName(string name)
        {
            Database.MetaColumn o;
            if (name == kRowIndexColumnName)
            {
                o = new Database.MetaColumn(kRowIndexColumnName, "Row", typeof(long), false, null, null);
                return o;
            }

            if (m_ColumnsByName.TryGetValue(name, out o))
            {
                return o;
            }
            return null;
        }

        public long GetColumnIndexByName(string name)
        {
            return m_Columns.FindIndex(x => x.name == name);
        }

        public void SetColumns(Database.MetaColumn[] cols)
        {
            m_Columns = new List<MetaColumn>(cols);
            int i = 0;
            foreach (var c in m_Columns)
            {
                c.index = i;
                m_ColumnsByName.Add(c.name, c);
                ++i;
            }
        }

        public void AddColumn(MetaColumn col)
        {
            m_Columns.Add(col);
            col.index = m_Columns.Count - 1;
            m_ColumnsByName.Add(col.name, col);
        }

        public void SwapColumn(int a, int b)
        {
            var col = m_Columns[a];
            m_Columns[a] = m_Columns[b];
            m_Columns[b] = col;
            m_Columns[a].index = a;
            m_Columns[b].index = b;
        }
    }
}
