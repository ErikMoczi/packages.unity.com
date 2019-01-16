namespace Unity.MemoryProfiler.Editor.Database.Soa
{
    internal abstract class DataSource<DataT>
    {
        public abstract void Get(Range range, ref DataT[] dataOut);
    }
}
