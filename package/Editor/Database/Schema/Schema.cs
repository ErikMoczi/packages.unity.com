namespace Unity.MemoryProfiler.Editor.Database
{
    internal class ParameterSet
    {
        public System.Collections.Generic.Dictionary<string, Operation.Expression> param = new System.Collections.Generic.Dictionary<string, Operation.Expression>();
        public void Add(string key, Operation.Expression value)
        {
            param.Add(key, value);
        }

        public bool TryGet(string key, out Operation.Expression value)
        {
            return param.TryGetValue(key, out value);
        }

        public void Add<DataT>(string key, DataT value) where DataT : System.IComparable
        {
            param.Add(key, new Operation.ExpConst<DataT>(value));
        }

        public bool TryGet<DataT>(string key, out DataT value) where DataT : System.IComparable
        {
            Operation.Expression exp;
            if (param.TryGetValue(key, out exp))
            {
                if (exp is Operation.ExpConst<DataT>)
                {
                    value = (exp as Operation.ExpConst<DataT>).GetValue(0);
                    return true;
                }
            }
            value = default(DataT);
            return false;
        }
    }
    internal class TableLink
    {
        public TableLink(string name)
        {
            this.name = name;
        }

        public TableLink(string name, ParameterSet param)
        {
            this.name = name;
            this.param = param;
        }

        public string name;
        public ParameterSet param;
    }
    internal abstract class Schema
    {
        public abstract long GetTableCount();
        public abstract Table GetTableByIndex(long index);
        public abstract Table GetTableByName(string name);
        public virtual Table GetTableByName(string name, ParameterSet param)
        {
            return GetTableByName(name);
        }

        public virtual Table GetTableByLink(TableLink link)
        {
            return GetTableByName(link.name, link.param);
        }

        public abstract string GetDisplayName();
        public abstract bool OwnsTable(Table table);
    }
    internal class SchemaAggregate : Schema
    {
        public string name = "<unknown>";
        public System.Collections.Generic.List<Table> tables = new System.Collections.Generic.List<Table>();
        public System.Collections.Generic.Dictionary<string, Table> tablesByName = new System.Collections.Generic.Dictionary<string, Table>();

        public override string GetDisplayName()
        {
            return name;
        }

        public override bool OwnsTable(Table table)
        {
            if (table.Schema == this) return true;
            return tables.Contains(table);
        }

        public override long GetTableCount()
        {
            return tables.Count;
        }

        public override Table GetTableByIndex(long index)
        {
            return tables[(int)index];
        }

        public override Table GetTableByName(string name)
        {
            Table vt;
            if (tablesByName.TryGetValue(name, out vt))
            {
                return vt;
            }
            return null;
        }

        public void AddTable(Table t)
        {
            string name = t.GetName();
            var existingTable = GetTableByName(name);
            if (existingTable != null)
            {
                tables.Remove(existingTable);
                tablesByName.Remove(name);
            }
            tables.Add(t);
            tablesByName.Add(name, t);
        }

        public void ClearTable()
        {
            tables.Clear();
            tablesByName.Clear();
        }
    }
}
