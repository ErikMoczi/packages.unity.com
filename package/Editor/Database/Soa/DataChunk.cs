namespace Unity.MemoryProfiler.Editor.Database.Soa
{
    internal class DataChunk<DataT>
    {
        public DataChunk(long size)
        {
            m_Data = new DataT[size];
        }

        public DataT[] m_Data;
    }
}
