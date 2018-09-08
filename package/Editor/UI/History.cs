namespace Unity.MemoryProfiler.Editor.UI
{
    public abstract class HistoryEvent
    {
        protected const string seperator = "::";
        //public abstract bool IsSame(HistoryEvent e);
    }
    public class HETable : HistoryEvent
    {
        public HETable(UIState.BaseMode mode, Database.TableLink table, Database.Operation.Filter.Filter filter, Database.CellLink cell)
        {
            this.mode = mode;
            this.table = table;
            this.filter = filter;
            this.cell = cell;
        }

        public UIState.BaseMode mode;
        public Database.TableLink table;
        public Database.Operation.Filter.Filter filter;
        public Database.CellLink cell;
        public override string ToString()
        {
            string s = mode.GetSheme().GetDisplayName() + seperator + table.name;
            if (table.param != null && table.param.param != null)
            {
                s += "(";
                string sp = "";
                foreach (var p in table.param.param)
                {
                    if (sp != "")
                    {
                        sp += ", ";
                    }
                    sp += p.Key;
                    sp += "=";
                    sp += p.Value.GetValueString(0);
                }
                s += sp + ")";
            }
            return s;
        }
    }
    public class HEMemoryMap : HistoryEvent
    {
        public UIState.BaseMode mode;
        public HEMemoryMap(UIState.BaseMode mode)
        {
            this.mode = mode;
        }

        public override string ToString()
        {
            return mode.GetSheme().GetDisplayName() + seperator + "Memory Map";
        }
    }
    public class HETreeMap : HistoryEvent
    {
        public UIState.BaseMode mode;
        public Treemap.IMetricValue selected;
        public HETreeMap(UIState.BaseMode mode, Treemap.IMetricValue selected)
        {
            this.mode = mode;
            this.selected = selected;
        }

        public override string ToString()
        {
            return mode.GetSheme().GetDisplayName() + seperator + "Tree Map" + seperator + selected.GetName();
        }
    }

    public class History
    {
        public System.Collections.Generic.List<HistoryEvent> events = new System.Collections.Generic.List<HistoryEvent>();
        public int backCount = 0;
        public bool hasPresentEvent = false;
        public void Clear()
        {
            backCount = 0;
            hasPresentEvent = false;
            events.Clear();
        }

        protected int eventCount
        {
            get
            {
                if (hasPresentEvent) return events.Count;
                return events.Count + 1;
            }
        }
        public bool isPresent
        {
            get
            {
                return backCount == 0;
            }
        }
        public bool hasPast
        {
            get
            {
                return backCount + 1 < eventCount;
            }
        }
        public bool hasFuture
        {
            get
            {
                return backCount > 0;
            }
        }


        public void AddEvent(HistoryEvent e)
        {
            if (hasFuture)
            {
                //remove future
                var i = events.Count - backCount;
                events.RemoveRange(i, backCount);
            }
            backCount = 0;
            if (events.Count > 0)
            {
                var last = events[events.Count - 1];
                if (!last.Equals(e))
                {
                    events.Add(e);
                }
            }
            else
            {
                events.Add(e);
            }
            hasPresentEvent = false;
            //UnityEngine.Debug.Log("History add: " + e.ToString());
            //PrintHistory();
        }

        public void SetPresentEvent(HistoryEvent ePresent)
        {
            if (ePresent == null) return;
            events.Add(ePresent);
            hasPresentEvent = true;
        }

        public HistoryEvent Backward()
        {
            if (hasPast)
            {
                if (isPresent && !hasPresentEvent)
                {
                    //remove last event
                    int l = events.Count - 1;
                    var e = events[l];
                    events.RemoveAt(l);
                    return e;
                }
                else
                {
                    ++backCount;
                    var i = GetCurrent();
                    return events[i];
                }
            }

            return null;
        }

        public HistoryEvent Forward()
        {
            if (hasFuture)
            {
                --backCount;
                var i = GetCurrent();
                return events[i];
            }
            return null;
        }

        protected int GetCurrent()
        {
            return events.Count - 1 - backCount;
        }

        void PrintHistory()
        {
            string strOut = "";
            foreach (var e in events)
            {
                strOut += e.ToString() + "\n";
            }
            UnityEngine.Debug.Log(strOut);
        }
    }
}
