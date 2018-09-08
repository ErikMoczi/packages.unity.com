using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.Database
{
    namespace Operation
    {
        namespace Filter
        {
            public class DefaultSort : Filter
            {
                public Sort defaultSort;
                public Sort overrideSort;

                public override Filter Clone(FilterCloning fc)
                {
                    DefaultSort o = new DefaultSort();
                    o.defaultSort = (Sort)fc.CloneUnique(defaultSort);
                    if (overrideSort != null) o.overrideSort = (Sort)overrideSort.Clone(fc);
                    return o;
                }

                public override Database.Table CreateFilter(Database.Table tableIn)
                {
                    if (overrideSort != null) return overrideSort.CreateFilter(tableIn);
                    if (defaultSort != null) return defaultSort.CreateFilter(tableIn);
                    return tableIn;
                }

                public override Database.Table CreateFilter(Database.Table tableIn, ArrayRange range)
                {
                    if (overrideSort != null) return overrideSort.CreateFilter(tableIn, range);
                    if (defaultSort != null) return defaultSort.CreateFilter(tableIn, range);
                    return new Database.Operation.IndexedTable(tableIn, range);
                }

                public override IEnumerable<Filter> SubFilters()
                {
                    if (overrideSort != null)
                    {
                        yield return overrideSort;
                    }
                    else
                    {
                        yield return defaultSort;
                    }
                }

                public override bool RemoveSubFilters(Filter f)
                {
                    if (f == overrideSort)
                    {
                        overrideSort = null;
                        return true;
                    }
                    return false;
                }

                public override bool ReplaceSubFilters(Filter replaced, Filter with)
                {
                    if (replaced == overrideSort)
                    {
                        if (with is Sort)
                        {
                            overrideSort = (Sort)with;
                            return true;
                        }
                    }
                    return false;
                }

                //Filter to replace with when it gets removed
                public override Filter GetSurrogate() { return null; }

                //return if the filter must be deleted
                public override bool OnGui(Database.Table sourceTable, ref bool dirty)
                {
                    return false;
                }

                public override void UpdateColumnState(Database.Table sourceTable, ColumnState[] colState)
                {
                    if (overrideSort != null)
                    {
                        overrideSort.UpdateColumnState(sourceTable, colState);
                    }
                    else if (defaultSort != null)
                    {
                        foreach (var l in defaultSort.sortLevel)
                        {
                            colState[l.GetColumnIndex(sourceTable)].defaultSorted = l.order;
                        }
                    }
                }

                public override bool Simplify(ref bool dirty)
                {
                    if (overrideSort != null)
                    {
                        overrideSort.Simplify(ref dirty);
                    }
                    return false;
                }
            }
        }
    }
}
