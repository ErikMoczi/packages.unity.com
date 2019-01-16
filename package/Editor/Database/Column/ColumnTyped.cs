using System;
using Unity.MemoryProfiler.Editor.Database.Operation;

namespace Unity.MemoryProfiler.Editor.Database
{
    internal abstract class ColumnTyped<DataT> : Column where DataT : System.IComparable
    {
        protected long[] m_SortIndexAsc;
        protected bool m_IsDataNullable;
        public ColumnTyped()
        {
            var type = typeof(DataT);
            m_IsDataNullable = Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
        }

        //Call this to get a value to do computation with. May differ from GetRowValueString
        public abstract DataT GetRowValue(long row);
        public abstract void SetRowValue(long row, DataT value);
        public virtual string ValueToString(DataT a)
        {
            return a.ToString();
        }

        public virtual System.Collections.Generic.IEnumerable<DataT> VisitRows(ArrayRange ar)
        {
            for (long i = 0; i != ar.indexCount; ++i)
            {
                yield return GetRowValue(ar[i]);
            }
        }

        public override string GetRowValueString(long row)
        {
            if (row >= GetRowCount())
            {
                return "Out of Range";
            }
            return ValueToString(GetRowValue(row));
        }

        public override int CompareRow(long rowA, long rowB)
        {
            var a = GetRowValue(rowA);
            var b = GetRowValue(rowB);
            if (m_IsDataNullable)
            {
                if (a != null && b == null)
                    return 1;
                else if (a == null && b != null)
                    return -1;
                else if (a == null && b == null)
                    return 0;
            }
            return a.CompareTo(b);
        }

        protected long[] GetSortIndexAsc()
        {
            if (m_SortIndexAsc == null)
            {
                m_SortIndexAsc = GetSortIndex(Operation.Comparer.Ascending<DataT>(), new ArrayRange(0, GetRowCount()), false);
            }
            return m_SortIndexAsc;
        }

        public override long[] GetSortIndex(SortOrder order, ArrayRange indices, bool relativeIndex)
        {
            switch (order)
            {
                case SortOrder.Ascending:
                    return GetSortIndex(Operation.Comparer.Ascending<DataT>(), indices, relativeIndex);
                case SortOrder.Descending:
                    return GetSortIndex(Operation.Comparer.Descending<DataT>(), indices, relativeIndex);
            }
            throw new System.Exception("Bad SordOrder");
        }

        protected virtual long[] GetSortIndex(System.Collections.Generic.IComparer<DataT> comparer, ArrayRange indices, bool relativeIndex)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.Sort).Auto())
            {
                Update();
                long count = indices.indexCount;
                DataT[] keys = new DataT[count];


                //create index array
                long[] index = new long[count];
                if (relativeIndex)
                {
                    for (long i = 0; i != count; ++i)
                    {
                        index[i] = i;
                        keys[i] = GetRowValue(indices[i]);
                    }
                }
                else
                {
                    for (long i = 0; i != count; ++i)
                    {
                        index[i] = indices[i];
                        keys[i] = GetRowValue(indices[i]);
                    }
                }

                using (Profiling.GetMarker(Profiling.MarkerId.ArraySort).Auto())
                {
                    System.Array.Sort(keys, index, comparer);
                }
                return index;
            }
        }

        public override long[] GetMatchIndex(ArrayRange rowRange, Operation.Operator operation, Operation.Expression expression, long expressionRowFirst, bool rowToRow)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ColumnMatchQuery).Auto())
            {
                Update();
                long count = rowRange.indexCount;
                long[] matchedIndices = new long[count];
                long indexOflastMatchedIndex = 0;
                Operation.Operation.ComparableComparator comparator = Operation.Operation.GetComparator(type, expression.type);
                if (rowToRow)
                {
                    for (long i = 0; i != count; ++i)
                    {
                        var leftValue = GetRowValue(rowRange[i]);
                        if (Operation.Operation.Match(operation, comparator, leftValue, expression, expressionRowFirst + i))
                        {
                            matchedIndices[indexOflastMatchedIndex] = rowRange[i];
                            ++indexOflastMatchedIndex;
                        }
                    }
                }
                else
                {
                    if (Operation.Operation.IsOperatorOneToMany(operation))
                    {
                        for (int i = 0; i != count; ++i)
                        {
                            var leftValue = GetRowValue(rowRange[i]);
                            if (Operation.Operation.Match(operation, comparator, leftValue, expression, expressionRowFirst))
                            {
                                matchedIndices[indexOflastMatchedIndex] = rowRange[i];
                                ++indexOflastMatchedIndex;
                            }
                        }
                    }
                    else
                    {
                        var valueRight = expression.GetComparableValue(expressionRowFirst);
                        for (int i = 0; i != count; ++i)
                        {
                            var leftValue = GetRowValue(rowRange[i]);
                            if (Operation.Operation.Match(operation, comparator, leftValue, valueRight))
                            {
                                matchedIndices[indexOflastMatchedIndex] = rowRange[i];
                                ++indexOflastMatchedIndex;
                            }
                        }
                    }
                }

                if (indexOflastMatchedIndex != count)
                {
                    System.Array.Resize(ref matchedIndices, (int)indexOflastMatchedIndex);
                }

#if MEMPROFILER_DEBUG_INFO
	            Algorithm.DebugLog("GetMatchIndex : indexCount " + rowRange.indexCount
	                + " op:" + Operation.Operation.OperatorToString(operation)
	                + " Exp():" + expression.GetValueString(expressionRowFirst)
	                + " expRowFirst:" + expressionRowFirst
	                + " Column:" + this.GetDebugString(rowRange[0])
	                + " Expression:" + expression.GetDebugString(expressionRowFirst)
	                + " Result Count:" + (matchedIndices != null ? matchedIndices.Length : 0));
#endif

                return matchedIndices;
            }
        }

        public override long[] GetMatchIndex(ArrayRange indices, Operation.Matcher matcher)
        {
            var expression = new Operation.ExpColumn<DataT>(this);
            var result = matcher.GetMatchIndex(expression, indices);

#if MEMPROFILER_DEBUG_INFO
            Algorithm.DebugLog("GetMatchIndex : indexCount " + indices.indexCount
                + " matcher: " + matcher.GetHashCode()
                + " Result Count:" + (result != null ? result.Length : 0));
#endif
            return result;
        }

        private long LowerBound(IComparable key, Operation.Operation.ComparableComparator comparator)
        {
            long[] sortedIndex = GetSortIndexAsc();
            long step;
            long first = 0;
            long count = sortedIndex.Length;
            while (count > 0)
            {
                long it = first;
                step = count / 2;
                it += step;
                var val = GetRowValue(sortedIndex[it]);
                int result = comparator(val, key);
                if (result < 0)
                {
                    first = it + 1;
                    count -= step + 1;
                }
                else
                {
                    count = step;
                }
            }
            return first;
        }

        private long UpperBound(IComparable key, Operation.Operation.ComparableComparator comparator)
        {
            long[] sortedIndex = GetSortIndexAsc();
            long first = 0;
            long count = sortedIndex.Length;
            while (count > 0)
            {
                long it = first;
                long step = count / 2;
                it += step;

                var value = GetRowValue(sortedIndex[it]);
                int result = comparator(key, value);
                if (result >= 0)
                {
                    first = it + 1;
                    count -= step + 1;
                }
                else
                {
                    count = step;
                }
            }
            return first;
        }

        public override long GetFirstMatchIndex(Operation.Operator operation, Operation.Expression expression, long expRowFirst)
        {
            using (Profiling.GetMarker(Profiling.MarkerId.ColumnFirstMatchQuery).Auto())
            {
                Update();
                long[] sortedIndex = GetSortIndexAsc();

                Operation.Operation.ComparableComparator comparator = Operation.Operation.GetComparator(type, expression.type);
                var val2 = expression.GetComparableValue(expRowFirst);

                long firstMatchIndex = -1;
                switch (operation)
                {
                    case Operation.Operator.Less:
                        {
                            long iFirst = sortedIndex[0];
                            var val1 = GetRowValue(iFirst);
                            int result = comparator(val1, val2);
                            if (result < 0)
                            {
                                firstMatchIndex = iFirst;
                            }
                            break;
                        }
                    case Operation.Operator.LessEqual:
                        {
                            long iFirst = sortedIndex[0];
                            var val1 = GetRowValue(iFirst);
                            int result = comparator(val1, val2);
                            if (result <= 0)
                            {
                                firstMatchIndex = iFirst;
                            }
                            break;
                        }
                    case Operation.Operator.Equal:
                        {
                            long iFirstGreaterEqual = LowerBound(val2, comparator);
                            if (iFirstGreaterEqual < sortedIndex.Length)
                            {
                                long index = sortedIndex[iFirstGreaterEqual];
                                var val1 = GetRowValue(index);
                                int comparisonResult = comparator(val1, val2);
                                if (comparisonResult == 0)
                                {
                                    firstMatchIndex = index;
                                }
                            }
                            break;
                        }
                    case Operation.Operator.GreaterEqual:
                        {
                            long iFirstGreaterEqual = LowerBound(val2, comparator);
                            if (iFirstGreaterEqual < sortedIndex.Length)
                            {
                                firstMatchIndex = sortedIndex[iFirstGreaterEqual];
                            }
                            break;
                        }
                    case Operation.Operator.Greater:
                        {
                            long iFirstGreater = UpperBound(val2, comparator);
                            if (iFirstGreater < sortedIndex.Length)
                            {
                                firstMatchIndex = sortedIndex[iFirstGreater];
                            }
                            break;
                        }
                }
#if (PROFILER_DEBUG_TEST)

	            {
	                long count = sortedIndex.Length;
	                long resultTest = -1;
	                for (long i = 0; i != count; ++i)
	                {
	                    long index = sortedIndex[i];
	                    var val = GetRowValue(index);
	                    int comparisonResult = comparator(val, val2);
	                    bool matchResult = Operation.Operation.Match(operation, comparisonResult);
	                    if (matchResult)
	                    {
	                        resultTest = index;
	                        break;
	                    }
	                }
	                UnityEngine.Debug.Assert(resultTest == firstMatchIndex);
	                if (resultTest >= 0)
	                {
	                    var val1 = GetRowValue(resultTest);
	                    int comparisonResult = comparator(val1, val2);
	                    bool resultMatch = Operation.Operation.Match(operation, comparisonResult);
	                    UnityEngine.Debug.Assert(resultMatch);
	                }
	            }
#endif

#if MEMPROFILER_DEBUG_INFO
	            Algorithm.DebugLog("GetFirstMatchIndex :"
	                + " op:" + Operation.Operation.OperatorToString(operation)
	                + " Exp():" + expression.GetValueString(expRowFirst)
	                + " expRowFirst:" + expRowFirst
	                + " Column:" + this.GetDebugString(0)
	                + " Expression:" + expression.GetDebugString(expRowFirst)
	                + " Result:" + firstMatchIndex);
#endif
                return firstMatchIndex;
            }
        }

        public override int Compare(long rowLhs, Operation.Expression expression, long rowRhs)
        {
            Operation.Operation.ComparableComparator comparator = Operation.Operation.GetComparator(type, expression.type);
            var val1 = GetRowValue(rowLhs);
            var val2 = expression.GetComparableValue(rowRhs);
            int result = comparator(val1, val2);
            return result;
        }
    }
}
