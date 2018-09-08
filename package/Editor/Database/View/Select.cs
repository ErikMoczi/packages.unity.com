using System.Xml;
using System.Collections.Generic;
using Unity.MemoryProfiler.Editor.Debuging;

namespace Unity.MemoryProfiler.Editor.Database.View
{
    // Defines what data to select and where it comes from
    public class Select
    {
#if MEMPROFILER_DEBUG_INFO
        public string GetDebugString(long columnRow, long valueRow)
        {
            string str = "'" + name + "' => Select from table '" + sourceTable.GetName() + "'";
            if (where.Count > 0)
            {
                str += " Where ";
                str += where.GetDebugString(columnRow, valueRow);
            }
            if (isManyToMany)
            {
                str += "[ManyToMany]";
            }
            return str;
        }

#endif
        public string name;
        public Database.Table sourceTable;
        public WhereUnion where;

        // Will yield a result of no more than this amount of rows. -1 is unlimited
        public int MaxRow = -1;

        //if the select statement has any where statements with multiple row conditions
        public bool isManyToMany = false;

        public bool HasWhereCondition()
        {
            return where != null && where.Count > 0;
        }

        // useful when isManyToMany == false as it does not require a row parameter
        // if isManyToMany == true, return the result only for matching where expression row 0
        public long[] GetMatchingIndices()
        {
            return GetMatchingIndices(0);
        }

        // useful when isManyToMany = false as it does not need require a row parameter
        public bool ComputeRowCount()
        {
            if (!isManyToMany && cacheMatchingIndices == null)
            {
                GetMatchingIndices();
                return true;
            }
            return false;
        }

        // Cache for querying matching indices for a specific source row
        private long[] cacheMatchingIndices;
        private long cacheMatchingIndicesRow = -1;
        public long[] GetMatchingIndices(long row)
        {
            if (cacheMatchingIndicesRow != row)
            {
                if (HasWhereCondition())
                {
                    cacheMatchingIndices = where.GetMatchingIndices(row);
                    cacheMatchingIndicesRow = row;
                    if (MaxRow >= 0)
                    {
                        System.Array.Resize(ref cacheMatchingIndices, MaxRow);
                    }
                }
            }
            return cacheMatchingIndices;
        }

        // Get row count if known without heavy computation
        public long GetRowCount()
        {
            if (isManyToMany)
            {
                // requires to computes all results for all rows. too heavy
                return -1;
            }
            else
            {
                if (cacheMatchingIndices != null)
                {
                    // we've computed the result already
                    return cacheMatchingIndices.Length;
                }
                else if (!HasWhereCondition())
                {
                    // we are selecting all rows from source table
                    return sourceTable.GetRowCount();
                }
            }

            return -1;
        }

        //get the first matching index for each row between [rowFirst, rowLast) of expressions used in where statements
        public void GetIndexFirstMatches(bool[] aIsComputed, long[] aIndex, long rowFirst, long rowLast)
        {
            if (where == null)
            {
                long[] result = new long[rowLast - rowFirst];
                for (int i = 0; i != result.Length; ++i)
                {
                    aIsComputed[i] = true;
                    aIndex[i] = i;
                }
            }
            else
            {
                for (long i = rowFirst; i < rowLast; ++i)
                {
                    if (!aIsComputed[i])
                    {
                        aIsComputed[i] = true;
                        aIndex[i] = where.GetIndexFirstMatch(i);
                    }
                }
            }
        }

        //get the first matching index for each row between [rowFirst, rowLast) of expressions used in where statements
        public long[] GetIndexFirstMatches(long rowFirst, long rowLast)
        {
            if (where == null)
            {
                long[] result = new long[rowLast - rowFirst];
                for (int i = 0; i != result.Length; ++i)
                {
                    result[i] = i;
                }
                return result;
            }
            return where.GetIndexFirstMatches(rowFirst, rowLast);
        }

        //get the first matching index for a specific row of expressions used in where statements
        public long GetIndexFirstMatch(long row)
        {
            if (where == null) return row;
            return where.GetIndexFirstMatch(row);
        }

        public long GetIndexFirstMatchRowCount()
        {
            if (where == null) return 0;
            long rowMin = long.MaxValue;
            foreach (var w in where.where)
            {
                long rowCount = w.Comparison.value.RowCount();
                if (rowCount < rowMin)
                {
                    rowMin = rowCount;
                }
            }
            if (rowMin == long.MaxValue)
            {
                return 0;
            }
            return rowMin;
        }

        public class Builder
        {
            public string name;
            public string sourceTableName;
            public int MaxRow = -1;
            public Where.Builder AddWhere(string column, Operation.Operator op, Operation.Expression.MetaExpression value)
            {
                Where.Builder w = new Where.Builder(column, op, value);
                where.Add(w);
                return w;
            }

            protected System.Collections.Generic.List<Where.Builder> where = new System.Collections.Generic.List<Where.Builder>();
            public Select Create(ViewScheme vs, Database.Scheme baseScheme, Table table)
            {
                Select sel = new Select();
                sel.name = name;
                sel.sourceTable = baseScheme.GetTableByName(sourceTableName);
                sel.MaxRow = MaxRow;
                if (sel.sourceTable == null)
                {
                    using (ScopeDebugContext.Func(() => { return "Select:'" + name + "'"; }))
                    {
                        DebugUtility.LogError("Error while building view '" + vs.name + "' select '" + name + "'. No table named '" + sourceTableName + "'");
                        return null;
                    }
                }

                return sel;
            }

            public void Build(ViewScheme vs, ViewTable vTable, SelectSet selectSet, Select sel, Database.Scheme baseScheme, Operation.ExpressionParsingContext expressionParsingContext)
            {
                using (ScopeDebugContext.Func(() => { return "Select:'" + name + "'"; }))
                {
                    if (where.Count > 0)
                    {
                        sel.where = new WhereUnion();
                        foreach (var w in where)
                        {
                            var w2 = w.Build(vs, vTable, selectSet, sel, baseScheme, sel.sourceTable, expressionParsingContext);
                            if (w2.Comparison.IsManyToMany())
                            {
                                sel.isManyToMany = true;
                            }
                            sel.where.Add(w2);
                        }
                    }
                }
            }

            public static Builder LoadFromXML(XmlElement root)
            {
                Builder b = new Builder();
                b.name = root.GetAttribute("name");
                b.sourceTableName = root.GetAttribute("table");
                string strMaxRow = root.GetAttribute("maxRow");
                if (string.IsNullOrEmpty(strMaxRow) || !int.TryParse(strMaxRow, out b.MaxRow))
                {
                    b.MaxRow = -1;
                }
                using (ScopeDebugContext.Func(() => { return "Select:'" + b.name + "'"; }))
                {
                    foreach (XmlNode node in root.ChildNodes)
                    {
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            XmlElement e = (XmlElement)node;
                            switch (e.Name)
                            {
                                case "Where":
                                {
                                    var w = Where.Builder.LoadFromXML(e);
                                    if (w != null)
                                    {
                                        b.where.Add(w);
                                    }
                                    break;
                                }
                                default:
                                    DebugUtility.LogInvalidXmlChild(root, e);
                                    break;
                            }
                        }
                    }
                    return b;
                }
            }
        }
    }


    // A collection of select statement
    public class SelectSet
    {
        public System.Collections.Generic.List<Select> select = new System.Collections.Generic.List<Select>();
        public System.Collections.Generic.Dictionary<string, Select> selectByName = new System.Collections.Generic.Dictionary<string, Select>();

        // Applies a condition on the main select result rows. Will remove the rows that does not meet the condition. When null, all rows are valid
        public Operation.ExpComparison Condition;


        public bool TryGetMainSelect(out Select select)
        {
            if (this.select.Count > 0)
            {
                select = this.select[0];
                return true;
            }
            select = null;
            return false;
        }

        private long[] m_Indices;
        // Returns the index of the resulting rows from the main select.
        public long[] GetMainRows()
        {
            if (m_Indices != null) return m_Indices;
            if (select.Count == 0) return null;
            m_Indices = select[0].GetMatchingIndices();
            return m_Indices;
        }

        private long[] m_ConditionalIndices;
        // Returns an array of indices into GetMainRows() of rows that passes the SelectSet condition.
        // If it returns null, all entries in GetMainRowIndices() pass the condition
        public long[] GetConditionalRowIndices()
        {
            if (m_ConditionalIndices != null) return m_ConditionalIndices;
            if (select.Count == 0) return null;
            long[] rows = GetMainRows();
            if (Condition != null)
            {
                //run the select result through the condition
                var conditionalIndices = new List<long>();
                for (int i = 0; i != rows.Length; ++i)
                {
                    if (Condition.GetValue(i))
                    {
                        conditionalIndices.Add(i);
                    }
                }
                m_ConditionalIndices = conditionalIndices.ToArray();
            }
            return m_ConditionalIndices;
        }

        public long GetRowCount()
        {
            var indices = GetConditionalRowIndices();
            if (indices != null) return indices.LongLength;
            var rows = GetMainRows();
            if (rows != null) return rows.LongLength;
            return -1;
        }

        public bool ComputeRowCount()
        {
            if (m_Indices == null)
            {
                GetConditionalRowIndices();
                return m_Indices != null;
            }
            return false;
        }

        public bool IsManyToMany()
        {
            if (select.Count > 0)
            {
                return select[0].isManyToMany;
            }
            return false;
        }

        public void Add(Select newSelect)
        {
            m_Indices = null;
            m_ConditionalIndices = null;
            select.Add(newSelect);
            selectByName.Add(newSelect.name, newSelect);
        }

        public bool TryGetSelect(string name, out Select select)
        {
            return selectByName.TryGetValue(name, out select);
        }

        public class Builder
        {
            public System.Collections.Generic.List<Select.Builder> select = new System.Collections.Generic.List<Select.Builder>();
            public Operation.MetaExpComparison Condition;

            public SelectSet Build(ViewTable viewTable, ViewScheme viewScheme, Database.Scheme baseScheme)
            {
                if (select.Count == 0) return null;

                SelectSet selectSet = new SelectSet();

                // Create select statements (first pass)
                foreach (var iSelect in select)
                {
                    Select s = iSelect.Create(viewTable.viewScheme, baseScheme, viewTable);
                    if (s != null)
                    {
                        selectSet.Add(s);
                    }
                }

                // add current set to the expression parsing hierarchy
                var expressionParsingContext = new Operation.ExpressionParsingContext(viewTable.expressionParsingContext, selectSet);

                // Build select statements (second pass)
                var eSelBuilder = select.GetEnumerator();
                var eSelList = selectSet.select.GetEnumerator();
                while (eSelBuilder.MoveNext())
                {
                    eSelList.MoveNext();
                    eSelBuilder.Current.Build(viewScheme, viewTable, selectSet, eSelList.Current, baseScheme, expressionParsingContext);
                }

                if (Condition != null)
                {
                    Operation.Expression.ParseIdentifierOption parseOpt = new Operation.Expression.ParseIdentifierOption(viewScheme, viewTable, true, false, null, expressionParsingContext);
                    parseOpt.BypassSelectSetCondition = selectSet;
                    selectSet.Condition = Condition.Build(parseOpt);
                }

                return selectSet;
            }

            public static Builder LoadFromXML(XmlElement root)
            {
                using (ScopeDebugContext.Func(() => { return "Loading SelectSet"; }))
                {
                    Builder b = new Builder();
                    foreach (XmlNode xNode in root.ChildNodes)
                    {
                        if (xNode.NodeType == XmlNodeType.Element)
                        {
                            XmlElement e = (XmlElement)xNode;
                            switch (e.Name)
                            {
                                case "Select":
                                {
                                    var s = Select.Builder.LoadFromXML(e);
                                    if (s != null)
                                    {
                                        b.select.Add(s);
                                    }
                                    break;
                                }
                                case "Condition":
                                {
                                    b.Condition = Operation.MetaExpComparison.LoadFromXML(e);
                                    break;
                                }
                                default:
                                    DebugUtility.LogInvalidXmlChild(root, e);
                                    break;
                            }
                        }
                    }
                    return b;
                }
            }
        }
    }
}
