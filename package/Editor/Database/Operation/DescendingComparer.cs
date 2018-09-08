namespace Unity.MemoryProfiler.Editor.Database.Operation
{
    public class DescendingComparerValueType<DataT> : System.Collections.Generic.IComparer<DataT> where DataT : System.IComparable
    {
        public int Compare(DataT a, DataT b) { return b.CompareTo(a); }
    }
    public class DescendingComparerReferenceType<DataT> : System.Collections.Generic.IComparer<DataT> where DataT : System.IComparable
    {
        public int Compare(DataT a, DataT b)
        {
            if (b == null)
            {
                if (a == null) return 0;
                return -1;
            }
            return b.CompareTo(a);
        }
    }
}
