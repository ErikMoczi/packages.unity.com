namespace Unity.MemoryProfiler.Editor.Database.Soa
{
    public abstract class DataSource<DataT>
    {
        public abstract void Get(Range range, ref DataT[] dataOut);
    }
}
