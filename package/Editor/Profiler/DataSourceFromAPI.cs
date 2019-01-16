using System;
using UnityEditor.Profiling.Memory.Experimental;

namespace Unity.MemoryProfiler.Editor
{
    internal class DataSourceFromAPI
    {
        public class Adaptor<DataT> : Database.Soa.DataSource<DataT>
        {
            private ArrayEntries<DataT> m_Array;
            public Adaptor(ArrayEntries<DataT> array)
            {
                m_Array = array;
            }

            public override void Get(Range range, ref DataT[] dataOut)
            {
                m_Array.GetEntries((uint)range.first, (uint)range.length, ref dataOut);
            }
        }
        public class Adaptor_String : Database.Soa.DataSource<string>
        {
            private ArrayEntries<string> m_Array;
            public Adaptor_String(UnityEditor.Profiling.Memory.Experimental.ArrayEntries<string> array)
            {
                m_Array = array;
            }

            public override void Get(Range range, ref string[] dataOut)
            {
                string[] tmp = new string[range.length];
                m_Array.GetEntries((uint)range.first, (uint)range.length, ref tmp);
                for (long i = 0; i < range.length; ++i)
                {
                    dataOut[i] = tmp[i];
                }
            }
        }

        public class Adaptor_Array<DataT> : Database.Soa.DataSource<DataT[]> where DataT : IComparable
        {
            private ArrayEntries<DataT[]> m_Array;
            public Adaptor_Array(UnityEditor.Profiling.Memory.Experimental.ArrayEntries<DataT[]> array)
            {
                m_Array = array;
            }

            public override void Get(Range range, ref DataT[][] dataOut)
            {
                dataOut = new DataT[range.length][];
                m_Array.GetEntries((uint)range.first, (uint)range.length, ref dataOut);
            }
        }
        public static Adaptor<DataT> ApiToDatabase<DataT>(UnityEditor.Profiling.Memory.Experimental.ArrayEntries<DataT> array)
        {
            return new Adaptor<DataT>(array);
        }

        public static Adaptor_String ApiToDatabase(UnityEditor.Profiling.Memory.Experimental.ArrayEntries<string> array)
        {
            return new Adaptor_String(array);
        }

        public static Adaptor_Array<DataT> ApiToDatabase<DataT>(UnityEditor.Profiling.Memory.Experimental.ArrayEntries<DataT[]> array) where DataT : IComparable
        {
            return new Adaptor_Array<DataT>(array);
        }
    }
}
