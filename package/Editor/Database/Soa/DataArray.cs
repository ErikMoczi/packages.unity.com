//#define PROFILER_DEBUG_TEST TODO: MOVE TESTS OUT TO THE TESING ASSEMBLY
using System;
using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.Database.Soa
{
    internal class DataArray
    {
        public static Cache<DataT[]> MakeCache<DataT>(SoaDataSet dataSet, DataSource<DataT[]> source) where DataT : IComparable
        {
            return new Cache<DataT[]>(dataSet, source);
        }

        public static Cache<DataT> MakeCache<DataT>(SoaDataSet dataSet, DataSource<DataT> source) where DataT : IComparable
        {
            return new Cache<DataT>(dataSet, source);
        }

        public static Column<DataT> MakeColumn<DataT>(SoaDataSet dataSet, DataSource<DataT> source) where DataT : System.IComparable
        {
            return MakeColumn(MakeCache(dataSet, source));
        }

        public static Column<DataT> MakeColumn<DataT>(Cache<DataT> source) where DataT : System.IComparable
        {
            switch (Type.GetTypeCode(typeof(DataT)))
            {
                case TypeCode.Int64:
                    return new ColumnLong(source as Cache<long>) as Column<DataT>;
                case TypeCode.Int32:
                    return new ColumnInt(source as Cache<int>) as Column<DataT>;
                default:
                    return new Column<DataT>(source);
            }
        }

        public static ColumnArray<DataT> MakeColumn<DataT>(DataT[] source) where DataT : System.IComparable
        {
            return new ColumnArray<DataT>(source);
        }

        public static Column_Transform<DataOutT, DataInT> MakeColumn_Transform<DataOutT, DataInT>(Cache<DataInT> cache, Column_Transform<DataOutT, DataInT>.Transformer transform, Column_Transform<DataOutT, DataInT>.Untransformer untransform)
            where DataInT : System.IComparable
            where DataOutT : System.IComparable
        {
            return new Column_Transform<DataOutT, DataInT>(cache, transform, untransform);
        }

        public class Cache<DataT>
        {
            public Cache(SoaDataSet dataSet, DataSource<DataT> source)
            {
                m_DataSet = dataSet;
                m_DataSource = source;
                chunkCount = (m_DataSet.DataCount + dataSet.ChunkSize - 1) / m_DataSet.ChunkSize;
                m_DataChunk = new DataChunk<DataT>[chunkCount];
            }

            public SoaDataSet m_DataSet;
            public DataSource<DataT> m_DataSource;
            public void IndexToChunckIndex(long index, out long chunkIndex, out long chunkSubIndex)
            {
                chunkIndex = index / m_DataSet.ChunkSize;
                chunkSubIndex = index % m_DataSet.ChunkSize;
            }

            public DataChunk<DataT> IndexToChunck(long index, out long chunkSubIndex)
            {
                long chunkIndex = index / m_DataSet.ChunkSize;
                chunkSubIndex = index % m_DataSet.ChunkSize;
                var chunk = GetChunk(chunkIndex);
                return chunk;
            }

            public DataT this[long i]
            {
                get
                {
                    long dataIndex;
                    var chunk = IndexToChunck(i, out dataIndex);
                    if (dataIndex < 0 || dataIndex >= chunk.m_Data.Length)
                    {
                        throw new Exception("out of bound");
                    }
                    return chunk.m_Data[dataIndex];
                }
                set
                {
                    long dataIndex;
                    var chunk = IndexToChunck(i, out dataIndex);
                    chunk.m_Data[dataIndex] = value;
                }
            }
            public long Length
            {
                get
                {
                    return m_DataSet.DataCount;
                }
            }
            public DataChunk<DataT> GetChunk(long chunkIndex)
            {
                if (m_DataChunk[chunkIndex] == null)
                {
                    using (Profiling.GetMarker(Profiling.MarkerId.NewDataChunk).Auto())
                    {
                        long indexFirst = chunkIndex * m_DataSet.ChunkSize;
                        long indexLast = indexFirst + m_DataSet.ChunkSize;
                        if (indexLast > m_DataSet.DataCount)
                        {
                            indexLast = m_DataSet.DataCount;
                        }
                        m_DataChunk[chunkIndex] = new DataChunk<DataT>(indexLast - indexFirst);
                        m_DataSource.Get(Range.FirstLast(indexFirst, indexLast), ref m_DataChunk[chunkIndex].m_Data);
                    }
                }
                return m_DataChunk[chunkIndex];
            }

            long chunkCount;
            DataChunk<DataT>[] m_DataChunk;


            public int FindIndex(Predicate<DataT> match)
            {
                for (int i = 0; i != chunkCount; ++i)
                {
                    int r = Array.FindIndex(GetChunk(i).m_Data, match);
                    if (r >= 0)
                    {
                        return (int)(i * m_DataSet.ChunkSize + r);
                    }
                }
                return -1;
            }

            public int[] FindAllIndex(Predicate<DataT> match)
            {
                var result = new List<int>();
                for (int i = 0; i != chunkCount; ++i)
                {
                    var c = GetChunk(i).m_Data;
                    for (int j = 0; j != c.Length; ++j)
                    {
                        if (match(c[j]))
                        {
                            result.Add((int)(i * m_DataSet.ChunkSize + j));
                        }
                    }
                }
                return result.ToArray();
            }
        }
        public class Column<DataT> : ColumnTyped<DataT> where DataT : IComparable
        {
#if MEMPROFILER_DEBUG_INFO
            public override string GetDebugString(long row)
            {
                return "SAO.Column<" + typeof(DataT).Name + ">[" + row + "]{" + GetRowValueString(row) + "}";
            }

#endif
            protected Cache<DataT> m_Cache;
            public Column(Cache<DataT> cache)
            {
                m_Cache = cache;
                type = typeof(DataT);
            }

            public override long GetRowCount()
            {
                return m_Cache.Length;
            }


            public int LowerBoundIndex(long[] index, int first, int count, IComparable v)
            {
                while (count > 0)
                {
                    int it = first;
                    int step = count / 2;
                    it += step;
                    if (m_Cache[index[it]].CompareTo(v) < 0)
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
            public int UpperBoundIndex(long[] index, int first, int count, IComparable v)
            {
                while (count > 0)
                {
                    int it = first;
                    int step = count / 2;
                    it += step;
                    if (v.CompareTo(m_Cache[index[it]]) >= 0)
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
            public override long[] GetMatchIndex(ArrayRange rowRange, Operation.Operator operation, Operation.Expression expression, long expressionRowFirst, bool rowToRow)
            {
                using (Profiling.GetMarker(Profiling.MarkerId.ColumnMatchQuery).Auto())
                {
                    Update();
                    long count = rowRange.indexCount;
                    var matchedIndices = new List<long>(128);
                    Operation.Operation.ComparableComparator comparator = Operation.Operation.GetComparator(type, expression.type);
                    if (rowToRow)
                    {
                        for (long i = 0; i != count; ++i)
                        {
                            var leftValue = m_Cache[rowRange[i]];
                            if (Operation.Operation.Match(operation, comparator, leftValue, expression, expressionRowFirst + i))
                            {
                                matchedIndices.Add(rowRange[i]);
                            }
                        }
                    }
                    else
                    {
                        if (Operation.Operation.IsOperatorOneToMany(operation))
                        {
                            for (int i = 0; i != count; ++i)
                            {
                                var leftValue = m_Cache[rowRange[i]];
                                if (Operation.Operation.Match(operation, comparator, leftValue, expression, expressionRowFirst))
                                {
                                    matchedIndices.Add(rowRange[i]);
                                }
                            }
                        }
                        else
                        {
                            var valueRight = expression.GetComparableValue(expressionRowFirst);
                            //Optimization for equal operation when querying on all data
                            if (rowRange.array == null && operation == Operation.Operator.Equal)
                            {
                                //use the sorted index to trim down invalid values
                                long[] sortedIndex = GetSortIndexAsc();
                                int lowerIndexIndex = LowerBoundIndex(sortedIndex, (int)rowRange.directIndexFirst, (int)rowRange.indexCount, valueRight);
                                int upperIndexIndex = (int)rowRange.directIndexLast;
                                for (int i = lowerIndexIndex; i < upperIndexIndex; ++i)
                                {
                                    if (m_Cache[sortedIndex[i]].CompareTo(valueRight) == 0)
                                    {
                                        matchedIndices.Add(sortedIndex[i]);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
#if PROFILER_DEBUG_TEST
                                {
                                    //Test to validate if optimization works
                                    int matchCount = 0;
                                    for (int i = 0; i != count; ++i)
                                    {
                                        var leftValue = m_Cache[rowRange[i]];
                                        if (Operation.Operation.Match(operation, comparator, leftValue, valueRight))
                                        {
                                            ++matchCount;
                                            if(matchedIndices.FindIndex(x => x == rowRange[i]) < 0)
                                            {
                                                UnityEngine.Debug.LogError("GetMatchIndex Optimization failure on index " + rowRange[i]);
                                            }
                                        }
                                    }
                                    if(matchCount != matchedIndices.Count)
                                    {
                                        UnityEngine.Debug.LogError("GetMatchIndex Optimization failure on match count " + matchCount + " != " + matchedIndices.Count);
                                    }
                                }
#endif

                            }
                            else
                            {
                                for (int i = 0; i != count; ++i)
                                {
                                    var leftValue = m_Cache[rowRange[i]];
                                    if (Operation.Operation.Match(operation, comparator, leftValue, valueRight))
                                    {
                                        matchedIndices.Add(rowRange[i]);
                                    }
                                }
                            }
                        }
                    }

                    var matchedIndicesArray = matchedIndices.ToArray();

#if MEMPROFILER_DEBUG_INFO
	            Algorithm.DebugLog("GetMatchIndex : indexCount " + rowRange.indexCount
	                + " op:" + Operation.Operation.OperatorToString(operation)
	                + " Exp():" + expression.GetValueString(expressionRowFirst)
	                + " expRowFirst:" + expressionRowFirst
	                + " Column:" + this.GetDebugString(rowRange[0])
	                + " Expression:" + expression.GetDebugString(expressionRowFirst)
	                + " Result Count:" + (matchedIndices != null ? matchedIndices.Length : 0));
#endif

                    return matchedIndicesArray;
                }
            }
            protected override long[] GetSortIndex(IComparer<DataT> comparer, ArrayRange indices, bool relativeIndex)
            {
                if (indices.array != null)
                {
                    return base.GetSortIndex(comparer, indices, relativeIndex);
                }
                long count = indices.indexCount;
                DataT[] k = new DataT[count];

                long firstChunkSubIndex = indices.directIndexFirst % m_Cache.m_DataSet.ChunkSize;
                long lastChunkSubIndex = indices.directIndexLast % m_Cache.m_DataSet.ChunkSize;

                long firstChunkIndex = indices.directIndexFirst / m_Cache.m_DataSet.ChunkSize;
                long lastChunkIndex = indices.directIndexLast / m_Cache.m_DataSet.ChunkSize;

                long firstChunkLength = m_Cache.m_DataSet.ChunkSize - firstChunkSubIndex;
                long lastChunkLength = lastChunkSubIndex;

                long firstFullChunk = firstChunkIndex + 1;
                long lastFullChunk = lastChunkIndex;
                long fullChunkCount = lastFullChunk - firstFullChunk;

                if (firstChunkLength > m_Cache.m_DataSet.DataCount)
                {
                    firstChunkLength = m_Cache.m_DataSet.DataCount;
                    lastChunkLength = 0;
                }
                //copy first chunk
                if (firstChunkLength > 0)
                {
                    var c = m_Cache.GetChunk(firstChunkIndex);
                    System.Array.Copy(c.m_Data, 0, k, 0, firstChunkLength);
                }

                //copy full chunks
                for (long i = 0; i < fullChunkCount; ++i)
                {
                    long chunkIndex = i + firstFullChunk;
                    long chunkRowFirst = chunkIndex * m_Cache.m_DataSet.ChunkSize;
                    var c = m_Cache.GetChunk(chunkIndex);
                    System.Array.Copy(c.m_Data, 0, k, chunkRowFirst - indices.directIndexFirst, m_Cache.m_DataSet.ChunkSize);
                }

                //copy last chunk
                if (lastChunkLength > 0)
                {
                    long chunkRowFirst = lastChunkIndex * m_Cache.m_DataSet.ChunkSize;
                    var c = m_Cache.GetChunk(lastChunkIndex);
                    System.Array.Copy(c.m_Data, 0, k, chunkRowFirst - indices.directIndexFirst, lastChunkLength);
                }

                //create index array
                long[] index = new long[count];
                if (relativeIndex)
                {
                    for (long i = 0; i != count; ++i)
                    {
                        index[i] = i;
                        k[i] = GetRowValue(i + indices.directIndexFirst);
                    }
                }
                else
                {
                    for (long i = 0; i != count; ++i)
                    {
                        index[i] = i + indices.directIndexFirst;
                        k[i] = GetRowValue(i + indices.directIndexFirst);
                    }
                }


#if (PROFILER_DEBUG_TEST)
                //Test if data is good against a simple un-optimized query loop

                for (long i = 0; i != count; ++i)
                {
                    var v = GetRowValue(i + indices.directIndexFirst);
                    long c = k[i].CompareTo(v);
                    UnityEngine.Debug.Assert(c == 0);
                }
#endif
                System.Array.Sort(k, index, comparer);
                return index;
            }

            public override string GetRowValueString(long row)
            {
                return m_Cache[row].ToString();
            }

            public override DataT GetRowValue(long row)
            {
                return m_Cache[row];
            }

            public override void SetRowValue(long row, DataT value)
            {
                m_Cache[row] = value;
            }

            //public override bool VisitRows(Visitor v, long[] indices, long firstIndex, long lastIndex)
            public override System.Collections.Generic.IEnumerable<DataT> VisitRows(ArrayRange indices)
            {
                for (long i = 0; i != indices.indexCount; ++i)
                {
                    yield return m_Cache[indices[i]];
                }
            }
        }


        public class ColumnLong : Column<long>
        {
            public ColumnLong(Cache<long> cache)
                :base(cache)
            {
            }

            public int LowerBoundIndex(long[] index, int first, int count, long v)
            {
                while (count > 0)
                {
                    int it = first;
                    int step = count / 2;
                    it += step;
                    if (m_Cache[index[it]] < v)
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
            public int UpperBoundIndex(long[] index, int first, int count, long v)
            {
                while (count > 0)
                {
                    int it = first;
                    int step = count / 2;
                    it += step;
                    if (v >= m_Cache[index[it]])
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
            public override long[] GetMatchIndex(ArrayRange rowRange, Operation.Operator operation, Operation.Expression expression, long expressionRowFirst, bool rowToRow)
            {
                if (expression.type != type)
                {
                    return base.GetMatchIndex(rowRange, operation, expression, expressionRowFirst, rowToRow);
                }
                
                using (Profiling.GetMarker(Profiling.MarkerId.ColumnMatchQueryLong).Auto())
                {
                    Update();
                    Operation.TypedExpression<long> typedExpression = expression as Operation.TypedExpression<long>;
                    long count = rowRange.indexCount;
                    var matchedIndices = new List<long>(128);
                    if (rowToRow)
                    {
                        for (long i = 0; i != count; ++i)
                        {
                            var lhs = m_Cache[rowRange[i]];
                            var rhs = typedExpression.GetValue(expressionRowFirst + i);
                            if (Operation.Operation.Match(operation, lhs, rhs))
                            {
                                matchedIndices.Add(rowRange[i]);
                            }
                        }
                    }
                    else
                    {
                        if (Operation.Operation.IsOperatorOneToMany(operation))
                        {
                            for (int i = 0; i != count; ++i)
                            {
                                var leftValue = m_Cache[rowRange[i]];
                                if (Operation.Operation.Match(operation, leftValue, typedExpression, expressionRowFirst))
                                {
                                    matchedIndices.Add(rowRange[i]);
                                }
                            }
                        }
                        else
                        {
                            var valueRight = typedExpression.GetValue(expressionRowFirst);
                            //Optimization for equal operation when querying on all data
                            if (rowRange.array == null && operation == Operation.Operator.Equal)
                            {
                                //use the sorted index to trim down invalid values
                                long[] sortedIndex = GetSortIndexAsc();
                                int lowerIndexIndex = LowerBoundIndex(sortedIndex, (int)rowRange.directIndexFirst, (int)rowRange.indexCount, valueRight);
                                int upperIndexIndex = (int)rowRange.directIndexLast;
                                for (int i = lowerIndexIndex; i < upperIndexIndex; ++i)
                                {
                                    if (m_Cache[sortedIndex[i]] == valueRight)
                                    {
                                        matchedIndices.Add(sortedIndex[i]);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
#if PROFILER_DEBUG_TEST
                                {
                                    //Test to validate if optimization works
                                    int matchCount = 0;
                                    for (int i = 0; i != count; ++i)
                                    {
                                        var leftValue = m_Cache[rowRange[i]];
                                        if (Operation.Operation.Match(operation, leftValue, valueRight))
                                        {
                                            ++matchCount;
                                            if(matchedIndices.FindIndex(x => x == rowRange[i]) < 0)
                                            {
                                                UnityEngine.Debug.LogError("GetMatchIndex Optimization failure on index " + rowRange[i]);
                                            }
                                        }
                                    }
                                    if(matchCount != matchedIndices.Count)
                                    {
                                        UnityEngine.Debug.LogError("GetMatchIndex Optimization failure on match count " + matchCount + " != " + matchedIndices.Count);
                                    }
                                }
#endif
                            }
                            else
                            {
                                for (int i = 0; i != count; ++i)
                                {
                                    var leftValue = m_Cache[rowRange[i]];
                                    if (Operation.Operation.Match(operation, leftValue, valueRight))
                                    {
                                        matchedIndices.Add(rowRange[i]);
                                    }
                                }
                            }
                        }
                    }
                    var matchedIndicesArray = matchedIndices.ToArray();

#if MEMPROFILER_DEBUG_INFO
	            Algorithm.DebugLog("GetMatchIndex : indexCount " + rowRange.indexCount
	                + " op:" + Operation.Operation.OperatorToString(operation)
	                + " Exp():" + expression.GetValueString(expressionRowFirst)
	                + " expRowFirst:" + expressionRowFirst
	                + " Column:" + this.GetDebugString(rowRange[0])
	                + " Expression:" + expression.GetDebugString(expressionRowFirst)
	                + " Result Count:" + (matchedIndices != null ? matchedIndices.Length : 0));
#endif

                    return matchedIndicesArray;
                }
            }
        }

        public class ColumnInt : Column<int>
        {
            public ColumnInt(Cache<int> cache)
                : base(cache)
            {
            }

            public int LowerBoundIndex(long[] index, int first, int count, int v)
            {
                while (count > 0)
                {
                    int it = first;
                    int step = count / 2;
                    it += step;
                    if (m_Cache[index[it]] < v)
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
            public int UpperBoundIndex(long[] index, int first, int count, int v)
            {
                while (count > 0)
                {
                    int it = first;
                    int step = count / 2;
                    it += step;
                    if (v >= m_Cache[index[it]])
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
            public override long[] GetMatchIndex(ArrayRange rowRange, Operation.Operator operation, Operation.Expression expression, long expressionRowFirst, bool rowToRow)
            {
                if (expression.type != type)
                {
                    return base.GetMatchIndex(rowRange, operation, expression, expressionRowFirst, rowToRow);
                }
                using (Profiling.GetMarker(Profiling.MarkerId.ColumnMatchQueryInt).Auto())
                {
                    Update();
                    Operation.TypedExpression<int> typedExpression = expression as Operation.TypedExpression<int>;
                    long count = rowRange.indexCount;
                    var matchedIndices = new List<long>(128);
                    if (rowToRow)
                    {
                        for (long i = 0; i != count; ++i)
                        {
                            var lhs = m_Cache[rowRange[i]];
                            var rhs = typedExpression.GetValue(expressionRowFirst + i);
                            if (Operation.Operation.Match(operation, lhs, rhs))
                            {
                                matchedIndices.Add(rowRange[i]);
                            }
                        }
                    }
                    else
                    {
                        if (Operation.Operation.IsOperatorOneToMany(operation))
                        {
                            for (int i = 0; i != count; ++i)
                            {
                                var leftValue = m_Cache[rowRange[i]];
                                if (Operation.Operation.Match(operation, leftValue, typedExpression, expressionRowFirst))
                                {
                                    matchedIndices.Add(rowRange[i]);
                                }
                            }
                        }
                        else
                        {
                            var valueRight = typedExpression.GetValue(expressionRowFirst);
                            //Optimization for equal operation when querying on all data
                            if (rowRange.array == null && operation == Operation.Operator.Equal)
                            {
                                //use the sorted index to trim down invalid values
                                long[] sortedIndex = GetSortIndexAsc();
                                int lowerIndexIndex = LowerBoundIndex(sortedIndex, (int)rowRange.directIndexFirst, (int)rowRange.indexCount, valueRight);
                                int upperIndexIndex = (int)rowRange.directIndexLast;
                                for (int i = lowerIndexIndex; i < upperIndexIndex; ++i)
                                {
                                    if (m_Cache[sortedIndex[i]] == valueRight)
                                    {
                                        matchedIndices.Add(sortedIndex[i]);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
#if PROFILER_DEBUG_TEST
                                {
                                    //Test to validate if optimization works
                                    int matchCount = 0;
                                    for (int i = 0; i != count; ++i)
                                    {
                                        var leftValue = m_Cache[rowRange[i]];
                                        if (Operation.Operation.Match(operation, leftValue, valueRight))
                                        {
                                            ++matchCount;
                                            if(matchedIndices.FindIndex(x => x == rowRange[i]) < 0)
                                            {
                                                UnityEngine.Debug.LogError("GetMatchIndex Optimization failure on index " + rowRange[i]);
                                            }
                                        }
                                    }
                                    if(matchCount != matchedIndices.Count)
                                    {
                                        UnityEngine.Debug.LogError("GetMatchIndex Optimization failure on match count " + matchCount + " != " + matchedIndices.Count);
                                    }
                                }
#endif
                            }
                            else
                            {
                                for (int i = 0; i != count; ++i)
                                {
                                    var leftValue = m_Cache[rowRange[i]];
                                    if (Operation.Operation.Match(operation, leftValue, valueRight))
                                    {
                                        matchedIndices.Add(rowRange[i]);
                                    }
                                }
                            }
                        }
                    }

                    var matchedIndicesArray = matchedIndices.ToArray();

#if MEMPROFILER_DEBUG_INFO
	            Algorithm.DebugLog("GetMatchIndex : indexCount " + rowRange.indexCount
	                + " op:" + Operation.Operation.OperatorToString(operation)
	                + " Exp():" + expression.GetValueString(expressionRowFirst)
	                + " expRowFirst:" + expressionRowFirst
	                + " Column:" + this.GetDebugString(rowRange[0])
	                + " Expression:" + expression.GetDebugString(expressionRowFirst)
	                + " Result Count:" + (matchedIndices != null ? matchedIndices.Length : 0));
#endif

                    return matchedIndicesArray;
                }
            }
        }

        public class Column_Transform<DataOutT, DataInT> : Database.ColumnTyped<DataOutT> where DataOutT : System.IComparable where DataInT : System.IComparable
        {
#if MEMPROFILER_DEBUG_INFO
            public override string GetDebugString(long row)
            {
                return "SAO.Column_Transform<" + typeof(DataOutT).Name + "," + typeof(DataInT).Name + ">[" + row + "]{" + GetRowValueString(row) + "}";
            }

#endif
            protected Cache<DataInT> m_Cache;
            public delegate DataOutT Transformer(DataInT a);
            public delegate void Untransformer(ref DataInT a, DataOutT b);

            Transformer m_Transformer;
            Untransformer m_Untransformer;
            public Column_Transform(Cache<DataInT> cache, Transformer transformer, Untransformer untransformer)
            {
                m_Cache = cache;
                m_Transformer = transformer;
                m_Untransformer = untransformer;
                type = typeof(DataOutT);
            }

            public override long GetRowCount()
            {
                return m_Cache.Length;
            }

            public override string GetRowValueString(long row)
            {
                return m_Transformer(m_Cache[row]).ToString();
            }

            public override DataOutT GetRowValue(long row)
            {
                return m_Transformer(m_Cache[row]);
            }

            public override void SetRowValue(long row, DataOutT value)
            {
                if (m_Untransformer != null)
                {
                    long subIndex;
                    var c = m_Cache.IndexToChunck(row, out subIndex);
                    m_Untransformer(ref c.m_Data[subIndex], value);
                }
            }

            public override System.Collections.Generic.IEnumerable<DataOutT> VisitRows(ArrayRange indices)
            {
                for (long i = 0; i != indices.indexCount; ++i)
                {
                    yield return m_Transformer(m_Cache[indices[i]]);
                }
            }
        }


        public class ColumnArray<DataT> : ColumnTyped<DataT> where DataT : IComparable
        {
#if MEMPROFILER_DEBUG_INFO
            public override string GetDebugString(long row)
            {
                return "SAO.ColumnArray<" + typeof(DataT).Name + ">[" + row + "]{" + GetRowValueString(row) + "}";
            }

#endif
            protected DataT[] m_Data;
            public ColumnArray(DataT[] data)
            {
                m_Data = data;
                type = typeof(DataT);
            }

            public override long GetRowCount()
            {
                return m_Data.LongLength;
            }

            protected override long[] GetSortIndex(IComparer<DataT> comparer, ArrayRange indices, bool relativeIndex)
            {
                if (indices.array != null)
                {
                    return base.GetSortIndex(comparer, indices, relativeIndex);
                }
                long count = indices.indexCount;
                DataT[] k = new DataT[count];
                System.Array.Copy(m_Data, indices.directIndexFirst, k, 0, count);

                //create index array
                long[] index = new long[count];
                if (relativeIndex)
                {
                    for (long i = 0; i != count; ++i)
                    {
                        index[i] = i;
                    }
                }
                else
                {
                    for (long i = 0; i != count; ++i)
                    {
                        index[i] = i + indices.directIndexFirst;
                    }
                }

                System.Array.Sort(k, index, comparer);
                return index;
            }

            public override string GetRowValueString(long row)
            {
                return m_Data[row].ToString();
            }

            public override DataT GetRowValue(long row)
            {
                return m_Data[row];
            }

            public override void SetRowValue(long row, DataT value)
            {
                m_Data[row] = value;
            }

            public override System.Collections.Generic.IEnumerable<DataT> VisitRows(ArrayRange indices)
            {
                for (long i = 0; i != indices.indexCount; ++i)
                {
                    yield return m_Data[indices[i]];
                }
            }
        }
    }
}
