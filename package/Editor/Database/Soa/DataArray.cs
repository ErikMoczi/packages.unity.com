using System;
using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.Database.Soa
{
    public class DataArray
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
            return new Column<DataT>(MakeCache(dataSet, source));
        }

        public static Column<DataT> MakeColumn<DataT>(Cache<DataT> source) where DataT : System.IComparable
        {
            return new Column<DataT>(source);
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
                chunkCount = (m_DataSet.m_dataCount + dataSet.m_chunkSize - 1) / m_DataSet.m_chunkSize;
                m_DataChunk = new DataChunk<DataT>[chunkCount];
            }

            public SoaDataSet m_DataSet;
            public DataSource<DataT> m_DataSource;
            public void IndexToChunckIndex(long index, out long chunkIndex, out long chunkSubIndex)
            {
                chunkIndex = index / m_DataSet.m_chunkSize;
                chunkSubIndex = index % m_DataSet.m_chunkSize;
            }

            public DataChunk<DataT> IndexToChunck(long index, out long chunkSubIndex)
            {
                long chunkIndex = index / m_DataSet.m_chunkSize;
                chunkSubIndex = index % m_DataSet.m_chunkSize;
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
                    return m_DataSet.m_dataCount;
                }
            }
            public DataChunk<DataT> GetChunk(long chunkIndex)
            {
                if (m_DataChunk[chunkIndex] == null)
                {
                    long indexFirst = chunkIndex * m_DataSet.m_chunkSize;
                    long indexLast = indexFirst + m_DataSet.m_chunkSize;
                    if (indexLast > m_DataSet.m_dataCount)
                    {
                        indexLast = m_DataSet.m_dataCount;
                    }
                    m_DataChunk[chunkIndex] = new DataChunk<DataT>(indexLast - indexFirst);
                    m_DataSource.Get(Range.FirstLast(indexFirst, indexLast), ref m_DataChunk[chunkIndex].m_Data);
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
                        return (int)(i * m_DataSet.m_chunkSize + r);
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
                            result.Add((int)(i * m_DataSet.m_chunkSize + j));
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

            protected override long[] GetSortIndex(IComparer<DataT> comparer, ArrayRange indices, bool relativeIndex)
            {
                if (indices.array != null)
                {
                    return base.GetSortIndex(comparer, indices, relativeIndex);
                }
                long count = indices.indexCount;
                DataT[] k = new DataT[count];

                long firstChunkSubIndex = indices.directIndexFirst % m_Cache.m_DataSet.m_chunkSize;
                long lastChunkSubIndex = indices.directIndexLast % m_Cache.m_DataSet.m_chunkSize;

                long firstChunkIndex = indices.directIndexFirst / m_Cache.m_DataSet.m_chunkSize;
                long lastChunkIndex = indices.directIndexLast / m_Cache.m_DataSet.m_chunkSize;

                long firstChunkLength = m_Cache.m_DataSet.m_chunkSize - firstChunkSubIndex;
                long lastChunkLength = lastChunkSubIndex;

                long firstFullChunk = firstChunkIndex + 1;
                long lastFullChunk = lastChunkIndex;
                long fullChunkCount = lastFullChunk - firstFullChunk;

                if (firstChunkLength > m_Cache.m_DataSet.m_dataCount)
                {
                    firstChunkLength = m_Cache.m_DataSet.m_dataCount;
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
                    long chunkRowFirst = chunkIndex * m_Cache.m_DataSet.m_chunkSize;
                    var c = m_Cache.GetChunk(chunkIndex);
                    System.Array.Copy(c.m_Data, 0, k, chunkRowFirst - indices.directIndexFirst, m_Cache.m_DataSet.m_chunkSize);
                }

                //copy last chunk
                if (lastChunkLength > 0)
                {
                    long chunkRowFirst = lastChunkIndex * m_Cache.m_DataSet.m_chunkSize;
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
                    Debug.Assert(c == 0);
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
