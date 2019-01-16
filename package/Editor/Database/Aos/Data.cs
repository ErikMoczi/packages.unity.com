using System;
using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.Database.Aos
{
    internal class Data
    {
        public static Column<StructT, DataT> MakeColumn<StructT, DataT>(StructT[] array, Column<StructT, DataT>.Getter getter, Column<StructT, DataT>.Setter setter) where DataT : IComparable, new()
        {
            return new Column<StructT, DataT>(array, getter, setter);
        }

        public static Column<StructT, DataT> MakeColumn<StructT, DataT>(StructT[] array, Column<StructT, DataT>.Getter getter) where DataT : IComparable, new()
        {
            return new Column<StructT, DataT>(array, getter, (ref StructT o, DataT v) => { throw new Exception("Cannot set value on this column"); });
        }

        internal class Column<StructT, DataT> : Database.ColumnTyped<DataT> where DataT : System.IComparable, new()
        {
#if MEMPROFILER_DEBUG_INFO
            public override string GetDebugString(long row)
            {
                return "AOS.Column<" + typeof(StructT).Name + ", " + typeof(DataT).Name + ">[" + row + "]{" + GetRowValueString(row) + "}";
            }

#endif
            public delegate void Setter(ref StructT s, DataT v);
            public delegate DataT Getter(StructT s);
            Setter setter;
            Getter getter;
            StructT[] array;
            public Column(StructT[] array, Getter getter, Setter setter)
            {
                type = typeof(DataT);
                this.array = array;
                this.getter = getter;
                this.setter = setter;
            }

            public override long GetRowCount()
            {
                return array.Length;
            }

            public override string GetRowValueString(long row)
            {
                return getter(array[row]).ToString();
            }

            public override DataT GetRowValue(long row)
            {
                return getter(array[row]);
            }

            public override void SetRowValue(long row, DataT value)
            {
                setter(ref array[row], value);
            }

            public override System.Collections.Generic.IEnumerable<DataT> VisitRows(ArrayRange indices)
            {
                for (long i = 0; i != indices.indexCount; ++i)
                {
                    yield return getter(array[indices[i]]);
                }
            }
        }

        public static ColumnList<StructT, DataT> MakeColumn<StructT, DataT>(List<StructT> list, ColumnList<StructT, DataT>.Getter getter, ColumnList<StructT, DataT>.Setter setter) where DataT : IComparable, new()
        {
            return new ColumnList<StructT, DataT>(list, getter, setter);
        }

        public static ColumnList<StructT, DataT> MakeColumn<StructT, DataT>(List<StructT> list, ColumnList<StructT, DataT>.Getter getter) where DataT : IComparable, new()
        {
            return new ColumnList<StructT, DataT>(list, getter, (List<StructT> l, int index, DataT v) => { throw new Exception("Cannot set value on this column"); });
        }

        internal class ColumnList<StructT, DataT> : Database.ColumnTyped<DataT> where DataT : System.IComparable, new()
        {
#if MEMPROFILER_DEBUG_INFO
            public override string GetDebugString(long row)
            {
                return "AOS.ColumnList<" + typeof(StructT).Name + ", " + typeof(DataT).Name + ">[" + row + "]{" + GetRowValueString(row) + "}";
            }

#endif
            public delegate void Setter(List<StructT> list, int index, DataT v);
            public delegate DataT Getter(StructT s);
            Setter setter;
            Getter getter;
            List<StructT> list;
            public ColumnList(List<StructT> list, Getter getter, Setter setter)
            {
                type = typeof(DataT);
                this.list = list;
                this.getter = getter;
                this.setter = setter;
            }

            public override long GetRowCount()
            {
                return list.Count;
            }

            public override string GetRowValueString(long row)
            {
                return getter(list[(int)row]).ToString();
            }

            public override DataT GetRowValue(long row)
            {
                return getter(list[(int)row]);
            }

            public override void SetRowValue(long row, DataT value)
            {
                setter(list, (int)row, value);
            }

            public override System.Collections.Generic.IEnumerable<DataT> VisitRows(ArrayRange indices)
            {
                for (long i = 0; i != indices.indexCount; ++i)
                {
                    yield return getter(list[(int)indices[i]]);
                }
            }
        }
    }
}
