using System;
using Unity.MemoryProfiler.Editor.Database.Operation;

namespace Unity.MemoryProfiler.Editor.Database
{
    internal abstract class Column
    {
        public Type type;
        public abstract long[] GetSortIndex(SortOrder order, ArrayRange indices, bool relativeIndex);

        // equality is an array the same size as the returned index. the value is the index that it's equal to.
        // so that R[x] == R[equality[x]]
        // If equality[x] == x then it is unique
        // if equality == null then all entries are considered equal
        public virtual long[] GetSortIndexAndEquality(SortOrder order, ArrayRange indices, bool relativeIndex, out long[] equality)
        {
            equality = null;
            return GetSortIndex(order, indices, relativeIndex);
        }

        //call this to get a displayable value
        public abstract string GetRowValueString(long row);
        public abstract int CompareRow(long rowA, long rowB);
        public abstract int Compare(long rowLhs, Operation.Expression exp, long rowRhs);
        //returning indice array is always in ascending index order
        public abstract long[] GetMatchIndex(ArrayRange rowRange, Operation.Operator op, Operation.Expression exp, long expRowFirst, bool rowToRow);
        public abstract long[] GetMatchIndex(ArrayRange indices, Operation.Matcher matcher);
        public abstract long GetFirstMatchIndex(Operation.Operator op, Operation.Expression exp, long expRowFirst);

        public virtual LinkRequest GetRowLink(long row) { return null; }

        // will return -1 if the underlying data has not been computed yet.
        // ComputeRowCount or Update should be called at least once before getting accurate row count
        public abstract long GetRowCount();

        // Update is provided to offset the load of computing the table's data outside the table's construction
        // return if anything changed
        public virtual bool Update() { return false; }

        // ComputeRowCount is provided to offset the load of computing the table's data outside the table's construction
        // return if new row count was computed
        public virtual bool ComputeRowCount() { return false; }
#if MEMPROFILER_DEBUG_INFO
        // output a string representing the underlying structure of the column
        public abstract string GetDebugString(long row);
#endif
    }

    internal interface IColumnDecorator
    {
        Column GetBaseColumn();
    }
}
